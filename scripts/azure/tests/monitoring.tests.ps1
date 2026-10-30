$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/../monitoring-policy.ps1"
$script:WorkspaceCap = 0.05
$script:InsightsCap = 0.05
$script:Case = 'success'
$script:Calls = @()
function Set-AzOperationalInsightsWorkspace { param($ResourceGroupName, $Name, [Nullable[int]]$DailyQuotaGb)
    throw 'The integer-only workspace cap setter must not be used.'
}
function Set-AzApplicationInsightsDailyCap {
    [CmdletBinding()]
    param($ResourceGroupName, $Name, [double]$DailyCapGB)
    $script:Calls += 'component-set'
    if ($DailyCapGB -ne 0.05 -or $ResourceGroupName -ne 'fixture-group' -or $Name -ne 'fixture-insights') { throw 'Invalid component cap/routing.' }
    if ($script:Case -eq 'component-setter-failed') { throw 'Mock component setter rejected the cap.' }
}
function Get-AzContext { return @{Subscription = @{Id = 'fixture-subscription'}} }
function Invoke-AzRestMethod {
    [CmdletBinding()]
    param($Method, $Path, $Payload)
    $workspacePath = '/subscriptions/fixture-subscription/resourceGroups/fixture-group/providers/Microsoft.OperationalInsights/workspaces/fixture-logs?api-version=2023-09-01'
    $componentPath = '/subscriptions/fixture-subscription/resourceGroups/fixture-group/providers/Microsoft.Insights/components/fixture-insights/CurrentBillingFeatures?api-version=2015-05-01'
    if ($Method -eq 'PATCH') {
        $script:Calls += 'workspace-patch'
        if ($Path -ne $workspacePath) { throw 'Wrong workspace routing.' }
        $body = $Payload | ConvertFrom-Json
        $value = $body.properties.workspaceCapping.dailyQuotaGb
        if ($value -is [string] -or [decimal]$value -ne [decimal]0.05) { throw 'Fractional JSON cap was changed or stringified.' }
        if (($body.PSObject.Properties.Name -join ',') -ne 'properties' -or
            ($body.properties.PSObject.Properties.Name -join ',') -ne 'workspaceCapping' -or
            ($body.properties.workspaceCapping.PSObject.Properties.Name -join ',') -ne 'dailyQuotaGb') {
            throw 'PATCH must not modify unrelated workspace configuration.'
        }
        if ($script:Case -eq 'workspace-setter-failed') { throw 'Mock workspace PATCH rejected.' }
        return @{StatusCode=$(if ($script:Case -eq 'workspace-patch-status') {400} else {200});Content='{}'}
    }
    if ($Method -ne 'GET') { throw 'Unsupported REST mutation.' }
    if ($Path -eq $workspacePath) {
        $script:Calls += 'workspace-get'
        return @{StatusCode=$(if ($script:Case -eq 'workspace-get-status') {500} else {200});Content=(@{
            properties=@{workspaceCapping=@{dailyQuotaGb=$script:WorkspaceCap};retentionInDays=$(if ($script:Case -eq 'retention') {90} else {30})}
        } | ConvertTo-Json -Depth 4)}
    }
    if ($Path -eq $componentPath) {
        $script:Calls += 'component-get'
        return @{StatusCode=$(if ($script:Case -eq 'component-get-status') {500} else {200});Content=(@{
            DataVolumeCap=@{Cap=$script:InsightsCap}
        } | ConvertTo-Json)}
    }
    throw 'Unexpected REST target.'
}
$cases = @('success','repeat','rounded-workspace','rounded-component','zero-workspace','missing-workspace','missing-component',
           'workspace-setter-failed','component-setter-failed','workspace-patch-status','workspace-get-status','component-get-status','retention','comma-culture')
foreach ($case in $cases) {
    $script:Case = $case
    $script:Calls = @()
    $script:WorkspaceCap = switch ($case) { 'rounded-workspace' {1}; 'zero-workspace' {0}; 'missing-workspace' {$null}; default {0.05} }
    $script:InsightsCap = switch ($case) { 'rounded-component' {1}; 'missing-component' {$null}; default {0.05} }
    $oldCulture = [System.Threading.Thread]::CurrentThread.CurrentCulture
    if ($case -eq 'comma-culture') { [System.Threading.Thread]::CurrentThread.CurrentCulture = [cultureinfo]'de-DE' }
    $failed = $false
    try { Set-LinguaDeskMonitoringCaps -ResourceGroup fixture-group -Workspace fixture-logs -Insights fixture-insights -DailyCapMb 50 }
    catch { $failed = $true }
    finally { [System.Threading.Thread]::CurrentThread.CurrentCulture = $oldCulture }
    if ($failed -ne ($case -notin @('success','repeat','comma-culture'))) { throw "Unexpected cap-policy outcome for $case" }
    if (-not $failed -and ($script:Calls -join ',') -ne 'workspace-patch,component-set,workspace-get,component-get') { throw 'Missing independent cap readbacks.' }
    if ($case -in @('workspace-setter-failed','workspace-patch-status') -and $script:Calls -contains 'component-set') { throw 'Workspace failure did not stop the component update.' }
}
Write-Output "Monitoring policy: $($cases.Count) fractional JSON, typed-cmdlet, readback, routing, idempotency, culture and failure fixtures passed."
