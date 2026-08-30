param(
    [ValidatePattern('^[a-z0-9][a-z0-9-]{1,15}$')]
    [string]$Name = 'srm-test',
    [string]$Location = 'germanywestcentral',
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function New-Secret {
    param([int]$ByteCount = 24)
    $bytes = [byte[]]::new($ByteCount)
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
    }
    finally {
        $generator.Dispose()
    }
    return 'Aa9!' + ([BitConverter]::ToString($bytes) -replace '-', '').ToLowerInvariant()
}

function Write-EnvironmentFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][System.Collections.IDictionary]$Values
    )

    if ((Test-Path -LiteralPath $Path) -and -not $Force) {
        throw "Refusing to overwrite $Path. Use -Force only when rotating the complete environment."
    }

    $lines = foreach ($key in $Values.Keys) { "$key=$($Values[$key])" }
    [System.IO.File]::WriteAllLines($Path, [string[]]$lines, [System.Text.UTF8Encoding]::new($false))
}

$nameParts = $Name -split '-'
$databasePrefix = ($nameParts | ForEach-Object {
    [System.Globalization.CultureInfo]::InvariantCulture.TextInfo.ToTitleCase($_)
}) -join ''
$uniqueSuffix = [Guid]::NewGuid().ToString('N').Substring(0, 8)

function New-ApplicationValues {
    param(
        [Parameter(Mandatory)][string]$SqlHost,
        [Parameter(Mandatory)][string]$RedisHost,
        [Parameter(Mandatory)][string]$RedmineHost,
        [Parameter(Mandatory)][string]$AuthHost,
        [Parameter(Mandatory)][string]$CoreHost,
        [Parameter(Mandatory)][string]$EnvironmentLabel
    )

    return [ordered]@{
        SQL_HOST = $SqlHost
        SQL_PORT = '1433'
        SQL_USERNAME = 'sa'
        SQL_PASSWORD = New-Secret
        SQL_AUTH_DATABASE = "${databasePrefix}${EnvironmentLabel}AuthDb"
        SQL_CORE_DATABASE = "${databasePrefix}${EnvironmentLabel}CoreDb"
        REDIS_HOST = $RedisHost
        REDIS_PORT = '6379'
        REDMINE_HOST = $RedmineHost
        REDMINE_PORT = '3000'
        JWT_ISSUER = "${Name}-${EnvironmentLabel}-auth"
        JWT_AUDIENCE = "${Name}-${EnvironmentLabel}-services"
        JWT_SIGNING_KEY = New-Secret -ByteCount 32
        JWT_ACCESS_TOKEN_LIFETIME_MINUTES = '15'
        BOOTSTRAP_ADMIN_USERNAME = "${Name}-${EnvironmentLabel}-admin"
        BOOTSTRAP_ADMIN_EMAIL = "${Name}-${EnvironmentLabel}-admin@example.invalid"
        BOOTSTRAP_ADMIN_PASSWORD = New-Secret
        BOOTSTRAP_ADMIN_FIRST_NAME = 'SRM'
        BOOTSTRAP_ADMIN_LAST_NAME = 'Test'
        BOOTSTRAP_ADMIN_PHONE_NUMBER = ''
        BOOTSTRAP_ADMIN_MUST_CHANGE_PASSWORD = 'false'
        REDMINE_ENABLED = 'false'
        REDMINE_API_KEY = New-Secret
        REDMINE_ADMIN_USERNAME = "${Name}-${EnvironmentLabel}-redmine-admin"
        REDMINE_ADMIN_PASSWORD = New-Secret
        REDMINE_PROJECT_IDENTIFIER = "${Name}-demo"
        REDMINE_TRACKER_ID = '1'
        REDMINE_STATUS_ID = '1'
        REDMINE_POLL_INTERVAL_SECONDS = '15'
        REDMINE_WARNING_PRIORITY_ID = '3'
        REDMINE_MAJOR_PRIORITY_ID = '4'
        REDMINE_CRITICAL_PRIORITY_ID = '5'
        AUTH_HOST = $AuthHost
        CORE_HOST = $CoreHost
        AGENT_CLIENT_IDENTIFIER = "${Name}-${EnvironmentLabel}-agent"
        AGENT_CLIENT_SECRET = New-Secret
    }
}

$development = New-ApplicationValues `
    -SqlHost 'localhost' -RedisHost 'localhost' -RedmineHost 'localhost' `
    -AuthHost 'localhost' -CoreHost 'localhost' -EnvironmentLabel 'Development'
