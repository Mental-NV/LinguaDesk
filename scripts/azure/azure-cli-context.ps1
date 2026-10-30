function Connect-LinguaDeskAzureCliContext {
    $tokenJson = $null
    $token = $null
    $stage = 'Azure CLI account lookup'
    try {
        $accountJson = az account show --only-show-errors -o json 2>$null
        if ($LASTEXITCODE -ne 0) { throw 'Account lookup failed.' }
        $account = $accountJson | ConvertFrom-Json -ErrorAction Stop
        if (-not $account.id -or -not $account.tenantId -or -not $account.user.name) { throw 'Incomplete Azure account.' }

        $stage = 'Azure CLI ARM token lookup'
        $tokenJson = az account get-access-token --resource https://management.azure.com/ --subscription $account.id --only-show-errors -o json 2>$null
        if ($LASTEXITCODE -ne 0) { throw 'Token lookup failed.' }
        $token = $tokenJson | ConvertFrom-Json -ErrorAction Stop
        $stage = 'token tenant/subscription verification'
        if ($token.accessToken -isnot [string] -or [string]::IsNullOrWhiteSpace($token.accessToken) -or
            $token.subscription -ne $account.id -or $token.tenant -ne $account.tenantId) {
            throw 'Invalid or mismatched CLI token metadata.'
        }

        $stage = 'Az PowerShell sign-in'
        Disable-AzContextAutosave -Scope Process -ErrorAction Stop | Out-Null
        # Connect-AzAccount expects String. SecureString would be coerced to the
        # literal "System.Security.SecureString", producing an invalid token.
        Connect-AzAccount -AccessToken $token.accessToken -AccountId $account.user.name -Tenant $account.tenantId -Subscription $account.id -Scope Process -ErrorAction Stop | Out-Null

        $stage = 'Az PowerShell context verification'
        $context = Get-AzContext -ErrorAction Stop
        if ($context.Subscription.Id -ne $account.id -or $context.Tenant.Id -ne $account.tenantId) {
            throw 'PowerShell context differs from the Azure CLI account.'
        }
    }
    catch {
        # Cmdlet/native errors may include authentication material; print only our
        # fixed stage label. Never include the underlying exception or CLI output.
        throw "Monitoring authentication failed during $stage. No cap settings were changed."
    }
    finally {
        $tokenJson = $null
        $token = $null
    }
}
