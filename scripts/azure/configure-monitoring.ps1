param(
    [Parameter(Mandatory)][string]$ResourceGroup,
    [Parameter(Mandatory)][string]$Workspace,
    [Parameter(Mandatory)][string]$Insights,
    [Parameter(Mandatory)][ValidateRange(1,50)][int]$DailyCapMb,
    [switch]$UseAzureCli
)
$ErrorActionPreference = 'Stop'
# Pin the full Az distribution so the cap cmdlets and Accounts dependency agree.
$version = (Get-Content "$PSScriptRoot/../../infra/azure/tooling.json" -Raw | ConvertFrom-Json).azPowerShell
Write-Host 'Monitoring: checking the pinned Az PowerShell modules.'
if (-not (Get-Module -ListAvailable Az | Where-Object Version -EQ $version)) {
    Write-Host 'Monitoring: installing the pinned Az modules; this can take several minutes.'
    Install-Module Az -RequiredVersion $version -Repository PSGallery -Scope CurrentUser -Force -AllowClobber
}
Write-Host 'Monitoring: importing the pinned Az modules.'
Import-Module Az -RequiredVersion $version
if ($UseAzureCli) {
    Write-Host 'Monitoring: authenticating from the current Azure CLI context.'
    . "$PSScriptRoot/azure-cli-context.ps1"
    Connect-LinguaDeskAzureCliContext
}
. "$PSScriptRoot/monitoring-policy.ps1"
Write-Host 'Monitoring: applying and independently reading workspace/component caps and retention.'
Set-LinguaDeskMonitoringCaps -ResourceGroup $ResourceGroup -Workspace $Workspace -Insights $Insights -DailyCapMb $DailyCapMb
