param(
    [ValidateSet('Infrastructure', 'Full', 'Simulators')]
    [string]$Mode = 'Infrastructure',
    [ValidatePattern('^[a-z0-9][a-z0-9_-]+$')]
    [string]$ProjectName = 'srm',
    [switch]$Build,
    [switch]$PrepareOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Deployment.Library.ps1')

function Initialize-LocalRedmine {
    param(
        [Parameter(Mandatory)][string]$ProjectName,
        [Parameter(Mandatory)][ValidateSet('Infrastructure', 'Full', 'Simulators')][string]$Mode,
        [Parameter(Mandatory)][string]$ConfigurationPath,
        [Parameter(Mandatory)][string]$RuntimePath
    )

    $configuration = Read-KeyValueFile -Path $ConfigurationPath
    $missingAdministratorValues = -not $configuration.ContainsKey('REDMINE_ADMIN_USERNAME') -or
        -not $configuration.ContainsKey('REDMINE_ADMIN_PASSWORD')
    if ($missingAdministratorValues) {
        $bytes = [byte[]]::new(24)
        $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
        try { $generator.GetBytes($bytes) } finally { $generator.Dispose() }
        $generatedPassword = 'Aa9!' + ([BitConverter]::ToString($bytes) -replace '-', '').ToLowerInvariant()
        $existingLines = [System.Collections.Generic.List[string]]::new()
        $existingLines.AddRange([string[]](Get-Content -LiteralPath $ConfigurationPath))
        if (-not $configuration.ContainsKey('REDMINE_ADMIN_USERNAME')) {
            $existingLines.Add("REDMINE_ADMIN_USERNAME=$ProjectName-redmine-admin")
        }
        if (-not $configuration.ContainsKey('REDMINE_ADMIN_PASSWORD')) {
            $existingLines.Add("REDMINE_ADMIN_PASSWORD=$generatedPassword")
        }
        [System.IO.File]::WriteAllLines(
            (Resolve-Path -LiteralPath $ConfigurationPath),
            $existingLines,
            [System.Text.UTF8Encoding]::new($false))
        $configuration = Read-KeyValueFile -Path $ConfigurationPath
    }
    foreach ($key in @('REDMINE_ADMIN_USERNAME', 'REDMINE_ADMIN_PASSWORD', 'REDMINE_PROJECT_IDENTIFIER')) {
        [void](Get-RequiredValue -Values $configuration -Key $key)
    }

    $containerName = "$ProjectName-srm-redmine-1"
    & docker inspect $containerName *> $null
    if ($LASTEXITCODE -ne 0) { throw "Redmine container '$containerName' is not running." }

    $defaultsLoaded = $false
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        & docker exec --env REDMINE_LANG=en --env RAILS_ENV=production $containerName bin/rake redmine:load_default_data
        if ($LASTEXITCODE -eq 0) {
            $defaultsLoaded = $true
            break
        }
        if ($attempt -lt 30) {
            Write-Host "Redmine is not ready yet (attempt $attempt/30); retrying in 5 seconds..." -ForegroundColor Yellow
            Start-Sleep -Seconds 5
        }
    }
    if (-not $defaultsLoaded) { throw 'Redmine default-data initialization failed after 30 attempts.' }

    $railsCode = @'
Setting.rest_api_enabled = '1'
admin = User.where(admin: true).order(:id).first!
admin.login = ENV.fetch('ADMIN_USERNAME')
admin.password = ENV.fetch('ADMIN_PASSWORD')
admin.password_confirmation = ENV.fetch('ADMIN_PASSWORD')
admin.must_change_passwd = false
admin.save!
project = Project.find_or_initialize_by(identifier: ENV.fetch('PROJECT_IDENTIFIER'))
project.name = ENV.fetch('PROJECT_IDENTIFIER')
project.is_public = false
project.save!
project.trackers = Tracker.all
token = Token.where(user: admin, action: 'api').first_or_create!
puts token.value
'@

    $apiKeyOutput = & docker exec `
        --env "ADMIN_USERNAME=$($configuration['REDMINE_ADMIN_USERNAME'])" `
        --env "ADMIN_PASSWORD=$($configuration['REDMINE_ADMIN_PASSWORD'])" `
        --env "PROJECT_IDENTIFIER=$($configuration['REDMINE_PROJECT_IDENTIFIER'])" `
        $containerName bundle exec rails runner $railsCode
    if ($LASTEXITCODE -ne 0) { throw 'Redmine administrator, project, or API-key initialization failed.' }
    $apiKey = [string]($apiKeyOutput | Select-Object -Last 1)
    if ([string]::IsNullOrWhiteSpace($apiKey)) { throw 'Redmine did not generate an API key.' }

    $lines = foreach ($line in Get-Content -LiteralPath $ConfigurationPath) {
        if ($line -match '^REDMINE_ENABLED=') { 'REDMINE_ENABLED=true' }
        elseif ($line -match '^REDMINE_API_KEY=') { "REDMINE_API_KEY=$apiKey" }
        else { $line }
    }
    [System.IO.File]::WriteAllLines(
        (Resolve-Path -LiteralPath $ConfigurationPath),
        [string[]]$lines,
        [System.Text.UTF8Encoding]::new($false))

    if ($Mode -eq 'Full') {
        $coreEnvironmentPath = Join-Path $RuntimePath 'srm-core.env'
        if (-not (Test-Path -LiteralPath $coreEnvironmentPath)) {
            throw "Generated Core environment file not found: $coreEnvironmentPath"
        }
        $enabledUpdated = $false
        $apiKeyUpdated = $false
        $coreLines = foreach ($line in Get-Content -LiteralPath $coreEnvironmentPath) {
            if ($line -match '^Redmine__Enabled=') {
                $enabledUpdated = $true
                'Redmine__Enabled=true'
            }
            elseif ($line -match '^Redmine__ApiKey=') {
                $apiKeyUpdated = $true
                "Redmine__ApiKey=$apiKey"
            }
            else { $line }
        }
        if (-not $enabledUpdated -or -not $apiKeyUpdated) {
            throw 'Generated Core environment does not contain the expected Redmine settings.'
        }
        [System.IO.File]::WriteAllLines(
            $coreEnvironmentPath,
            [string[]]$coreLines,
            [System.Text.UTF8Encoding]::new($false))

        Push-Location $PSScriptRoot
        try {
            & docker compose --project-name $ProjectName `
                --file docker-compose.yml --file .runtime-env/docker-compose.runtime.yml `
                --profile apps --profile simulators up --detach --no-deps --force-recreate srm-core
            if ($LASTEXITCODE -ne 0) { throw 'Core recreation with Redmine configuration failed.' }
        }
        finally {
            Pop-Location
        }
    }

    Write-Host 'Local Redmine defaults, administrator, REST API, project, and integration configuration are ready.' -ForegroundColor Green
}

