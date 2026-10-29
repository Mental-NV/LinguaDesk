#!/usr/bin/env python3
import argparse
import getpass
import json
import os
from pathlib import Path
import re
import signal
import shutil
import subprocess
import sys
import tempfile
from control import ROOT, ENVIRONMENTS, manifest, environment_configuration
from deploy import az, preflight, reconcile, check_app_name, check_group_location, validate_app_service


def main():
    parser = argparse.ArgumentParser(description='Create isolated F1 resources, OIDC and GitHub environments. Never commits or pushes.')
    parser.add_argument('--dry-run', action='store_true', help='Show desired setup; no authentication, prompts or mutations.')
    args = parser.parse_args()
    configuration = environment_configuration()
    if args.dry_run:
        print(json.dumps({'environments': configuration, 'sharedIdentityGroup': False, 'sku': 'F1', 'dailyCapMb': 50,
                          'githubVariables': [], 'secretPerEnvironment': 'DEEPSEEK_API_KEY', 'mutations': False}))
        return
    for command in ('az', 'gh', 'pwsh'):
        if not shutil.which(command): raise ValueError('Required command missing: ' + command)
    # Authentication, admin access, keys and regional availability are checked BEFORE mutation.
    account = az('account', 'show')
    repository_info = json.loads(subprocess.check_output(['gh', 'repo', 'view', '--json', 'nameWithOwner,viewerPermission'], text=True))
    repository = repository_info['nameWithOwner']
    if repository_info['viewerPermission'] != 'ADMIN': raise ValueError('GitHub repository admin access is required for environment setup.')
    if not re.fullmatch(r'[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+', repository): raise ValueError('Invalid GitHub repository.')
    existing_path = ROOT / 'infra/azure/deployment.json'
    if existing_path.exists():
        existing = manifest(existing_path)
        if existing['subscriptionId'] != account['id'] or existing['tenantId'] != account['tenantId']:
            raise ValueError('Active Azure context differs from the existing generated deployment manifest.')
    for selected in configuration.values():
        if az('group', 'exists', '--name', selected['resourceGroup']):
            check_group_location(selected, az('group', 'show', '--name', selected['resourceGroup']))
    provider_keys = {}
    for environment in ENVIRONMENTS:
        value = os.environ.get('LINGUADESK_BOOTSTRAP_' + environment.upper() + '_KEY')
        if not value:
            if not sys.stdin.isatty(): raise ValueError('Supply both environment keys through secure input before bootstrap.')
            value = getpass.getpass(environment + ' DEEPSEEK_API_KEY: ')
        if len(value.strip()) < 12 or '<' in value: raise ValueError('Missing or placeholder provider credential.')
        provider_keys[environment] = value
    for environment in ENVIRONMENTS:
        selected = configuration[environment]
        preflight(account['id'], selected['location'], required_plans=0 if existing_path.exists() else 1)
        check_app_name(account['id'], selected)
    # Subscription-scope validation exercises resource/role schemas without mutation.
    # Subscription deployment records have immutable locations too. Use a distinct
    # record name per region without changing any resource or identity names.
    deployment_name = 'linguadesk-bootstrap-' + configuration['Production']['location']
    base = ('--name', deployment_name, '--location', configuration['Production']['location'], '--template-file', str(ROOT / 'infra/azure/bootstrap.bicep'), '--parameters', 'repository=' + repository)
    az('deployment', 'sub', 'validate', *base)
    result = az('deployment', 'sub', 'create', *base)
    data = {'subscriptionId': account['id'], 'tenantId': account['tenantId'], 'environments': {}}
    for item in result['properties']['outputs']['environments']['value']:
        data['environments'][item['environment']] = {k: item[k] for k in ('clientId', 'resourceGroup')}
    with tempfile.TemporaryDirectory() as directory:
        temporary = Path(directory) / 'deployment.json'
        temporary.write_text(json.dumps(data, indent=2) + '\n')
        manifest(temporary)
        destination = ROOT / 'infra/azure/deployment.json'
        if destination.exists() and manifest(destination) != data:
            raise ValueError('Existing manifest targets differ; refusing to redirect deployment identities.')
        destination.write_text(temporary.read_text())
    # Validate BOTH regional F1 allocations before changing GitHub configuration
    # or provisioning either runtime. The outer template can skip nested validation.
    for environment in ENVIRONMENTS:
        validate_app_service(environment)
    for environment in ENVIRONMENTS:
        endpoint = f'repos/{repository}/environments/{environment}'
        policy = {'wait_timer': 0, 'reviewers': [], 'deployment_branch_policy': {'protected_branches': False, 'custom_branch_policies': True}}
        subprocess.run(['gh', 'api', '--method', 'PUT', endpoint, '--input', '-'], input=json.dumps(policy), text=True, stdout=subprocess.DEVNULL, check=True)
        policies = json.loads(subprocess.check_output(['gh', 'api', endpoint + '/deployment-branch-policies?per_page=100'], text=True))['branch_policies']
        wanted = {('main', 'branch')} if environment == 'Production' else {('refs/pull/*/merge', 'branch')}
        for existing in policies:
            if (existing['name'], existing['type']) not in wanted:
                subprocess.run(['gh', 'api', '--method', 'DELETE', endpoint + '/deployment-branch-policies/' + str(existing['id'])], check=True)
        for name, kind in wanted - {(p['name'], p['type']) for p in policies}:
            subprocess.run(['gh', 'api', '--method', 'POST', endpoint + '/deployment-branch-policies', '--input', '-'],
                           input=json.dumps({'name': name, 'type': kind}), text=True, stdout=subprocess.DEVNULL, check=True)
        subprocess.run(['gh', 'secret', 'set', '--repo', repository, '--env', environment, 'DEEPSEEK_API_KEY'], input=provider_keys[environment], text=True, check=True)
        reconcile(environment, data, provider_keys[environment], bootstrap=True)
    print('Bootstrap complete. Commit generated infra/azure/deployment.json. Shared billing allowance consumption is unverified; daily caps may overshoot.')


if __name__ == '__main__':
    signal.signal(signal.SIGTERM, lambda signum, frame: sys.exit(128 + signum))
    try: main()
    except Exception as error:
        print('Bootstrap stopped: ' + (str(error) if isinstance(error, ValueError) else type(error).__name__), file=sys.stderr)
        sys.exit(1)
