#!/usr/bin/env python3
"""Serialized by the environment workflow concurrency group. Secret input is environment-only."""
import json
import os
import re
import signal
from pathlib import Path
import subprocess
import sys
import tempfile
from control import ROOT, ENVIRONMENTS, manifest, verify_artifact, fresh, environment_configuration
from progress import log, stage


def azure_failure(stderr):
    # Error messages can contain expanded templates, app settings or secure input.
    # Emit only recognized service codes and our own fixed text, never raw messages.
    if ('Timeout reached while tracking deployment status' in stderr
            or 'Deployment failed because the site failed to start within' in stderr):
        return '[DeploymentStartupTimeout]. ZIP extraction may have completed, but CLI runtime tracking failed. Keep startup tracking disabled for the stopped upload phase; use the separate activation/smoke gate.'
    if 'Zip deployment failed.' in stderr:
        return '[ZipDeploymentFailed]. Azure reported ZIP extraction failure; activation is blocked.'
    if 'Timeout reached by the command, however, the deployment operation' in ' '.join(stderr.split()):
        return '[ZipDeploymentTimeout]. ZIP completion is unverified; activation is blocked.'
    hints = {
        'ResourceNotFound': 'A required Azure resource was not found. Check resource dependencies and the selected group.',
        'SubscriptionIsOverQuotaForSku': 'The subscription has no available quota for this SKU in the selected region. No paid upgrade was attempted.',
        'QuotaExceeded': 'Azure quota is exhausted in the selected region. No paid upgrade was attempted.',
        'AuthorizationFailed': 'The signed-in identity lacks permission for this operation.',
        'MissingSubscriptionRegistration': 'Register the required Azure resource provider in the selected subscription.',
        'NoRegisteredProviderFound': 'Check resource-provider registration, region and API support.',
        'RequestDisallowedByPolicy': 'An Azure policy blocks this resource configuration.',
        'NameNotAvailable': 'The exact requested Azure resource name is unavailable.',
        'LocationNotAvailableForResourceType': 'The resource type is unavailable in the selected region.',
        'InvalidTemplate': 'Azure rejected the compiled template.',
        'InvalidParameter': 'Azure rejected a resource parameter.',
    }
    known = set(hints) | {'InvalidTemplateDeployment', 'DeploymentFailed', 'ResourceDeploymentFailure', 'ValidationForResourceFailed',
                          'BadRequest', 'Conflict', 'NotFound', 'InvalidResourceReference', 'SkuNotAvailable'}
    codes = []
    worker_mode_conflict = False

    def collect(node):
        nonlocal worker_mode_conflict
        if isinstance(node, dict):
            code = node.get('code')
            if isinstance(code, str) and code in known and code not in codes:
                codes.append(code)
            for field in ('error', 'details', 'innererror', 'innerError', 'message'):
                collect(node.get(field))
        elif isinstance(node, list):
            for child in node:
                collect(child)
        elif isinstance(node, str):
            if re.search(r'\b(?:64[\s-]*bit|x64)\s+worker\b', node, re.IGNORECASE):
                worker_mode_conflict = True
            # Azure sometimes embeds a serialized inner error in its message.
            offset = 0
            while (start := node.find('{', offset)) != -1:
                try:
                    value, consumed = json.JSONDecoder().raw_decode(node[start:])
                except ValueError:
                    offset = start + 1
                    continue
                collect(value)
                # CLI may print multiple JSON objects separated by "Inner Errors:".
                offset = start + consumed

    collect(stderr)
    # Also support Azure CLI's non-JSON "ERROR: (Code)" / "Code: Code" format.
    for code in re.findall(r'(?m)^(?:ERROR: )?\(([A-Za-z]+)\)|^Code:\s*([A-Za-z]+)\s*$', stderr):
        value = code[0] or code[1]
        if value in known and value not in codes:
            codes.append(value)
    if not codes:
        return 'Diagnostic details withheld because no recognized safe Azure error code was returned.'
    hint = next((hints[code] for code in reversed(codes) if code in hints), '')
    if worker_mode_conflict and 'Conflict' in codes:
        hint = 'F1/shared hosting rejects the requested 64-bit worker setting. Set siteConfig.use32BitWorkerProcess=true; keep the F1 SKU.'
    return '[' + ' > '.join(codes) + '].' + (' ' + hint if hint else '')


