#!/usr/bin/env python3
"""Bounded, provider-free hosted evidence. Requires bootstrap/operator access to BOTH groups."""
import argparse
import json
import re
import sys
import time
from control import manifest
from deploy import az


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('environment', choices=('Staging','Production'))
    parser.add_argument('sha')
    args=parser.parse_args()
    if not re.fullmatch('[a-f0-9]{40}',args.sha): raise ValueError('Invalid revision.')
    data=manifest()
    account=az('account','show')
    if account['id']!=data['subscriptionId']: raise ValueError('Wrong active subscription.')
    workspaces={}
    for env,entry in data['environments'].items():
        group=entry['resourceGroup']
        workspace=az('monitor','log-analytics','workspace','list','--resource-group',group)
        insights=az('resource','list','--resource-group',group,'--resource-type','Microsoft.Insights/components')
        if len(workspace)!=1 or len(insights)!=1: raise ValueError('Unexpected monitoring topology.')
        component=az('rest','--method','get','--url','https://management.azure.com'+insights[0]['id']+'/CurrentBillingFeatures?api-version=2015-05-01')
        deployment=az('deployment','group','show','--resource-group',group,'--name','linguadesk-runtime')
        cap_mb=deployment['properties']['outputs']['dailyCapMb']['value']
        if type(cap_mb) is not int or not 1 <= cap_mb <= 50: raise ValueError('Invalid recorded cap policy.')
        expected_cap=cap_mb/1000
        if workspace[0]['workspaceCapping']['dailyQuotaGb'] != expected_cap or component['DataVolumeCap']['Cap'] != expected_cap or workspace[0]['retentionInDays'] != 30:
            raise ValueError('Exact cap/retention readback failed.')
        workspaces[env]=workspace[0]['customerId']
    query=("AppDependencies | where TimeGenerated > ago(30m) | where Name == 'deployment.marker' "
           f"| where tostring(Properties['deployment.revision']) == '{args.sha}' "
           f"and tostring(Properties['deployment.environment']) == '{args.environment}' | count")
    deadline=time.monotonic()+300
    while time.monotonic()<deadline:
        counts={}
        for env,workspace in workspaces.items():
            result=az('monitor','log-analytics','query','--workspace',workspace,'--analytics-query',query)
            counts[env]=int(result[0]['Count'])
        other='Production' if args.environment=='Staging' else 'Staging'
        if counts[other]: raise ValueError('Telemetry crossed environment boundaries.')
        if counts[args.environment]:
            print(f'{args.environment} marker routing, both versioned caps and both 30-day retentions verified for {args.sha}.')
            return
        time.sleep(15)
    raise ValueError('Marker unavailable within five minutes; hosted telemetry acceptance remains unverified.')


if __name__=='__main__':
    try: main()
    except Exception as error:
        print('Monitoring verification failed: '+(str(error) if isinstance(error,ValueError) else type(error).__name__),file=sys.stderr)
        sys.exit(1)
