import contextlib
import io
import json
import os
from pathlib import Path
import subprocess
import sys
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import find_f1_regions as finder


class F1RegionsTests(unittest.TestCase):
    def test_region_names_normalize_filter_and_deduplicate(self):
        locations = [{'name': 'West Europe'}, {'name': 'Austria East'}]
        self.assertEqual(finder.candidates(locations), ['austriaeast', 'westeurope'])
        self.assertEqual(finder.candidates(locations, ['West Europe', 'westeurope']), ['westeurope'])
        with self.assertRaisesRegex(ValueError, 'Not advertised'):
            finder.candidates(locations, ['unknown'])

    def test_probe_validates_new_plan_with_explicit_provider_level(self):
        with patch.object(finder, 'azure', return_value={'properties': {'provisioningState': 'Succeeded'}}) as azure:
            for _ in range(2):
                self.assertEqual(finder.probe('westeurope', 'probe.json', 'subscription', 'group', 30)['status'], 'passed')
        names = []
        for call in azure.call_args_list:
            args = call.args
            self.assertEqual(args[:3], ('deployment', 'group', 'validate'))
            self.assertEqual(args[args.index('--validation-level') + 1], 'Provider')
            self.assertEqual(call.kwargs, {'subscription': 'subscription', 'timeout': 30})
            name = args[args.index('--name') + 1]
            names.append(name)
            self.assertIn('planName=' + name, args)
            self.assertIn('location=westeurope', args)
            self.assertNotIn('providerKey', ' '.join(args))
        self.assertNotEqual(*names)

    def test_quota_and_other_failures_are_distinct(self):
        for code, expected in [('SubscriptionIsOverQuotaForSku', 'quota_exceeded'),
                               ('QuotaExceeded', 'quota_exceeded'), ('AuthorizationFailed', 'failed')]:
            with patch.object(finder, 'azure', side_effect=ValueError('[' + code + ']')):
                result = finder.probe('region', 'probe.json', 'subscription', 'group', 30)
            self.assertEqual(result['status'], expected)

    def test_incomplete_validation_and_timeouts_never_pass(self):
        for properties in ({}, {'provisioningState': 'Failed'},
                           {'provisioningState': 'Succeeded', 'diagnostics': [{'code': 'NestedDeploymentShortCircuited'}]}):
            with patch.object(finder, 'azure', return_value={'properties': properties}):
                self.assertEqual(finder.probe('region', 'probe.json', 'subscription', 'group', 30)['status'], 'unverified')
        with patch.object(finder, 'azure', side_effect=subprocess.TimeoutExpired('az', 30)):
            self.assertEqual(finder.probe('region', 'probe.json', 'subscription', 'group', 30)['status'], 'timeout')

    def test_validation_error_in_successful_cli_response_is_rejected(self):
        result = {'properties': {'provisioningState': 'Succeeded', 'error': {'code': 'QuotaExceeded', 'message': 'private'}}}
        with patch.object(finder, 'azure', return_value=result):
            outcome = finder.probe('region', 'probe.json', 'subscription', 'group', 30)
        self.assertEqual(outcome['status'], 'quota_exceeded')
        self.assertNotIn('private', outcome['detail'])

    def test_json_command_scopes_subscription_never_mutates_and_cleans_up(self):
        temporary_paths = []

        def command(args, **kwargs):
            if args[1:3] == ['bicep', 'install']:
                output = None
            elif args[1:3] == ['bicep', 'build']:
                template = Path(args[args.index('--outfile') + 1])
                template.write_text('{}')
                temporary_paths.append(template)
                output = None
            else:
                if args[1:3] == ['account', 'show']:
                    output = {'id': 'resolved-subscription'}
                    self.assertEqual(args[args.index('--subscription') + 1], 'requested-subscription')
                else:
                    self.assertEqual(args[args.index('--subscription') + 1], 'resolved-subscription')
                    if args[1:3] == ['group', 'exists']:
                        output = True
                    elif args[1:3] == ['appservice', 'list-locations']:
                        output = [{'name': 'West Europe'}, {'name': 'Australia Central'}]
                    elif args[1:4] == ['deployment', 'group', 'validate']:
                        if 'location=australiacentral' in args:
                            error = 'ERROR: ' + json.dumps({'code': 'SubscriptionIsOverQuotaForSku', 'message': 'secret-sentinel'})
                            return subprocess.CompletedProcess(args, 1, '', error)
                        output = {'properties': {'provisioningState': 'Succeeded'}}
                    else:
                        raise AssertionError('Unexpected Azure operation: ' + ' '.join(args[:4]))
            return subprocess.CompletedProcess(args, 0, json.dumps(output), '')

        stdout, stderr = io.StringIO(), io.StringIO()
        with patch.object(finder.shutil, 'which', return_value='/fixture/az'), patch.object(subprocess, 'run', side_effect=command), patch.dict(os.environ, DEEPSEEK_API_KEY='secret-sentinel'), contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
            code = finder.main(['--subscription', 'requested-subscription', '--resource-group', 'existing', '--json'])
        self.assertEqual(code, 0)
        report = json.loads(stdout.getvalue())
        self.assertEqual(report['passingRegions'], ['westeurope'])
        self.assertFalse(report['mutations'])
        self.assertEqual(report['requiredNewPlans'], 1)
        self.assertNotIn('secret-sentinel', stdout.getvalue() + stderr.getvalue())
        self.assertEqual([row['status'] for row in report['results']], ['quota_exceeded', 'passed'])
        self.assertFalse(any(path.exists() for path in temporary_paths))

    def test_missing_group_stops_without_creating_resources(self):
        with patch.object(finder.shutil, 'which', return_value='/fixture/az'), patch.object(finder, 'azure', side_effect=[{'id': 'subscription'}, False]) as azure, contextlib.redirect_stderr(io.StringIO()):
            self.assertEqual(finder.main([]), 2)
            self.assertEqual(azure.call_count, 2)

    def test_no_passing_regions_exit_one_and_setup_timeout_exit_two(self):
        report = {'passingRegions': [], 'results': []}
        with patch.object(finder, 'scan', return_value=report), contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(finder.main(['--json']), 1)
        with patch.object(finder, 'scan', side_effect=subprocess.TimeoutExpired('az', 10)), contextlib.redirect_stderr(io.StringIO()):
            self.assertEqual(finder.main([]), 2)


if __name__ == '__main__':
    unittest.main()