def az(*args, timeout=None):
    try:
        result = subprocess.run(['az', *args, '--only-show-errors', '-o', 'json'], capture_output=True, text=True, timeout=timeout)
    except subprocess.TimeoutExpired:
        raise ValueError(f'Azure operation exceeded {timeout} seconds: ' + ' '.join(args[:3]) + '. Completion is unverified; activation is blocked.') from None
    if result.returncode:
        raise ValueError('Azure operation failed: ' + ' '.join(args[:3]) + ' ' + azure_failure(result.stderr))
    return json.loads(result.stdout) if result.stdout.strip() else None


def deploy_package(group, app, artifact):
    # The app is stopped/gated here. Linux CLI startup tracking would wait for
    # a runtime that cannot start until the subsequent activation deployment.
    with stage('Upload and extract ZIP; 330-second process limit'):
        result = az('webapp', 'deploy', '--resource-group', group, '--name', app,
                    '--src-path', str(Path(artifact) / 'app.zip'), '--type', 'zip',
                    '--restart', 'false', '--clean', 'true', '--async', 'false',
                    '--track-status', 'false', '--enable-kudu-warmup', 'false', '--timeout', '300000',
                    timeout=330)
        if not isinstance(result, dict) or type(result.get('status')) is not int or result['status'] != 4 or result.get('complete') is not True:
            raise ValueError('ZIP extraction did not return verified completion; activation is blocked.')
    log('ZIP extraction verified; proceeding to activation and provider-free smoke.')


def key():
    value = os.environ.get('DEEPSEEK_API_KEY', '')
    if len(value.strip()) < 12 or '<' in value or value.startswith(('placeholder', 'your-')):
        raise ValueError('A real environment DEEPSEEK_API_KEY must be present before mutation.')
    return value


def preflight(subscription, location, required_plans=0):
    tooling = json.loads((ROOT / 'infra/azure/tooling.json').read_text())
    with stage('Install pinned Bicep compiler'):
        subprocess.run(['az', 'bicep', 'install', '--version', tooling['bicep']], check=True, stdout=subprocess.DEVNULL)
    log('Checking Azure subscription, .NET 10 Linux runtime and regional F1 availability/quota.')
    account = az('account', 'show')
    if account['id'] != subscription:
        raise ValueError('Azure login subscription does not match the generated manifest.')
    runtimes = az('webapp', 'list-runtimes', '--os', 'linux')
    configurations = {item.get('config', '') if isinstance(item, dict) else item.replace(':', '|') for item in runtimes}
    if 'DOTNETCORE|10.0' not in configurations:
        raise ValueError('The selected .NET 10 Linux code stack is unavailable. No runtime downgrade is permitted.')
    locations = az('appservice', 'list-locations', '--sku', 'F1', '--linux-workers-enabled')
    normalize = lambda text: ''.join(text.lower().split())
    if normalize(location) not in {normalize(item['name']) for item in locations}:
        raise ValueError('Linux F1 is unavailable in the selected region; no paid upgrade is permitted.')
    usages = az('rest', '--method', 'get', '--url', f'https://management.azure.com/subscriptions/{subscription}/providers/Microsoft.Web/locations/{location}/usages?api-version=2024-11-01')
    free = [u for u in usages.get('value', []) if 'f1' in str(u.get('name', {})).lower() or 'free' in str(u.get('name', {})).lower()]
    if any(u.get('currentValue', 0) + required_plans > u.get('limit', 0) for u in free):
        raise ValueError('Regional F1 quota exhausted.')
    # Resource creation remains authoritative where the provider omits a free-tier quota entry.