$development['SQL_ACCEPT_EULA'] = 'Y'
$development['SQL_EDITION'] = 'Developer'
$development['SQL_HOST_PORT'] = '1433'
$development['SQL_TEST_AUTH_DATABASE'] = "${databasePrefix}DevelopmentTestAuthDb"
$development['SQL_TEST_CORE_DATABASE'] = "${databasePrefix}DevelopmentTestCoreDb"
$development['REDIS_HOST_PORT'] = '6379'
$development['REDMINE_DB_HOST'] = 'srm-redmine-db'
$development['REDMINE_DB_NAME'] = "${databasePrefix}DevelopmentRedmineDb"
$development['REDMINE_DB_USERNAME'] = "${Name}-development-redmine"
$development['REDMINE_DB_PASSWORD'] = New-Secret
$development['REDMINE_HOST_PORT'] = '3000'
$development['AUTH_PORT'] = '5141'
$development['CORE_PORT'] = '5140'
$development['SHELLY_PORT'] = '5000'
$development['SHELLY1_HOST_PORT'] = '5000'
$development['SHELLY2_HOST_PORT'] = '5001'
$development['SHELLY3_HOST_PORT'] = '5002'

$local = New-ApplicationValues `
    -SqlHost 'srm-sqlserver' -RedisHost 'srm-redis' -RedmineHost 'srm-redmine' `
    -AuthHost 'srm-auth' -CoreHost 'srm-core' -EnvironmentLabel 'Local'
$local['SQL_ACCEPT_EULA'] = 'Y'
$local['SQL_EDITION'] = 'Developer'
$local['SQL_HOST_PORT'] = '1433'
$local['REDIS_HOST_PORT'] = '6379'
$local['REDMINE_DB_HOST'] = 'srm-redmine-db'
$local['REDMINE_DB_NAME'] = "${databasePrefix}LocalRedmineDb"
$local['REDMINE_DB_USERNAME'] = "${Name}-local-redmine"
$local['REDMINE_DB_PASSWORD'] = New-Secret
$local['REDMINE_HOST_PORT'] = '3000'
$local['DOTNET_ENVIRONMENT'] = 'Production'
$local['DOTNET_HTTP_PORTS'] = '8080'
$local['AUTH_HOST_PORT'] = '7031'
$local['CORE_HOST_PORT'] = '7030'
$local['APP_HOST_PORT'] = '7001'
$local['AGENT_HOST_PORT'] = '7032'
$local['SHELLY_PORT'] = '5000'
$local['SHELLY1_HOST_PORT'] = '5000'
$local['SHELLY2_HOST_PORT'] = '5001'
$local['SHELLY3_HOST_PORT'] = '5002'

$azure = New-ApplicationValues `
    -SqlHost "${Name}-sql" -RedisHost "${Name}-redis" -RedmineHost "${Name}-redmine" `
    -AuthHost "${Name}-auth" -CoreHost "${Name}-core" -EnvironmentLabel 'Azure'
$azure['SQL_ACCEPT_EULA'] = 'Y'
$azure['SQL_EDITION'] = 'Developer'
$azure['REDMINE_DB_HOST'] = "${Name}-redmine-db"
$azure['REDMINE_DB_PORT'] = '5432'
$azure['REDMINE_DB_NAME'] = "${databasePrefix}AzureRedmineDb"
$azure['REDMINE_DB_USERNAME'] = "${Name}-azure-redmine"
$azure['REDMINE_DB_PASSWORD'] = New-Secret
$azure['APP_HOST'] = "${Name}-app"
$azure['AGENT_HOST'] = "${Name}-agent"
$azure['SHELLY_HOST'] = "${Name}-shelly"
$azure['SHELLY_PORT'] = '5000'
$azure['DOTNET_ENVIRONMENT'] = 'Production'
$azure['DOTNET_HTTP_PORTS'] = '8080'

$azureParametersPath = Join-Path $PSScriptRoot 'azure.parameters.json'
$targetPaths = @(
    (Join-Path $PSScriptRoot '.env.development'),
    (Join-Path $PSScriptRoot '.env.local'),
    (Join-Path $PSScriptRoot '.env.azure'),
    $azureParametersPath
)
if (-not $Force) {
    $existingTarget = $targetPaths | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if ($existingTarget) {
        throw "Refusing to overwrite $existingTarget. Use -Force only when rotating the complete environment."
    }
}

Write-EnvironmentFile -Path (Join-Path $PSScriptRoot '.env.development') -Values $development
Write-EnvironmentFile -Path (Join-Path $PSScriptRoot '.env.local') -Values $local
Write-EnvironmentFile -Path (Join-Path $PSScriptRoot '.env.azure') -Values $azure

$azureParameters = [ordered]@{
    '$schema' = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
    contentVersion = '1.0.0.0'
    resourceGroup = $Name
    parameters = [ordered]@{
        location = @{ value = $Location }
        prefix = @{ value = $Name }
        containerAppsEnvironmentName = @{ value = '' }
        containerAppsEnvironmentResourceGroup = @{ value = '' }
        registryName = @{ value = ($Name -replace '-', '') + $uniqueSuffix }
        storageAccountName = @{ value = ($Name -replace '-', '') + $uniqueSuffix }
        deployAgent = @{ value = $true }
    }
}
$azureParametersJson = $azureParameters | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText($azureParametersPath, $azureParametersJson, [System.Text.UTF8Encoding]::new($false))

Write-Host "Created isolated configuration for '$Name'. Values were written only to ignored files." -ForegroundColor Green
