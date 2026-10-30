#!/usr/bin/env python3
"""Find regions that pass provider validation for one additional Linux F1 plan."""
import argparse
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import uuid

from control import ROOT, ENVIRONMENTS, environment_configuration
from deploy import azure_failure


def azure(*args, subscription=None, timeout=120):
    command = ['az', *args, '--only-show-errors', '-o', 'json']
    if subscription:
        command += ['--subscription', subscription]
    result = subprocess.run(command, capture_output=True, text=True, timeout=timeout)
    if result.returncode:
        raise ValueError(azure_failure(result.stderr))
    try:
        return json.loads(result.stdout)
    except ValueError:
        raise ValueError('Azure CLI returned an unreadable response.') from None


def candidates(locations, requested=None):
    # list-locations returns display names such as "West Europe".
    normalize = lambda value: ''.join(value.lower().split())
    available = sorted({normalize(item['name']) for item in locations})
    if not requested:
        return available
    selected = list(dict.fromkeys(normalize(value) for value in requested))
    unknown = sorted(set(selected) - set(available))
    if unknown:
        raise ValueError('Not advertised for Linux F1: ' + ', '.join(unknown))
    return selected


def probe(region, template, subscription, group, timeout):
    # Unique per check, so an existing allocation can never produce a false pass.
    name = 'linguadesk-f1-probe-' + uuid.uuid4().hex
    try:
        result = azure('deployment', 'group', 'validate', '--resource-group', group,
                       '--name', name, '--template-file', str(template),
                       '--parameters', 'location=' + region, 'planName=' + name,
                       '--validation-level', 'Provider', subscription=subscription, timeout=timeout)
        properties = result.get('properties', {})
        if properties.get('error'):
            raise ValueError(azure_failure(json.dumps(properties['error'])))
        if properties.get('provisioningState') != 'Succeeded' or properties.get('diagnostics'):
            return {'region': region, 'status': 'unverified',
                    'detail': 'Azure did not confirm complete provider validation without diagnostics.'}
        return {'region': region, 'status': 'passed', 'detail': 'Provider validation passed for one new Linux F1 plan.'}
    except subprocess.TimeoutExpired:
        return {'region': region, 'status': 'timeout', 'detail': 'Azure validation timed out; quota is unknown.'}
    except ValueError as error:
        detail = str(error)
        quota = any(code in detail for code in ('SubscriptionIsOverQuotaForSku', 'QuotaExceeded'))
        return {'region': region, 'status': 'quota_exceeded' if quota else 'failed', 'detail': detail}


def scan(args):
    if not shutil.which('az'):
        raise ValueError('Azure CLI is required. Install it and run az login first.')
    account = azure('account', 'show', subscription=args.subscription, timeout=args.timeout)
    subscription = account['id']
    group = args.resource_group or environment_configuration()[args.environment]['resourceGroup']
    if not azure('group', 'exists', '--name', group, subscription=subscription, timeout=args.timeout):
        raise ValueError('An existing resource group is required; choose one with --resource-group. No group was created.')
    regions = candidates(azure('appservice', 'list-locations', '--sku', 'F1', '--linux-workers-enabled',
                               subscription=subscription, timeout=args.timeout), args.regions)
    if not regions:
        raise ValueError('Azure advertises no Linux F1 regions for this subscription.')
    print(f'Checking {len(regions)} regions; subscription {subscription}; validation group {group}.', file=sys.stderr)
    version = json.loads((ROOT / 'infra/azure/tooling.json').read_text())['bicep']
    installed = subprocess.run(['az', 'bicep', 'install', '--version', version, '--only-show-errors'],
                               capture_output=True, text=True, timeout=args.timeout)
    if installed.returncode:
        raise ValueError('Could not install the pinned Bicep compiler.')
    with tempfile.TemporaryDirectory(prefix='linguadesk-f1-scan-') as temporary:
        template = Path(temporary) / 'probe.json'
        compiled = subprocess.run(['az', 'bicep', 'build', '--file', str(ROOT / 'infra/azure/f1-quota-probe.bicep'),
                                   '--outfile', str(template), '--only-show-errors'],
                                  capture_output=True, text=True, timeout=args.timeout)
        if compiled.returncode:
            raise ValueError('F1 quota probe compilation failed.')
        results = []
        with ThreadPoolExecutor(max_workers=args.workers) as executor:
            pending = [executor.submit(probe, region, template, subscription, group, args.timeout) for region in regions]
            for future in as_completed(pending):
                result = future.result()
                results.append(result)
                print(f"[{len(results)}/{len(regions)}] {result['region']}: {result['status']}", file=sys.stderr, flush=True)
    results.sort(key=lambda item: item['region'])
    return {'subscriptionId': subscription, 'resourceGroup': group, 'sku': 'F1', 'os': 'Linux',
            'requiredNewPlans': 1, 'mutations': False, 'checkedAtUtc': datetime.now(timezone.utc).isoformat(),
            'passingRegions': [item['region'] for item in results if item['status'] == 'passed'],
            'results': results, 'note': 'Point-in-time provider validation; quota/capacity is not reserved. Configuration is unchanged.'}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--environment', choices=ENVIRONMENTS, default='Production', help='Select the existing validation group (default: Production).')
    parser.add_argument('--resource-group', help='Use this existing group instead of the environment group; resources are never created.')
    parser.add_argument('--subscription', help='Subscription ID/name (default: current Azure CLI account). Does not change the active account.')
    parser.add_argument('--regions', nargs='+', help='Check only these region codes (default: all advertised Linux F1 regions).')
    parser.add_argument('--workers', type=int, choices=range(1, 9), default=4, metavar='1-8', help='Parallel checks (default: 4).')
    parser.add_argument('--timeout', type=int, default=120, help='Per-command timeout in seconds (default: 120).')
    parser.add_argument('--json', action='store_true', help='Emit JSON on stdout; progress remains on stderr.')
    args = parser.parse_args(argv)
    if args.timeout < 1:
        parser.error('--timeout must be positive')
    try:
        report = scan(args)
    except subprocess.TimeoutExpired:
        print('F1 scan stopped: Azure setup/discovery timed out.', file=sys.stderr)
        return 2
    except (ValueError, OSError) as error:
        print('F1 scan stopped: ' + (str(error) if isinstance(error, ValueError) else type(error).__name__), file=sys.stderr)
        return 2
    if args.json:
        print(json.dumps(report, indent=2))
    else:
        print(f"Subscription: {report['subscriptionId']}\nValidation group: {report['resourceGroup']}")
        print(f"{'REGION':<24} {'RESULT':<16} DETAIL")
        for result in report['results']:
            print(f"{result['region']:<24} {result['status']:<16} {result['detail']}")
        print('\nPassing regions: ' + (', '.join(report['passingRegions']) or 'none'))
        print(report['note'])
    return 0 if report['passingRegions'] else 1


if __name__ == '__main__':
    sys.exit(main())
