$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/../azure-cli-context.ps1"
$script:Case = 'success'
$script:Calls = @()
$script:TokenFixture = 'offline.private-token.sentinel'
function az {
    $script:Calls += ,$args
    $global:LASTEXITCODE = 0
    if ($args[0] -ne 'account') { throw 'Unexpected CLI operation.' }
    if ($args[1] -eq 'show') {
        if ($script:Case -eq 'account-failure') { $global:LASTEXITCODE = 1; return }
        return @{id='fixture-subscription';tenantId='fixture-tenant';user=@{name='fixture-account'}} | ConvertTo-Json -Compress
    }
    if ($args[1] -ne 'get-access-token') { throw 'Unexpected CLI operation.' }
    if ($args -notcontains 'fixture-subscription') { throw 'Token request omitted the selected subscription.' }
    if ($script:Case -eq 'token-failure') { $global:LASTEXITCODE = 1; return }
    if ($script:Case -eq 'malformed-token') { return 'private-invalid-json-sentinel' }
    return @{
        accessToken = $(if ($script:Case -eq 'missing-token') {''} else {$script:TokenFixture})
        subscription = $(if ($script:Case -eq 'wrong-token-subscription') {'other'} else {'fixture-subscription'})
        tenant = $(if ($script:Case -eq 'wrong-token-tenant') {'other'} else {'fixture-tenant'})
    } | ConvertTo-Json -Compress
}
function Disable-AzContextAutosave { param($Scope, $ErrorAction)
    if ($Scope -ne 'Process') { throw 'Autosave must be disabled only for this process.' }
    $script:AutosaveDisabled = $true
}
function Connect-AzAccount { param([string]$AccessToken, $AccountId, $Tenant, $Subscription, $Scope, $ErrorAction)
    $script:Connected = $true
    if ($AccessToken -ne $script:TokenFixture) { throw 'Token was altered (SecureString regression).' }
    if (-not $script:AutosaveDisabled -or $Scope -ne 'Process' -or $Tenant -ne 'fixture-tenant' -or $Subscription -ne 'fixture-subscription') {
        throw 'Incorrect login scope or context.'
    }
    if ($script:Case -eq 'sign-in-failure') { throw "Authentication error: $AccessToken" }
}
function Get-AzContext { param($ErrorAction)
    return @{
        Subscription=@{Id=$(if ($script:Case -eq 'wrong-context-subscription') {'other'} else {'fixture-subscription'})}
        Tenant=@{Id=$(if ($script:Case -eq 'wrong-context-tenant') {'other'} else {'fixture-tenant'})}
    }
}
foreach ($case in @('success','account-failure','token-failure','malformed-token','missing-token',
                    'wrong-token-subscription','wrong-token-tenant','sign-in-failure','wrong-context-subscription','wrong-context-tenant')) {
    $script:Case = $case
    $script:Calls = @()
    $script:Connected = $false
    $script:AutosaveDisabled = $false
    $failed = $false
    try { Connect-LinguaDeskAzureCliContext }
    catch {
        $failed = $true
        if ($_.Exception.Message -notlike 'Monitoring authentication failed during *' -or
            $_.Exception.Message -match 'sentinel|offline\.private') { throw 'Authentication diagnostics leaked raw input.' }
    }
    if ($failed -ne ($case -ne 'success')) { throw "Unexpected authentication result for $case" }
    if ($case -eq 'account-failure' -and $script:Calls.Count -ne 1) { throw 'Token requested after failed account lookup.' }
    if ($case -in @('account-failure','token-failure','malformed-token','missing-token','wrong-token-subscription','wrong-token-tenant') -and $script:Connected) {
        throw 'Invalid CLI inputs reached Connect-AzAccount.'
    }
    if ($case -eq 'success' -and -not $script:Connected) { throw 'Valid token never reached Connect-AzAccount.' }
}
Write-Output 'Azure CLI context: 10 string-token, failure, context-isolation and sanitized-diagnostic fixtures passed.'
