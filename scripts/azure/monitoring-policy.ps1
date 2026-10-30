function Set-LinguaDeskMonitoringCaps {
    param([string]$ResourceGroup, [string]$Workspace, [string]$Insights, [ValidateRange(1,50)][int]$DailyCapMb)
    [decimal]$cap = [decimal]$DailyCapMb / 1000
    $subscription = (Get-AzContext -ErrorAction Stop).Subscription.Id
    $workspacePath = "/subscriptions/$subscription/resourceGroups/$ResourceGroup/providers/Microsoft.OperationalInsights/workspaces/${Workspace}?api-version=2023-09-01"
    # The pinned Set-AzOperationalInsightsWorkspace -DailyQuotaGb parameter is
    # Int32, which converts 0.05 to 0. ARM accepts an exact fractional JSON number.
    $payload = @{properties=@{workspaceCapping=@{dailyQuotaGb=$cap}}} | ConvertTo-Json -Depth 4 -Compress
    $updated = Invoke-AzRestMethod -Method PATCH -Path $workspacePath -Payload $payload -ErrorAction Stop
    if ($updated.StatusCode -ne 200) { throw 'Cannot apply the exact fractional workspace cap.' }
    Set-AzApplicationInsightsDailyCap -ResourceGroupName $ResourceGroup -Name $Insights -DailyCapGB ([double]$cap) -ErrorAction Stop | Out-Null
    # Read independently after both setters; never trust an update response alone.
    $workspaceResponse = Invoke-AzRestMethod -Method GET -Path $workspacePath -ErrorAction Stop
    if ($workspaceResponse.StatusCode -ne 200) { throw 'Cannot read workspace cap and retention.' }
    $workspaceState = ($workspaceResponse.Content | ConvertFrom-Json).properties
    $path = "/subscriptions/$subscription/resourceGroups/$ResourceGroup/providers/Microsoft.Insights/components/$Insights/CurrentBillingFeatures?api-version=2015-05-01"
    $response = Invoke-AzRestMethod -Method GET -Path $path -ErrorAction Stop
    if ($response.StatusCode -ne 200) { throw 'Cannot read Application Insights cap.' }
    $componentState = $response.Content | ConvertFrom-Json
    if ($null -eq $workspaceState.WorkspaceCapping.DailyQuotaGb -or $null -eq $componentState.DataVolumeCap.Cap -or
        [decimal]$workspaceState.WorkspaceCapping.DailyQuotaGb -ne [decimal]$cap -or
        [decimal]$componentState.DataVolumeCap.Cap -ne [decimal]$cap -or $workspaceState.RetentionInDays -ne 30) {
        throw 'Both exact fractional daily caps and 30-day retention must pass readback before enabling serving.'
    }
    Write-Output "Verified workspace/component caps: $cap GB/day; retention: 30 days."
}
