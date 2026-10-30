import copy
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

SCRIPTS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(SCRIPTS))
import control
import deploy
import bootstrap


class DeliveryTests(unittest.TestCase):
    def test_bootstrap_reads_exact_immutable_oidc_prefix(self):
        settings = {'use_default': True, 'use_immutable_subject': True,
                    'sub_claim_prefix': 'repo:Mental-NV@7886867/LinguaDesk@1359148631'}
        with patch.object(bootstrap.subprocess, 'check_output', return_value=json.dumps(settings)) as github:
            self.assertEqual(bootstrap.github_subject_prefix('Mental-NV/LinguaDesk'), settings['sub_claim_prefix'])
        github.assert_called_once_with(['gh', 'api', 'repos/Mental-NV/LinguaDesk/actions/oidc/customization/sub'], text=True)

    def test_bootstrap_supports_explicit_legacy_oidc_mode(self):
        settings = {'use_default': True, 'use_immutable_subject': False, 'sub_claim_prefix': 'repo:owner/repo'}
        with patch.object(bootstrap.subprocess, 'check_output', return_value=json.dumps(settings)):
            self.assertEqual(bootstrap.github_subject_prefix('owner/repo'), 'repo:owner/repo')

    def test_unsupported_oidc_template_stops_bootstrap_before_mutations_or_keys(self):
        replies = [json.dumps({'nameWithOwner': 'owner/repo', 'viewerPermission': 'ADMIN'}),
                   json.dumps({'use_default': False})]
        with patch.object(sys, 'argv', ['bootstrap.py']), patch.object(bootstrap.shutil, 'which', return_value='/fixture'), \
             patch.object(bootstrap, 'az', return_value={}) as azure, \
             patch.object(bootstrap.subprocess, 'check_output', side_effect=replies), \
             patch.object(bootstrap.subprocess, 'run') as mutation, patch.object(bootstrap.getpass, 'getpass') as prompt:
            with self.assertRaisesRegex(ValueError, 'custom templates are unsupported'):
                bootstrap.main()
            azure.assert_called_once_with('account', 'show')
            mutation.assert_not_called()
            prompt.assert_not_called()

    def test_bootstrap_rejects_unknown_custom_or_mismatched_oidc_subjects(self):
        good = {'use_default': True, 'use_immutable_subject': True, 'sub_claim_prefix': 'repo:owner@123/repo@456'}
        invalid = [dict(good, use_default=False), dict(good, use_immutable_subject=None),
                   dict(good, sub_claim_prefix=None), dict(good, use_immutable_subject=False)]
        invalid += [dict(good, sub_claim_prefix=prefix) for prefix in
                    ('repo:owner/repo', 'repo:other@123/repo@456', 'repo:owner@123/other@456',
                     'repo:owner@0/repo@456', 'repo:owner@123/repo@456:environment:Production',
                     'repo:owner@123/repo@456\n')]
        for settings in invalid:
            with self.subTest(settings=settings), patch.object(bootstrap.subprocess, 'check_output', return_value=json.dumps(settings)):
                with self.assertRaises(ValueError): bootstrap.github_subject_prefix('owner/repo')

    def event(self, fork=False, bot=False):
        return {'action': 'opened', 'sender': {'login': 'human'}, 'pull_request': {'user': {'login': 'dependabot[bot]' if bot else 'human'}, 'head': {'sha': 'a'*40, 'repo': {'full_name': 'fork/repo' if fork else 'owner/repo'}}}}

    def test_event_table(self):
        for action in ('opened', 'synchronize', 'reopened', 'ready_for_review'):
            event = self.event()
            event['action'] = action
            self.assertEqual(control.target('pull_request', event, 'owner/repo'), ('Staging', 'a'*40))
        for fork, bot in ((True, False), (False, True)):
            self.assertIsNone(control.target('pull_request', self.event(fork, bot), 'owner/repo'))
        self.assertEqual(control.target('push', {'ref': 'refs/heads/main', 'after': 'b'*40}, 'owner/repo'), ('Production', 'b'*40))
        self.assertIsNone(control.target('push', {'ref': 'refs/heads/master', 'after': 'b'*40}, 'owner/repo'))
        self.assertIsNone(control.target('push', {'ref': 'refs/heads/feature'}, 'owner/repo'))
        self.assertIsNone(control.target('pull_request_target', self.event(), 'owner/repo'))

    def good_manifest(self):
        return {'subscriptionId': '00000000-0000-0000-0000-000000000001', 'tenantId': '00000000-0000-0000-0000-000000000002',
                'environments': {e: {'clientId': f'00000000-0000-0000-0000-00000000000{i+3}', 'resourceGroup': control.environment_configuration()[e]['resourceGroup']} for i,e in enumerate(control.ENVIRONMENTS)}}

    def test_manifest_isolation_placeholders_and_no_variables(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary)/'deployment.json'
            good = self.good_manifest()
            path.write_text(json.dumps(good))
            self.assertEqual(control.manifest(path), good)
            for broken in (dict(good, tenantId='<AZURE_TENANT_ID>'), dict(good, unexpectedSecret='bad')):
                path.write_text(json.dumps(broken))
                with self.assertRaises(ValueError): control.manifest(path)
            broken = copy.deepcopy(good)
            broken['environments']['Staging']['clientId'] = good['environments']['Production']['clientId']
            path.write_text(json.dumps(broken))
            with self.assertRaises(ValueError): control.manifest(path)

    def test_what_if_rejects_cross_environment_delete_and_paid_sku(self):
        data = self.good_manifest()
        group = data['environments']['Staging']['resourceGroup']
        rid = f'/subscriptions/{data["subscriptionId"]}/resourceGroups/{group}/providers/Microsoft.Web/serverfarms/app'
        good = {'resourceId': rid, 'changeType': 'Modify', 'after': {'type': 'Microsoft.Web/serverfarms', 'sku': {'name': 'F1'}}}
        deploy.validate_changes({'changes': [good]}, data['subscriptionId'], group)
        for change in (dict(good, resourceId=rid.replace('staging', 'production')), dict(good, changeType='Delete'), dict(good, after={'type': 'Microsoft.Web/serverfarms', 'sku': {'name': 'B1'}})):
            with self.assertRaises(ValueError): deploy.validate_changes({'changes': [change]}, data['subscriptionId'], group)

    def test_stale_main_has_no_mutation(self):
        with patch.dict(os.environ, GITHUB_REPOSITORY='owner/repo'), patch.object(control, 'run', return_value={'object': {'sha': 'b'*40}}) as github:
            self.assertFalse(control.fresh('Production', 'a'*40))
            github.assert_called_once_with('gh','api','repos/owner/repo/git/ref/heads/main')

    def test_azure_failure_preserves_codes_without_expanded_secure_configuration(self):
        sentinel='private-provider-credential-sentinel'
        inner={'code':'ResourceNotFound','message':sentinel,'target':sentinel}
        error={'error':{'code':'InvalidTemplateDeployment','message':sentinel,
                        'details':[{'code':'DeploymentFailed','message':json.dumps({'error':inner})}]}}
        failure=subprocess.CompletedProcess([],1,stdout=sentinel,stderr='ERROR: '+json.dumps(error))
        with patch.object(subprocess,'run',return_value=failure):
            with self.assertRaises(ValueError) as caught:
                deploy.az('deployment','group','validate','--parameters',sentinel)
        message=str(caught.exception)
        self.assertIn('InvalidTemplateDeployment > DeploymentFailed > ResourceNotFound',message)
        self.assertIn('dependencies',message)
        self.assertNotIn(sentinel,message)
        self.assertNotIn(sentinel,deploy.azure_failure('ERROR: '+json.dumps({'code':sentinel,'message':sentinel})))

    def test_azure_failure_supports_cli_codes_and_safe_unknown_fallback(self):
        message=deploy.azure_failure('ERROR: (InvalidTemplateDeployment) private\nCode: SubscriptionIsOverQuotaForSku\nMessage: private')
        self.assertIn('SubscriptionIsOverQuotaForSku',message)
        self.assertIn('No paid upgrade',message)
        self.assertNotIn('private',message)
        self.assertIn('withheld',deploy.azure_failure('arbitrary private diagnostic'))

    def test_azure_cli_multiple_inner_errors_preserve_quota_cause(self):
        stderr='ERROR: '+json.dumps({'code':'InvalidTemplateDeployment','message':'private'})
        for code in ('ValidationForResourceFailed','SubscriptionIsOverQuotaForSku'):
            stderr+='\nInner Errors:\n'+json.dumps({'code':code,'message':'private'})
        message=deploy.azure_failure(stderr)
        self.assertIn('InvalidTemplateDeployment > ValidationForResourceFailed > SubscriptionIsOverQuotaForSku',message)
        self.assertIn('no available quota',message)
        self.assertNotIn('private',message)

    def test_f1_worker_conflict_explains_fix_without_raw_configuration(self):
        error={'code':'Conflict','message':'A 64 Bit worker cannot be used for the site. private-sentinel',
               'details':[{'Code':'Conflict','ExtendedCode':'01020','Message':'private-sentinel'}]}
        message=deploy.azure_failure('ERROR: '+json.dumps(error))
        self.assertIn('[Conflict]',message)
        self.assertIn('use32BitWorkerProcess=true',message)
        self.assertIn('keep the F1 SKU',message)
        self.assertNotIn('private-sentinel',message)
        self.assertNotIn('use32BitWorkerProcess',deploy.azure_failure('ERROR: '+json.dumps({'code':'Conflict','message':'unrelated conflict'})))

    def test_app_service_preflight_uses_dummy_inputs_and_never_mutates(self):
        paths=[]
        def azure(*args):
            self.assertEqual(args[:3],('deployment','group','validate'))
            self.assertEqual(args[4],'rg-staging-linguadesk')
            path=Path(args[-1][1:])
            paths.append(path)
            values=json.loads(path.read_text())['parameters']
            self.assertEqual(values['location']['value'],'polandcentral')
            self.assertEqual(values['providerKey']['value'],'validation-only-not-a-credential')
            self.assertFalse(values['enabled']['value'])
            self.assertEqual(path.stat().st_mode & 0o777,0o600)
            raise ValueError('[SubscriptionIsOverQuotaForSku]')
        with patch.object(deploy,'az',side_effect=azure), patch.dict(os.environ,DEEPSEEK_API_KEY='private-sentinel'):
            with self.assertRaisesRegex(ValueError,r'Staging App Service preflight \(polandcentral, F1\)'):
                deploy.validate_app_service('Staging')
        self.assertFalse(paths[0].exists())

    def test_app_service_preflight_failure_blocks_runtime_and_github_changes(self):
        with patch.object(deploy,'az',side_effect=[{'location':'polandcentral'},[]]) as azure, patch.object(deploy,'preflight'), patch.object(deploy,'check_app_name'), patch.object(deploy,'fresh',return_value=True), patch.object(deploy,'validate_app_service',side_effect=ValueError('quota')):
            with self.assertRaisesRegex(ValueError,'quota'):
                deploy.reconcile('Staging',self.good_manifest(),'fixture-secret',bootstrap=True)
            self.assertEqual([call.args[:2] for call in azure.call_args_list],[('group','show'),('webapp','list')])
        bootstrap=(SCRIPTS/'bootstrap.py').read_text()
        self.assertLess(bootstrap.index('        validate_app_service(environment)'),bootstrap.index("        endpoint = f'repos/"))

    def test_cap_failure_blocks_zip_and_start(self):
        calls=[]
        def azure(*args):
            calls.append(args)
            if args[:2] == ('group', 'show'): return {'location': 'polandcentral'}
            if args[:3] == ('deployment','group','what-if'): return {'changes': []}
            if args[:2] == ('webapp', 'list'): return []
            if args[:3] == ('deployment','group','create'):
                return {'properties': {'outputs': {k: {'value': v} for k,v in {**{k: control.environment_configuration()['Staging'][k] for k in ('appName','hostname','workspaceName','insightsName')}, 'dailyCapMb':50}.items()}}}
        def command(args, **kwargs):
            if args[:3] == ['az','bicep','build-params']:
                Path(args[-1]).write_text(json.dumps({'parameters': {'environment': {'value':'Staging'}, 'dailyCapMb':{'value':50}, 'providerKey':{'value':'fixture-secret'}}}))
                return subprocess.CompletedProcess(args,0)
            raise subprocess.CalledProcessError(1,'pwsh')
        with patch.object(deploy, 'az', side_effect=azure), patch.object(deploy, 'preflight'), patch.object(deploy, 'check_app_name'), patch.object(deploy, 'validate_app_service'), patch.object(deploy, 'fresh', return_value=True), patch.object(subprocess, 'run', side_effect=command):
            with self.assertRaises(subprocess.CalledProcessError): deploy.reconcile('Staging',self.good_manifest(),'secret-credential', 'artifact','a'*40)
        self.assertFalse(any(c[:2] in (('webapp','start'), ('webapp','deploy')) for c in calls))
        self.assertTrue(any(c[:2] == ('webapp','stop') for c in calls))

    def test_zip_waits_for_extraction_without_runtime_tracking(self):
        with patch.object(deploy, 'az', return_value={'status': 4, 'complete': True}) as azure:
            deploy.deploy_package('group', 'app', 'artifact')
        args = azure.call_args.args
        self.assertEqual(args[:2], ('webapp', 'deploy'))
        for flag, value in {'--type':'zip', '--restart':'false', '--clean':'true', '--async':'false',
                            '--track-status':'false', '--enable-kudu-warmup':'false', '--timeout':'300000'}.items():
            self.assertEqual(args[args.index(flag)+1], value)
        self.assertEqual(azure.call_args.kwargs, {'timeout': 330})

    def test_zip_requires_confirmed_completion(self):
        for result in (None, {}, {'status': 0}, {'status': 2, 'complete': True}, {'status': 3, 'complete': True},
                       {'status': 4}, {'status': 4, 'complete': False}, {'status': '4', 'complete': True}):
            with self.subTest(result=result), patch.object(deploy, 'az', return_value=result):
                with self.assertRaisesRegex(ValueError, 'activation is blocked'):
                    deploy.deploy_package('group', 'app', 'artifact')

    def test_zip_process_timeout_is_bounded_and_private(self):
        failure = subprocess.TimeoutExpired('private-command', 330, output='private-output', stderr='private-secret')
        with patch.object(subprocess, 'run', side_effect=failure) as command:
            with self.assertRaises(ValueError) as caught:
                deploy.deploy_package('group', 'app', 'artifact')
        self.assertEqual(command.call_args.kwargs['timeout'], 330)
        self.assertIn('330 seconds', str(caught.exception))
        self.assertIn('activation is blocked', str(caught.exception))
        self.assertNotIn('private', str(caught.exception))

    def test_zip_plain_text_failures_have_safe_phase_diagnostics(self):
        for text, expected in (
                ('Timeout reached while tracking deployment status, private-secret', 'DeploymentStartupTimeout'),
                ('Deployment failed because the site failed to start within 10 mins. private-secret', 'DeploymentStartupTimeout'),
                ('Zip deployment failed. private-secret', 'ZipDeploymentFailed'),
                ('Timeout reached by the command, however, the deployment\n operation is still on-going. private-secret', 'ZipDeploymentTimeout')):
            with self.subTest(text=text):
                diagnostic = deploy.azure_failure(text)
                self.assertIn(expected, diagnostic)
                self.assertNotIn('private', diagnostic)

    def test_reconcile_activates_only_after_caps_and_confirmed_zip(self):
        for zip_result in ({'status': 4, 'complete': True}, {'status': 3, 'complete': True}, {'status': 2, 'complete': False}):
            with self.subTest(zip_result=zip_result):
                events=[]
                output={**{k: control.environment_configuration()['Staging'][k] for k in ('appName','hostname','workspaceName','insightsName')}, 'dailyCapMb':50}
                def azure(*args, **kwargs):
                    if args[:2] == ('group','show'): return {'location':'polandcentral'}
                    if args[:2] == ('webapp','list'): return [{'name':output['appName']}]
                    if args[:2] == ('webapp','show'): return {'state':'Stopped'}
                    if args[:3] == ('deployment','group','what-if'): return {'changes':[]}
                    if args[:3] == ('deployment','group','create'):
                        params=json.loads(Path(args[args.index('--parameters')+1][1:]).read_text())['parameters']
                        events.append('activate' if params['enabled']['value'] else 'reconcile')
                        return {'properties':{'outputs':{k:{'value':v} for k,v in output.items()}}}
                    if args[:2] == ('webapp','deploy'):
                        events.append('zip')
                        return zip_result
                    if args[:2] in (('webapp','stop'),('webapp','start')): events.append(args[1])
                def command(args, **kwargs):
                    if args[:3] == ['az','bicep','build-params']:
                        Path(args[-1]).write_text(json.dumps({'parameters':{'environment':{'value':'Staging'},'dailyCapMb':{'value':50}}}))
                    else: events.append('caps')
                    return subprocess.CompletedProcess(args,0)
                with patch.object(deploy,'az',side_effect=azure), patch.object(deploy,'preflight'), \
                     patch.object(deploy,'check_app_name'), patch.object(deploy,'validate_app_service'), \
                     patch.object(deploy,'fresh',return_value=True), patch.object(subprocess,'run',side_effect=command), \
                     patch.object(control,'smoke',side_effect=lambda *args, **kwargs: events.append('smoke')):
                    if zip_result['status'] == 4:
                        deploy.reconcile('Staging',self.good_manifest(),'fixture-secret','artifact','a'*40)
                        self.assertEqual(events, ['stop','reconcile','stop','caps','zip','activate','start','smoke'])
                    else:
                        with self.assertRaisesRegex(ValueError, 'activation is blocked'):
                            deploy.reconcile('Staging',self.good_manifest(),'fixture-secret','artifact','a'*40)
                        self.assertEqual(events, ['stop','reconcile','stop','caps','zip','stop'])

    def test_existing_runtime_quota_blocks_before_preflight_or_mutation(self):
        site = {'state':'QuotaExceeded','usageState':'Exceeded'}
        stale_list = {'name':'staging-linguadesk','defaultHostName':'staging-linguadesk.azurewebsites.net',
                      'state':'Stopped','usageState':'Normal'}
        with patch.object(deploy,'az',side_effect=[{'location':'polandcentral'},[stale_list],site]) as azure, \
             patch.object(deploy,'preflight') as preflight:
            with self.assertRaisesRegex(ValueError, 'AppServiceRuntimeQuotaExceeded'):
                deploy.reconcile('Staging',self.good_manifest(),'fixture-secret','artifact','a'*40)
            preflight.assert_not_called()
            self.assertEqual([c.args[:2] for c in azure.call_args_list], [('group','show'),('webapp','list'),('webapp','show')])
            self.assertEqual(azure.call_args.kwargs, {'timeout':10})

    def test_runtime_quota_states_do_not_expose_configuration_or_change_sku(self):
        for site in ({'state':'QuotaExceeded'}, {'state':'Running','usageState':'Exceeded'}):
            with self.subTest(site=site), self.assertRaisesRegex(ValueError, 'No paid upgrade was attempted'):
                deploy.check_runtime_quota({**site, 'private':'private-secret'})
        for site in ({'state':'Running','usageState':'Normal'}, {'state':'Stopped'}, {}):
            deploy.check_runtime_quota(site)

    def test_bootstrap_dry_run_idempotent_and_non_mutating(self):
        cmd=[sys.executable,str(SCRIPTS/'bootstrap.py'),'--dry-run']
        first=subprocess.check_output(cmd,text=True)
        self.assertEqual(first,subprocess.check_output(cmd,text=True))
        data=json.loads(first)
        self.assertEqual(data['githubVariables'],[])
        self.assertEqual(data['dailyCapMb'],50)
        self.assertFalse(data['mutations'])

    def test_missing_key_fails_before_azure(self):
        with patch.dict(os.environ, DEEPSEEK_API_KEY=''), patch.object(deploy,'az') as azure:
            with self.assertRaises(ValueError): deploy.key()
            azure.assert_not_called()

    def test_package_checksums_revision_and_private_data(self):
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary)
            publish=root/'artifacts/publish'
            (publish/'wwwroot').mkdir(parents=True)
            (root/'scripts/azure').mkdir(parents=True)
            (root/'scripts/azure/start.sh').write_text('exec dotnet LinguaDesk.Api.dll')
            (publish/'LinguaDesk.Api.dll').write_bytes(b'fixture-api')
            (publish/'Microsoft.Data.Sqlite.dll').write_bytes(b'legitimate-sqlite-library')
            (publish/'wwwroot/index.html').write_text('<html>fixture</html>')
            (publish/'appsettings.json').write_text('{"Serving": {"CredentialRef": "deepseek"}}')
            with patch.object(control,'ROOT',root), patch.object(subprocess,'check_output',side_effect=lambda args, **kwargs: 'a'*40 if args[1]=='rev-parse' else ''):
                control.package()
                control.verify_artifact(root/'artifacts/azure','a'*40)
                with self.assertRaises(ValueError): control.verify_artifact(root/'artifacts/azure','b'*40)
                (root/'artifacts/azure/app.zip').write_bytes(b'tampered')
                with self.assertRaises(ValueError): control.verify_artifact(root/'artifacts/azure','a'*40)
                for forbidden in ('live.db','key-private.xml','runtime.log'):
                    path=publish/forbidden
                    path.write_text('private')
                    with self.assertRaises(ValueError): control.package()
                    path.unlink()
                (publish/'appsettings.json').write_text('{"Provider": {"ApiKey": "private-sentinel"}}')
                with self.assertRaises(ValueError): control.package()

    def test_newer_pr_run_owns_shared_staging(self):
        pr={'state':'open','head':{'sha':'a'*40,'repo':{'full_name':'owner/repo'}},'user':{'login':'human'}}
        responses=[pr,{'workflow_runs':[{'run_number':11,'pull_requests':[{'number':2}]}]},pr]
        with patch.dict(os.environ,GITHUB_REPOSITORY='owner/repo',PR_NUMBER='1',GITHUB_RUN_NUMBER='10'), patch.object(control,'run',side_effect=responses):
            self.assertFalse(control.fresh('Staging','a'*40))
        for state,head in [('closed','a'*40),('open','b'*40)]:
            changed=copy.deepcopy(pr)
            changed['state']=state
            changed['head']['sha']=head
            with patch.dict(os.environ,GITHUB_REPOSITORY='owner/repo',PR_NUMBER='1'), patch.object(control,'run',return_value=changed):
                self.assertFalse(control.fresh('Staging','a'*40))

    def test_current_runtime_records_and_existing_plan_quota(self):
        for runtimes in (['DOTNETCORE:10.0'], [{'runtime':'.NET','version':'10.0 (LTS)','config':'DOTNETCORE|10.0'}]):
            def azure(*args):
                if args[:2]==('account','show'): return {'id':'subscription'}
                if args[:2]==('webapp','list-runtimes'): return runtimes
                if args[:2]==('appservice','list-locations'): return [{'name':'West Europe'}]
                if args[:2]==('rest','--method'): return {'value':[{'name':{'value':'FreeWorkers'},'currentValue':2,'limit':2}]}
                raise AssertionError(args)
            with patch.object(deploy,'az',side_effect=azure), patch.object(subprocess,'run'):
                deploy.preflight('subscription','westeurope',required_plans=0)
                with self.assertRaises(ValueError): deploy.preflight('subscription','westeurope',required_plans=1)

    def test_exact_names_and_regions_in_bootstrap_dry_run(self):
        configuration=control.environment_configuration()
        expected={
            'Production': ('rg-prod-linguadesk','linguadesk','plan-prod-linguadesk','appi-prod-linguadesk','law-prod-linguadesk','id-prod-linguadesk','swedencentral'),
            'Staging': ('rg-staging-linguadesk','staging-linguadesk','plan-staging-linguadesk','appi-staging-linguadesk','law-staging-linguadesk','id-staging-linguadesk','polandcentral'),
        }
        keys=('resourceGroup','appName','planName','insightsName','workspaceName','identityName','location')
        for environment,values in expected.items():
            self.assertEqual(tuple(configuration[environment][k] for k in keys),values)
            self.assertEqual(configuration[environment]['hostname'],configuration[environment]['appName']+'.azurewebsites.net')
        dry=json.loads(subprocess.check_output([sys.executable,str(SCRIPTS/'bootstrap.py'),'--dry-run'],text=True))
        self.assertFalse(dry['sharedIdentityGroup'])
        self.assertEqual(dry['environments'],configuration)

    def test_unavailable_name_fails_without_alternative_or_resource_mutation(self):
        selected=control.environment_configuration()['Production']
        for available in (False,True):
            calls=[]
            def azure(*args):
                calls.append(args)
                return {'nameAvailable':available}
            with patch.object(deploy,'az',side_effect=azure):
                if available:
                    deploy.check_app_name('subscription',selected,[])
                else:
                    with self.assertRaisesRegex(ValueError,'No alternative'):
                        deploy.check_app_name('subscription',selected,[])
            self.assertEqual(len(calls),1)
            self.assertEqual(calls[0][:3],('rest','--method','post'))
            self.assertEqual(json.loads(calls[0][-1])['name'],'linguadesk')

    def test_existing_owned_app_is_reused_but_wrong_hostname_is_blocked(self):
        selected=control.environment_configuration()['Production']
        with patch.object(deploy,'az') as azure:
            deploy.check_app_name('subscription',selected,[{'name':'linguadesk','defaultHostName':'linguadesk.azurewebsites.net'}])
            azure.assert_not_called()
            for existing in ([{'name':'linguadesk','defaultHostName':'linguadesk-hash.swedencentral.azurewebsites.net'}],
                             [{'name':'different-app','defaultHostName':'different-app.azurewebsites.net'}]):
                with self.assertRaises(ValueError): deploy.check_app_name('subscription',selected,existing)

    def test_activation_requires_exact_resource_outputs_and_hostname(self):
        selected=control.environment_configuration()['Production']
        good={k:selected[k] for k in ('appName','hostname','workspaceName','insightsName')}
        good['dailyCapMb']=50
        deploy.validate_outputs('Production',good,50)
        for key,value in [('hostname','linguadesk-hash.swedencentral.azurewebsites.net'),('appName','staging-linguadesk'),('workspaceName','law-staging-linguadesk')]:
            with self.assertRaises(ValueError): deploy.validate_outputs('Production',{**good,key:value},50)

    def test_wrong_region_blocks_before_preflight_or_mutation(self):
        with patch.object(deploy,'az',return_value={'location':'westeurope'}) as azure, patch.object(deploy,'preflight') as preflight:
            with self.assertRaisesRegex(ValueError,'region'):
                deploy.reconcile('Production',self.good_manifest(),'fixture-secret')
            self.assertEqual(azure.call_args_list[0].args[:2],('group','show'))
            self.assertEqual(azure.call_count,1)
            preflight.assert_not_called()

    def test_previous_production_region_requires_explicit_migration(self):
        selected=control.environment_configuration()['Production']
        deploy.check_group_location(selected,{'location':'swedencentral'})
        with self.assertRaisesRegex(ValueError,'austriaeast.*swedencentral.*cannot be changed in place'):
            deploy.check_group_location(selected,{'location':'austriaeast'})
        bootstrap=(SCRIPTS/'bootstrap.py').read_text()
        self.assertLess(bootstrap.index('            check_group_location('),bootstrap.index('    provider_keys = {}'))

    def test_manifest_cannot_swap_environment_groups(self):
        with tempfile.TemporaryDirectory() as temporary:
            data=self.good_manifest()
            data['environments']['Production']['resourceGroup']='rg-staging-linguadesk'
            path=Path(temporary)/'deployment.json'
            path.write_text(json.dumps(data))
            with self.assertRaises(ValueError): control.manifest(path)

    def test_routine_reconciliation_leaves_in_group_identity_and_rbac_untouched(self):
        data=self.good_manifest()
        group=data['environments']['Production']['resourceGroup']
        for kind in ('Microsoft.ManagedIdentity/userAssignedIdentities/id-prod-linguadesk',
                     'Microsoft.ManagedIdentity/userAssignedIdentities/id-prod-linguadesk/federatedIdentityCredentials/github',
                     'Microsoft.Authorization/roleDefinitions/fixture','Microsoft.Authorization/roleAssignments/fixture'):
            resource=f'/subscriptions/{data["subscriptionId"]}/resourceGroups/{group}/providers/{kind}'
            for change in ('Ignore','NoChange'):
                deploy.validate_changes({'changes':[{'resourceId':resource,'changeType':change}]},data['subscriptionId'],group)
            for change in ('Create','Modify','Delete'):
                with self.assertRaises(ValueError):
                    deploy.validate_changes({'changes':[{'resourceId':resource,'changeType':change}]},data['subscriptionId'],group)

    def test_expected_auto_created_insights_alert_can_remain_untouched(self):
        data=self.good_manifest()
        for selected in control.environment_configuration().values():
            group=selected['resourceGroup']
            resource=f'/subscriptions/{data["subscriptionId"]}/resourceGroups/{group}/providers/Microsoft.AlertsManagement/smartDetectorAlertRules/Failure Anomalies - {selected["insightsName"]}'
            for change in ('Ignore','NoChange'):
                for casing in (resource,resource.upper()):
                    deploy.validate_changes({'changes':[{'resourceId':casing,'changeType':change}]},data['subscriptionId'],group)

    def test_auto_created_insights_alert_cannot_be_changed(self):
        data=self.good_manifest()
        for selected in control.environment_configuration().values():
            group=selected['resourceGroup']
            resource=f'/subscriptions/{data["subscriptionId"]}/resourceGroups/{group}/providers/Microsoft.AlertsManagement/smartDetectorAlertRules/Failure Anomalies - {selected["insightsName"]}'
            for change in ('Create','Modify','Delete','Deploy','Unsupported'):
                with self.assertRaisesRegex(ValueError,'cannot change the auto-created'):
                    deploy.validate_changes({'changes':[{'resourceId':resource,'changeType':change}]},data['subscriptionId'],group)

    def test_passive_alert_exception_preserves_scope_name_and_payload_privacy(self):
        data=self.good_manifest()
        selected=control.environment_configuration()['Staging']
        group=selected['resourceGroup']
        resource=f'/subscriptions/{data["subscriptionId"]}/resourceGroups/{group}/providers/Microsoft.AlertsManagement/smartDetectorAlertRules/Failure Anomalies - {selected["insightsName"]}'
        for changed in (
            resource.replace(group,'rg-prod-linguadesk'),
            resource.replace(data['subscriptionId'],'00000000-0000-0000-0000-000000000009'),
            resource+'-unexpected',
            resource.replace(selected['insightsName'],'appi-prod-linguadesk'),
            resource.replace('Microsoft.AlertsManagement/smartDetectorAlertRules','Microsoft.Storage/storageAccounts'),
        ):
            with self.assertRaises(ValueError) as caught:
                deploy.validate_changes({'changes':[{'resourceId':changed,'changeType':'Ignore','after':{'private':'secret-sentinel'}}]},data['subscriptionId'],group)
            message=str(caught.exception)
            self.assertIn(changed,message)
            self.assertIn('Ignore',message)
            self.assertNotIn('secret-sentinel',message)

    def test_workflow_security_contract(self):
        source=(control.ROOT/'.github/workflows/azure-app-service.yml').read_text()
        self.assertIn('branches: [main]',source)
        self.assertIn("github.ref == 'refs/heads/main'",source)
        self.assertNotIn('master',source)
        self.assertIn("{('main', 'branch')}",(SCRIPTS/'bootstrap.py').read_text())
        self.assertNotIn('pull_request_target',source)
        self.assertNotIn('vars.',source)
        self.assertEqual(source.count('id-token: write'),2)
        self.assertEqual(source.count('cancel-in-progress: false'),2)
        self.assertEqual(source.count('needs: checks'),2)
        self.assertIn('persist-credentials: false',source)
        check_job=source.split('  staging:')[0]
        self.assertNotIn('secrets.',check_job)
        for line in source.splitlines():
            if 'uses:' in line: self.assertRegex(line,r'@[a-f0-9]{40} ')
        offline=(control.ROOT/'scripts/ci-offline.sh').read_text()
        for forbidden in ('--live', 'verify-access', 'test:e2e-live', 'm046-live'): self.assertNotIn(forbidden,offline)
        self.assertIn('bash scripts/frontend.sh smoke',offline)
        self.assertIn('unset "$name"',offline)


if __name__ == '__main__': unittest.main()