function Test-LocalRedmineInitialized {
    param(
        [Parameter(Mandatory)][string]$ProjectName,
        [Parameter(Mandatory)][hashtable]$Configuration
    )

    if ([string]$Configuration['REDMINE_ENABLED'] -ne 'true' -or
        [string]::IsNullOrWhiteSpace([string]$Configuration['REDMINE_API_KEY'])) {
        return $false
    }

    $containerName = "$ProjectName-srm-redmine-1"
    $railsCode = @'
project = Project.find_by(identifier: ENV.fetch('PROJECT_IDENTIFIER'))
admin = User.where(admin: true).order(:id).first
token = admin && Token.find_by(user: admin, action: 'api')
exit(project && token && token.value == ENV.fetch('EXPECTED_API_KEY') ? 0 : 1)
'@

    & docker exec `
        --env "PROJECT_IDENTIFIER=$($Configuration['REDMINE_PROJECT_IDENTIFIER'])" `
        --env "EXPECTED_API_KEY=$($Configuration['REDMINE_API_KEY'])" `
        $containerName bundle exec rails runner $railsCode *> $null
    return $LASTEXITCODE -eq 0
}

$configurationFileName = if ($Mode -eq 'Full') { '.env.local' } else { '.env.development' }
$exampleFileName = "$configurationFileName.example"
$envPath = Join-Path $PSScriptRoot $configurationFileName
if (-not (Test-Path -LiteralPath $envPath)) {
    throw "Missing $envPath. Copy $exampleFileName to $configurationFileName and replace every placeholder first."
}

if (-not (Select-String -LiteralPath $envPath -Pattern '^REDIS_PASSWORD=' -Quiet)) {
    $bytes = [byte[]]::new(24)
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generator.GetBytes($bytes) } finally { $generator.Dispose() }
    $redisPassword = 'Aa9!' + ([BitConverter]::ToString($bytes) -replace '-', '').ToLowerInvariant()
    [System.IO.File]::AppendAllLines(
        (Resolve-Path -LiteralPath $envPath),
        [string[]]@("REDIS_PASSWORD=$redisPassword"),
        [System.Text.UTF8Encoding]::new($false))
    Write-Host "Generated the missing Redis password in $configurationFileName." -ForegroundColor Yellow
}

if (Select-String -LiteralPath $envPath -Pattern 'replace-with-' -SimpleMatch) {
    throw "ContainerServices/$configurationFileName still contains placeholder values."
}

$values = Read-KeyValueFile -Path $envPath
$runtimePath = Join-Path $PSScriptRoot '.runtime-env'
[System.IO.Directory]::CreateDirectory($runtimePath) | Out-Null

function Write-ServiceEnvironmentFile {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][System.Collections.IDictionary]$Variables,
        [string[]]$OptionalKeys = @()
    )

    $lines = foreach ($outputKey in $Variables.Keys) {
        $sourceKey = [string]$Variables[$outputKey]
        if (-not $values.ContainsKey($sourceKey)) {
            throw "Missing required key '$sourceKey' in $envPath."
        }

        $value = [string]$values[$sourceKey]
        if ([string]::IsNullOrWhiteSpace($value) -and $sourceKey -notin $OptionalKeys) {
            throw "Key '$sourceKey' must not be empty in $envPath."
        }
        if ($value.Contains("`r") -or $value.Contains("`n")) {
            throw "Key '$sourceKey' contains a newline, which is not supported in a container environment file."
        }

        "$outputKey=$value"
    }

    $target = Join-Path $runtimePath "$Name.env"
    [System.IO.File]::WriteAllLines($target, [string[]]$lines, [System.Text.UTF8Encoding]::new($false))
}

function Get-PortValue {
    param([Parameter(Mandatory)][string]$Key)
    $value = Get-RequiredValue -Values $values -Key $Key
    $port = 0
    if (-not [int]::TryParse($value, [ref]$port) -or $port -lt 1 -or $port -gt 65535) {
        throw "Key '$Key' must be an integer from 1 through 65535."
    }
    return $port
}

$redisHost = Get-RequiredValue -Values $values -Key 'REDIS_HOST'
$redisPort = Get-PortValue -Key 'REDIS_PORT'
$redisPassword = Get-RequiredValue -Values $values -Key 'REDIS_PASSWORD'
if ($redisPassword.Length -lt 16 -or $redisPassword.Contains(',')) {
    throw 'REDIS_PASSWORD must contain at least 16 characters and must not contain a comma.'
}
$values['REDIS_CONNECTION_STRING'] = "${redisHost}:${redisPort},password=${redisPassword},abortConnect=false"

if ($Mode -eq 'Full') {
    $servicePort = Get-PortValue -Key 'DOTNET_HTTP_PORTS'
    $redmineHost = Get-RequiredValue -Values $values -Key 'REDMINE_HOST'
    $redminePort = Get-PortValue -Key 'REDMINE_PORT'
    $authHost = Get-RequiredValue -Values $values -Key 'AUTH_HOST'
    $coreHost = Get-RequiredValue -Values $values -Key 'CORE_HOST'
    $values['REDMINE_BASE_URL'] = "http://${redmineHost}:${redminePort}/"
    $values['REDMINE_PUBLIC_BASE_URL'] = "http://localhost:$(Get-PortValue -Key 'REDMINE_HOST_PORT')/"
    $values['AUTH_API_BASE_URL'] = "http://${authHost}:${servicePort}/"
    $values['CORE_API_BASE_URL'] = "http://${coreHost}:${servicePort}/"
    $values['AGENT_AUTH_BASE_URL'] = $values['AUTH_API_BASE_URL']
    $values['AGENT_CORE_BASE_URL'] = $values['CORE_API_BASE_URL']
    $values['DEMO_SHELLY_BASE_URL'] = "http://shelly1:$($values['SHELLY_PORT'])"
}

$overrideLines = @(
    'services:'
    '  srm-sqlserver:'
    "    ports: ['$(Get-PortValue 'SQL_HOST_PORT'):$(Get-PortValue 'SQL_PORT')']"
    '  srm-redis:'
    "    ports: ['$(Get-PortValue 'REDIS_HOST_PORT'):$(Get-PortValue 'REDIS_PORT')']"
    '  srm-redmine:'
    "    ports: ['$(Get-PortValue 'REDMINE_HOST_PORT'):$(Get-PortValue 'REDMINE_PORT')']"
)
if ($Mode -eq 'Full') {
    $overrideLines += @(
        '  srm-auth:'
        "    ports: ['$(Get-PortValue 'AUTH_HOST_PORT'):$(Get-PortValue 'DOTNET_HTTP_PORTS')']"
        '  srm-core:'
        "    ports: ['$(Get-PortValue 'CORE_HOST_PORT'):$(Get-PortValue 'DOTNET_HTTP_PORTS')']"
        '  srm-app:'
        "    ports: ['$(Get-PortValue 'APP_HOST_PORT'):$(Get-PortValue 'DOTNET_HTTP_PORTS')']"
        '  srm-agent:'
        "    ports: ['$(Get-PortValue 'AGENT_HOST_PORT'):$(Get-PortValue 'DOTNET_HTTP_PORTS')']"
    )
}
if ($Mode -in @('Full', 'Simulators')) {
    $overrideLines += @(
        '  shelly1:'
        "    ports: ['$(Get-PortValue 'SHELLY1_HOST_PORT'):$(Get-PortValue 'SHELLY_PORT')']"
        '  shelly2:'
        "    ports: ['$(Get-PortValue 'SHELLY2_HOST_PORT'):$(Get-PortValue 'SHELLY_PORT')']"
        '  shelly3:'
        "    ports: ['$(Get-PortValue 'SHELLY3_HOST_PORT'):$(Get-PortValue 'SHELLY_PORT')']"
    )
}
[System.IO.File]::WriteAllLines(
    (Join-Path $runtimePath 'docker-compose.runtime.yml'),
    [string[]]$overrideLines,
    [System.Text.UTF8Encoding]::new($false))

Write-ServiceEnvironmentFile -Name 'srm-sqlserver' -Variables ([ordered]@{
    ACCEPT_EULA = 'SQL_ACCEPT_EULA'; MSSQL_PID = 'SQL_EDITION'; MSSQL_SA_PASSWORD = 'SQL_PASSWORD'
    SQL_HEALTH_USERNAME = 'SQL_USERNAME'
})
Write-ServiceEnvironmentFile -Name 'srm-redmine-db' -Variables ([ordered]@{
    POSTGRES_DB = 'REDMINE_DB_NAME'; POSTGRES_USER = 'REDMINE_DB_USERNAME'; POSTGRES_PASSWORD = 'REDMINE_DB_PASSWORD'
})
Write-ServiceEnvironmentFile -Name 'srm-redmine' -Variables ([ordered]@{
    REDMINE_DB_POSTGRES = 'REDMINE_DB_HOST'; REDMINE_DB_DATABASE = 'REDMINE_DB_NAME'
    REDMINE_DB_USERNAME = 'REDMINE_DB_USERNAME'; REDMINE_DB_PASSWORD = 'REDMINE_DB_PASSWORD'
})
Write-ServiceEnvironmentFile -Name 'srm-redis' -Variables ([ordered]@{
    REDIS_PASSWORD = 'REDIS_PASSWORD'
})
if ($Mode -eq 'Full') {
Write-ServiceEnvironmentFile -Name 'srm-auth' -Variables ([ordered]@{
    ASPNETCORE_ENVIRONMENT = 'DOTNET_ENVIRONMENT'; ASPNETCORE_HTTP_PORTS = 'DOTNET_HTTP_PORTS'
    SqlServer__Host = 'SQL_HOST'; SqlServer__Port = 'SQL_PORT'; SqlServer__Username = 'SQL_USERNAME'
    SqlServer__Password = 'SQL_PASSWORD'; SqlServer__AuthDatabase = 'SQL_AUTH_DATABASE'
    Redis__ConnectionString = 'REDIS_CONNECTION_STRING'
    Jwt__Issuer = 'JWT_ISSUER'; Jwt__Audience = 'JWT_AUDIENCE'; Jwt__SigningKey = 'JWT_SIGNING_KEY'
    Jwt__AccessTokenLifetimeMinutes = 'JWT_ACCESS_TOKEN_LIFETIME_MINUTES'
    BootstrapAdmin__Username = 'BOOTSTRAP_ADMIN_USERNAME'; BootstrapAdmin__Email = 'BOOTSTRAP_ADMIN_EMAIL'
    BootstrapAdmin__Password = 'BOOTSTRAP_ADMIN_PASSWORD'; BootstrapAdmin__FirstName = 'BOOTSTRAP_ADMIN_FIRST_NAME'
    BootstrapAdmin__LastName = 'BOOTSTRAP_ADMIN_LAST_NAME'; BootstrapAdmin__PhoneNumber = 'BOOTSTRAP_ADMIN_PHONE_NUMBER'
    BootstrapAdmin__MustChangePassword = 'BOOTSTRAP_ADMIN_MUST_CHANGE_PASSWORD'
}) -OptionalKeys @('BOOTSTRAP_ADMIN_PHONE_NUMBER')
Write-ServiceEnvironmentFile -Name 'srm-core' -Variables ([ordered]@{
    ASPNETCORE_ENVIRONMENT = 'DOTNET_ENVIRONMENT'; ASPNETCORE_HTTP_PORTS = 'DOTNET_HTTP_PORTS'
    SqlServer__Host = 'SQL_HOST'; SqlServer__Port = 'SQL_PORT'; SqlServer__Username = 'SQL_USERNAME'
    SqlServer__Password = 'SQL_PASSWORD'; SqlServer__CoreDatabase = 'SQL_CORE_DATABASE'
    Redis__ConnectionString = 'REDIS_CONNECTION_STRING'
    Jwt__Issuer = 'JWT_ISSUER'; Jwt__Audience = 'JWT_AUDIENCE'; Jwt__SigningKey = 'JWT_SIGNING_KEY'
    Redmine__Enabled = 'REDMINE_ENABLED'; Redmine__BaseUrl = 'REDMINE_BASE_URL'
    Redmine__PublicBaseUrl = 'REDMINE_PUBLIC_BASE_URL'; Redmine__ApiKey = 'REDMINE_API_KEY'
    Redmine__ProjectIdentifier = 'REDMINE_PROJECT_IDENTIFIER'; Redmine__TrackerId = 'REDMINE_TRACKER_ID'
    Redmine__StatusId = 'REDMINE_STATUS_ID'; Redmine__PollIntervalSeconds = 'REDMINE_POLL_INTERVAL_SECONDS'
    Redmine__WarningPriorityId = 'REDMINE_WARNING_PRIORITY_ID'; Redmine__MajorPriorityId = 'REDMINE_MAJOR_PRIORITY_ID'
    Redmine__CriticalPriorityId = 'REDMINE_CRITICAL_PRIORITY_ID'
}) -OptionalKeys @('REDMINE_API_KEY')
Write-ServiceEnvironmentFile -Name 'srm-app' -Variables ([ordered]@{
    ASPNETCORE_ENVIRONMENT = 'DOTNET_ENVIRONMENT'; ASPNETCORE_HTTP_PORTS = 'DOTNET_HTTP_PORTS'
    CoreApi__BaseUrl = 'CORE_API_BASE_URL'; AuthApi__BaseUrl = 'AUTH_API_BASE_URL'
    Redis__ConnectionString = 'REDIS_CONNECTION_STRING'
})
Write-ServiceEnvironmentFile -Name 'srm-demo-seeder' -Variables ([ordered]@{
    SQL_HOST = 'SQL_HOST'; SQL_PORT = 'SQL_PORT'; SQL_USERNAME = 'SQL_USERNAME'
    SQL_PASSWORD = 'SQL_PASSWORD'; SQL_CORE_DATABASE = 'SQL_CORE_DATABASE'; SQL_AUTH_DATABASE = 'SQL_AUTH_DATABASE'
    AGENT_CLIENT_IDENTIFIER = 'AGENT_CLIENT_IDENTIFIER'; AGENT_CLIENT_SECRET = 'AGENT_CLIENT_SECRET'
    SHELLY_BASE_URL = 'DEMO_SHELLY_BASE_URL'
})
Write-ServiceEnvironmentFile -Name 'srm-agent' -Variables ([ordered]@{
    ASPNETCORE_ENVIRONMENT = 'DOTNET_ENVIRONMENT'; ASPNETCORE_HTTP_PORTS = 'DOTNET_HTTP_PORTS'
    AgentApi__AuthBaseUrl = 'AGENT_AUTH_BASE_URL'; AgentApi__CoreBaseUrl = 'AGENT_CORE_BASE_URL'
    AgentApi__ClientIdentifier = 'AGENT_CLIENT_IDENTIFIER'; AgentApi__ClientSecret = 'AGENT_CLIENT_SECRET'
})
}

if ($PrepareOnly) {
    Write-Host "Scoped runtime environment files prepared in $runtimePath." -ForegroundColor Green
    return
}

Assert-CommandAvailable -Name 'docker'

$arguments = @(
    'compose', '--project-name', $ProjectName,
    '--file', 'docker-compose.yml', '--file', '.runtime-env/docker-compose.runtime.yml'
)
switch ($Mode) {
    'Full' { $arguments += @('--profile', 'apps', '--profile', 'simulators') }
    'Simulators' { $arguments += @('--profile', 'simulators') }
}
$arguments += @('up', '-d')
if ($Build -or $Mode -ne 'Infrastructure') { $arguments += '--build' }

Push-Location $PSScriptRoot
try {
    & docker @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Docker Compose failed.' }
    & docker compose --project-name $ProjectName --file docker-compose.yml --file .runtime-env/docker-compose.runtime.yml ps
    if ($LASTEXITCODE -ne 0) { throw 'Could not read Docker Compose status.' }
}
finally {
    Pop-Location
}

if (-not (Test-LocalRedmineInitialized -ProjectName $ProjectName -Configuration $values)) {
    Write-Host 'Initializing the fresh local Redmine instance...' -ForegroundColor Cyan
    Initialize-LocalRedmine `
        -ProjectName $ProjectName `
        -Mode $Mode `
        -ConfigurationPath $envPath `
        -RuntimePath $runtimePath
}