def check_app_name(subscription, selected, existing_apps=None):
    if existing_apps is None:
        exists = az('group', 'exists', '--name', selected['resourceGroup'])
        existing_apps = az('webapp', 'list', '--resource-group', selected['resourceGroup']) if exists else []
    if existing_apps:
        if len(existing_apps) != 1 or existing_apps[0]['name'] != selected['appName']:
            raise ValueError('Unexpected Web App in the selected environment group; refusing to redirect deployment.')
        if existing_apps[0].get('defaultHostName', '').lower() != selected['hostname']:
            raise ValueError('Existing Web App does not have the required exact Azure hostname.')
        return
    availability = az('rest', '--method', 'post', '--url',
        f'https://management.azure.com/subscriptions/{subscription}/providers/Microsoft.Web/checknameavailability?api-version=2024-11-01',
        '--body', json.dumps({'name': selected['appName'], 'type': 'Microsoft.Web/sites', 'isFqdn': False}))
    if availability.get('nameAvailable') is not True:
        raise ValueError(f"The exact App Service name '{selected['appName']}' is unavailable. No alternative name or hostname will be substituted.")


def check_group_location(selected, group_info):
    if group_info['location'].lower() != selected['location']:
        raise ValueError(f"Resource-group region mismatch: {selected['resourceGroup']} exists in {group_info['location']}; "
                         f"the selected region is {selected['location']}. Azure resource-group locations cannot be changed in place. "
                         'Explicit migration/recreation is required before bootstrap; no resources were deleted.')


def validate_app_service(environment):
    # ARM short-circuits validation of the nested app when its monitoring input
    # depends on a deployment output. Validate that module separately to exercise
    # Microsoft.Web's regional/SKU quota check. These dummy inputs are never applied.
    selected = environment_configuration()[environment]
    values = {
        'name': selected['appName'], 'planName': selected['planName'],
        'location': selected['location'], 'environment': environment,
        'providerKey': 'validation-only-not-a-credential',
        'monitoringConnection': 'validation-only-not-a-connection', 'enabled': False,
    }
    with tempfile.TemporaryDirectory(prefix='linguadesk-validation-') as temporary:
        params = Path(temporary) / 'parameters.json'
        params.write_text(json.dumps({'parameters': {key: {'value': value} for key, value in values.items()}}))
        os.chmod(params, 0o600)
        try:
            az('deployment', 'group', 'validate', '--resource-group', selected['resourceGroup'],
               '--template-file', str(ROOT / 'infra/azure/modules/app-service.bicep'), '--parameters', '@' + str(params))
        except ValueError as error:
            raise ValueError(f"{environment} App Service preflight ({selected['location']}, F1): {error}") from None


def validate_outputs(environment, output, cap):
    selected = environment_configuration()[environment]
    for key in ('appName', 'hostname', 'workspaceName', 'insightsName'):
        if output.get(key) != selected[key]:
            raise ValueError('Infrastructure returned an unexpected resource name or hostname; activation is blocked.')
    if output.get('dailyCapMb') != cap:
        raise ValueError('Unexpected infrastructure cap policy.')


def validate_changes(result, subscription, group):
    prefix = f'/subscriptions/{subscription}/resourcegroups/{group}/'
    allowed = ('microsoft.web/serverfarms/', 'microsoft.web/sites/', 'microsoft.operationalinsights/workspaces/', 'microsoft.insights/components/', 'microsoft.resources/deployments/')
    passive_alert_ids = {
        (prefix + 'providers/microsoft.alertsmanagement/smartdetectoralertrules/failure anomalies - ' + selected['insightsName']).lower()
        for selected in environment_configuration().values()
        if selected['resourceGroup'].lower() == group.lower()
    }
    for change in result.get('changes', []):
        rid = change['resourceId'].lower()
        if not rid.startswith(prefix.lower()):
            raise ValueError(f'Reconciliation escaped the selected environment resource group/type boundary: {change["changeType"]} {change["resourceId"]}')
        if rid in passive_alert_ids:
            # Application Insights creates this rule outside our runtime template.
            if change['changeType'] in ('Ignore', 'NoChange'):
                continue
            raise ValueError('Routine deployment cannot change the auto-created Insights alert rule.')
        bootstrap_types = ('microsoft.managedidentity/userassignedidentities/', 'microsoft.authorization/roledefinitions/', 'microsoft.authorization/roleassignments/')
        if any('/providers/' + kind in rid for kind in bootstrap_types):
            # Incremental what-if may report untouched in-group identity/RBAC resources.
            if change['changeType'] in ('Ignore', 'NoChange'):
                continue
            raise ValueError('Routine deployment cannot change identity, federation or RBAC resources.')
        if not any('/providers/' + kind in rid for kind in allowed):
            raise ValueError(f'Reconciliation escaped the selected environment resource group/type boundary: {change["changeType"]} {change["resourceId"]}')
        if change['changeType'] not in ('Create', 'Modify', 'NoChange', 'Ignore', 'Deploy'):
            raise ValueError('Destructive or unsupported reconciliation change rejected.')
        after = change.get('after') or {}
        resource_type = after.get('type', '').lower()
        if resource_type == 'microsoft.web/serverfarms' and after.get('sku', {}).get('name') != 'F1':
            raise ValueError('Paid hosting SKU rejected.')


