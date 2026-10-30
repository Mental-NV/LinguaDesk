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
if (-not (Get-Module -ListAvailable Az | Where-Object Version -EQ $version)) {
    Install-Module Az -RequiredVersion $version -Repository PSGallery -Scope CurrentUser -Force -AllowClobber
}
Import-Module Az -RequiredVersion $version
if ($UseAzureCli) {
    . "$PSScriptRoot/azure-cli-context.ps1"
    Connect-LinguaDeskAzureCliContext
}
. "$PSScriptRoot/monitoring-policy.ps1"
Set-LinguaDeskMonitoringCaps -ResourceGroup $ResourceGroup -Workspace $Workspace -Insights $Insights -DailyCapMb $DailyCapMb