def check_runtime_quota(site):
    if (str(site.get('state', '')).casefold() == 'quotaexceeded'
            or str(site.get('usageState', '')).casefold() == 'exceeded'):
        raise ValueError('[AppServiceRuntimeQuotaExceeded]. Azure has disabled this app for a runtime quota; '
                         'readiness cannot pass until Azure clears the condition. Check the app runtime quotas/reset, '
                         'not regional F1 allocation quota. No paid upgrade was attempted.')


def reconcile(environment, data, provider, artifact=None, sha=None, bootstrap=False):
    entry = data['environments'][environment]
    selected = environment_configuration()[environment]
    group = entry['resourceGroup']
    if group != selected['resourceGroup']:
        raise ValueError('Manifest group differs from the versioned environment configuration.')
    subscription = data['subscriptionId']
    location = selected['location']
    log(f'Target: {environment}; app {selected["appName"]}; group {group}; region {location}.')
    with stage('Check target resource group, exact hostname and runtime quota state'):
        group_info = az('group', 'show', '--name', group)
        check_group_location(selected, group_info)
        existing_apps = az('webapp', 'list', '--resource-group', group)
        check_app_name(subscription, selected, existing_apps)
        if existing_apps:
            # List metadata can still say Stopped/Normal while the individual site
            # endpoint reports a platform quota suspension.
            check_runtime_quota(az('webapp', 'show', '--resource-group', group, '--name', selected['appName'], timeout=10))
    with stage('Preflight Azure subscription, runtime and F1 allocation'):
        preflight(subscription, location, required_plans=0 if existing_apps else 1)
    if not bootstrap:
        with stage('Check current main revision before resource validation'):
            if not fresh(environment, sha):
                log('Stale revision skipped before resource mutation.')
                return
    with stage('Validate App Service provider configuration and F1 quota'):
        validate_app_service(environment)
    # Check all desired inputs and what-if before the first stop/resource mutation.
    with tempfile.TemporaryDirectory(prefix='linguadesk-secure-') as temporary:
        os.chmod(temporary, 0o700)
        params = Path(temporary) / 'parameters.json'
        with stage('Compile secure environment parameters'):
            compiled = subprocess.run(['az', 'bicep', 'build-params', '--file', str(ROOT / 'infra/azure' / (environment.lower() + '.bicepparam')),
                                       '--outfile', str(params)], env={**os.environ, 'DEEPSEEK_API_KEY': provider}, capture_output=True)
            if compiled.returncode:
                raise ValueError('Environment parameter compilation failed.')
        desired = json.loads(params.read_text())
        parameters = desired['parameters']
        cap = parameters['dailyCapMb']['value']
        if parameters['environment']['value'] != environment or type(cap) is not int or not 1 <= cap <= 50:
            raise ValueError('Versioned parameter environment or monitoring cap is invalid.')
        def write_params(enabled):
            desired['parameters']['enabled'] = {'value': enabled}
            desired['parameters']['reconcileMonitoring'] = {'value': not enabled}
            params.write_text(json.dumps(desired))
            os.chmod(params, 0o600)
        write_params(False)
        base = ('--resource-group', group, '--template-file', str(ROOT / 'infra/azure/main.bicep'), '--parameters', '@' + str(params))
        with stage('Validate infrastructure template'):
            az('deployment', 'group', 'validate', *base)
        with stage('Review infrastructure what-if against the resource boundary'):
            changes = az('deployment', 'group', 'what-if', *base, '--no-pretty-print', '--result-format', 'FullResourcePayloads')
            validate_changes(changes, subscription, group)
        with stage('Recheck target and current main revision before stopping'):
            apps = az('webapp', 'list', '--resource-group', group)
            if len(apps) > 1:
                raise ValueError('Unexpected extra Web Apps in the isolated resource group.')
            if not bootstrap and not fresh(environment, sha):
                log('Stale revision skipped immediately before stop.')
                return
        with stage('Stop existing app and verify the old worker is stopped'):
            for app in apps:
                az('webapp', 'stop', '--resource-group', group, '--name', app['name'])
                if az('webapp', 'show', '--resource-group', group, '--name', app['name'])['state'] != 'Stopped':
                    raise ValueError('Old serving worker is not stopped.')
        with stage('Apply infrastructure with serving and telemetry disabled'):
            result = az('deployment', 'group', 'create', *base, '--name', 'linguadesk-runtime')
        output = {k: v['value'] for k, v in result['properties']['outputs'].items()}
        app = output['appName']
        try:
            validate_outputs(environment, output, cap)
            # Explicit stop covers first creation; the runtime deployment guard and disabled exporter gate collection.
            with stage('Ensure the provisioned app is stopped'):
                az('webapp', 'stop', '--resource-group', group, '--name', app)
            with stage('Enforce and verify telemetry caps and retention with PowerShell'):
                subprocess.run(['pwsh', '-NoProfile', '-File', str(ROOT / 'scripts/azure/configure-monitoring.ps1'),
                                '-ResourceGroup', group, '-Workspace', output['workspaceName'], '-Insights', output['insightsName'],
                                '-DailyCapMb', str(output['dailyCapMb']), '-UseAzureCli'], check=True)
            if bootstrap:
                log(environment + ' infrastructure and caps provisioned; app remains stopped until Actions deploys a checked artifact.')
                return
            deploy_package(group, app, artifact)
            write_params(True)
            # One complete app-settings owner, including secure values. No competing partial writers.
            with stage('Activate runtime settings for the extracted package'):
                az('deployment', 'group', 'create', *base, '--name', 'linguadesk-runtime')
            with stage('Start the Web App'):
                az('webapp', 'start', '--resource-group', group, '--name', app)
            from control import smoke
            def check_runtime():
                check_runtime_quota(az('webapp', 'show', '--resource-group', group, '--name', app, timeout=10))
            with stage('Run provider-free HTTPS, readiness, revision and cookie smoke; 240-second budget'):
                smoke('https://' + output['hostname'], sha, state_probe=check_runtime)
            summary = os.environ.get('GITHUB_STEP_SUMMARY')
            if summary:
                with open(summary, 'a') as report:
                    report.write(f'{environment}: revision `{sha}`; https://{output["hostname"]}\n')
        except Exception:
            # Preserve persistent data and recovery material; leave failure unavailable.
            with stage('Stop the failed deployment and retain data and backups'):
                az('webapp', 'stop', '--resource-group', group, '--name', app)
            raise


def main():
    environment, artifact, sha = sys.argv[1:]
    if environment not in ENVIRONMENTS: raise ValueError('Unknown environment.')
    with stage('Validate generated manifest, provider input and artifact integrity'):
        data = manifest()
        provider = key()
        verify_artifact(artifact, sha)
    log(f'Deploying checked revision {sha}.')
    reconcile(environment, data, provider, artifact, sha)


if __name__ == '__main__':
    signal.signal(signal.SIGTERM, lambda signum, frame: sys.exit(128 + signum))
    try: main()
    except Exception as error:
        print('Deployment failed; data and backups retained. ' + (str(error) if isinstance(error, ValueError) else type(error).__name__), file=sys.stderr)
        sys.exit(1)
