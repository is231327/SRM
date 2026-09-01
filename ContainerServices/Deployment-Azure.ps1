param(
    [string]$SettingsPath = (Join-Path $PSScriptRoot 'azure.parameters.json'),
    [string]$SecretsPath = (Join-Path $PSScriptRoot '.env.azure'),
    [Parameter(Mandatory)][string]$ImageTag,
    [string]$SourcePath = (Split-Path -Parent $PSScriptRoot),
    [switch]$ConfigurationOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Deployment.Library.ps1')

function Initialize-AzureRedmine {
    param(
        [Parameter(Mandatory)][string]$SettingsPath,
        [Parameter(Mandatory)][string]$SecretsPath
    )

    $redmineSettings = Get-Content -LiteralPath $SettingsPath -Raw | ConvertFrom-Json
    $configuration = Read-KeyValueFile -Path $SecretsPath
    foreach ($key in @('REDMINE_HOST', 'REDMINE_ADMIN_USERNAME', 'REDMINE_ADMIN_PASSWORD', 'REDMINE_PROJECT_IDENTIFIER')) {
        [void](Get-RequiredValue -Values $configuration -Key $key)
    }

    $resourceGroup = [string]$redmineSettings.resourceGroup
    $redmineName = [string]$configuration['REDMINE_HOST']
    $defaultsLoaded = $false
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        & az containerapp exec --name $redmineName --resource-group $resourceGroup `
            --command 'bin/rake redmine:load_default_data RAILS_ENV=production REDMINE_LANG=en'
        if ($LASTEXITCODE -eq 0) {
            $defaultsLoaded = $true
            break
        }
        if ($attempt -lt 30) {
            Write-Host "Redmine is not ready yet (attempt $attempt/30); retrying in 10 seconds..." -ForegroundColor Yellow
            Start-Sleep -Seconds 10
        }
    }
    if (-not $defaultsLoaded) { throw 'Redmine default-data initialization failed after 30 attempts.' }

    $command = 'bundle exec rails runner /usr/src/redmine/srm_initialize.rb'
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $initializationOutput = & az containerapp exec --name $redmineName --resource-group $resourceGroup --command $command 2>&1
    $initializationExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    $outputText = $initializationOutput -join "`n"
    if ($initializationExitCode -ne 0) {
        $safeOutput = [regex]::Replace($outputText, 'SRM_TOKEN=[A-Za-z0-9]+', 'SRM_TOKEN=[redacted]')
        throw "Redmine administrator, project, or API-key initialization failed. Azure output: $safeOutput"
    }
    $match = [regex]::Match($outputText, 'SRM_TOKEN=([A-Za-z0-9]+)')
    if (-not $match.Success) {
        $safeOutput = [regex]::Replace($outputText, 'SRM_TOKEN=[A-Za-z0-9]+', 'SRM_TOKEN=[redacted]')
        throw "Redmine did not return its generated API key. Azure output: $safeOutput"
    }
    $apiKey = $match.Groups[1].Value

    $lines = foreach ($line in Get-Content -LiteralPath $SecretsPath) {
        if ($line -match '^REDMINE_ENABLED=') { 'REDMINE_ENABLED=true' }
        elseif ($line -match '^REDMINE_API_KEY=') { "REDMINE_API_KEY=$apiKey" }
        else { $line }
    }
    [System.IO.File]::WriteAllLines(
        (Resolve-Path -LiteralPath $SecretsPath),
        [string[]]$lines,
        [System.Text.UTF8Encoding]::new($false))

    Write-Host 'Azure Redmine defaults, administrator, REST API, and project are configured; the private runtime file now contains its generated API key.' -ForegroundColor Green
}

Assert-CommandAvailable -Name 'az'

if ($ImageTag -notmatch '^[A-Za-z0-9][A-Za-z0-9_.-]{0,127}$') {
    throw "ImageTag '$ImageTag' is not a valid container image tag."
}
if (-not (Test-Path -LiteralPath $SettingsPath)) { throw "Missing $SettingsPath. Copy azure.parameters.example.json first." }
if (-not (Test-Path -LiteralPath $SourcePath)) { throw "Source path not found: $SourcePath" }

$settingsJson = Get-Content -LiteralPath $SettingsPath -Raw
if ($settingsJson -match 'replace-with-') { throw "$SettingsPath still contains placeholder values." }
$settings = $settingsJson | ConvertFrom-Json
if ($null -eq $settings.resourceGroup -or [string]::IsNullOrWhiteSpace([string]$settings.resourceGroup)) {
    throw "The root 'resourceGroup' property is required in $SettingsPath."
}
$resourceGroup = [string]$settings.resourceGroup
$runtimeConfiguration = Read-KeyValueFile -Path $SecretsPath
$requiredRuntimeKeys = @(
    'SQL_ACCEPT_EULA', 'SQL_EDITION', 'SQL_HOST', 'SQL_PORT', 'SQL_USERNAME', 'SQL_PASSWORD',
    'SQL_AUTH_DATABASE', 'SQL_CORE_DATABASE', 'REDIS_HOST', 'REDIS_PORT', 'REDIS_PASSWORD',
    'REDMINE_DB_HOST', 'REDMINE_DB_PORT', 'REDMINE_DB_NAME', 'REDMINE_DB_USERNAME', 'REDMINE_DB_PASSWORD',
    'REDMINE_HOST', 'REDMINE_PORT', 'AUTH_HOST', 'CORE_HOST', 'APP_HOST', 'AGENT_HOST', 'SHELLY_HOST', 'SHELLY_PORT',
    'DOTNET_ENVIRONMENT', 'DOTNET_HTTP_PORTS',
    'JWT_ISSUER', 'JWT_AUDIENCE', 'JWT_SIGNING_KEY', 'JWT_ACCESS_TOKEN_LIFETIME_MINUTES',
    'BOOTSTRAP_ADMIN_USERNAME', 'BOOTSTRAP_ADMIN_EMAIL', 'BOOTSTRAP_ADMIN_PASSWORD',
    'BOOTSTRAP_ADMIN_FIRST_NAME', 'BOOTSTRAP_ADMIN_LAST_NAME', 'BOOTSTRAP_ADMIN_PHONE_NUMBER',
    'BOOTSTRAP_ADMIN_MUST_CHANGE_PASSWORD',
    'REDMINE_ENABLED', 'REDMINE_API_KEY', 'REDMINE_ADMIN_USERNAME', 'REDMINE_ADMIN_PASSWORD',
    'REDMINE_PROJECT_IDENTIFIER', 'REDMINE_TRACKER_ID',
    'REDMINE_STATUS_ID', 'REDMINE_POLL_INTERVAL_SECONDS', 'REDMINE_WARNING_PRIORITY_ID',
    'REDMINE_MAJOR_PRIORITY_ID', 'REDMINE_CRITICAL_PRIORITY_ID',
    'AGENT_CLIENT_IDENTIFIER', 'AGENT_CLIENT_SECRET'
)
$optionalRuntimeKeys = @()

foreach ($key in $requiredRuntimeKeys) {
    if ($key -eq 'BOOTSTRAP_ADMIN_PHONE_NUMBER') {
        if (-not $runtimeConfiguration.ContainsKey($key)) { throw "Missing required key '$key'." }
    } else {
        [void](Get-RequiredValue -Values $runtimeConfiguration -Key $key)
    }
}
foreach ($key in $runtimeConfiguration.Keys) {
    if ($key -notin ($requiredRuntimeKeys + $optionalRuntimeKeys)) { throw "Unknown key '$key' in $SecretsPath." }
}

$jwtSigningKey = [string]$runtimeConfiguration['JWT_SIGNING_KEY']
if ($jwtSigningKey.Length -lt 32) {
    throw 'JWT_SIGNING_KEY must contain at least 32 characters.'
}

try {
    Invoke-ExternalCommand -FilePath 'az' -Arguments @('account', 'show', '--output', 'none') -ErrorMessage 'Azure CLI is not authenticated.'
}
catch {
    Write-Host 'Azure login is required.' -ForegroundColor Yellow
    Invoke-ExternalCommand -FilePath 'az' -Arguments @('login') -ErrorMessage 'Azure login failed.'
}

$location = [string]$settings.parameters.location.value
$registryName = [string]$settings.parameters.registryName.value
$storageAccountName = [string]$settings.parameters.storageAccountName.value
$environmentName = [string]$settings.parameters.containerAppsEnvironmentName.value
$environmentResourceGroup = [string]$settings.parameters.containerAppsEnvironmentResourceGroup.value
if ([string]::IsNullOrWhiteSpace($location) -or [string]::IsNullOrWhiteSpace($registryName) -or
    [string]::IsNullOrWhiteSpace($storageAccountName)) {
    throw 'The Azure settings must include location, registryName, and storageAccountName parameters.'
}
$redisPassword = [string]$runtimeConfiguration['REDIS_PASSWORD']
if ($redisPassword.Length -lt 16 -or $redisPassword.Contains(',')) {
    throw 'REDIS_PASSWORD must contain at least 16 characters and must not contain a comma.'
}
if ([string]::IsNullOrWhiteSpace($environmentName) -ne [string]::IsNullOrWhiteSpace($environmentResourceGroup)) {
    throw 'containerAppsEnvironmentName and containerAppsEnvironmentResourceGroup must either both be set or both be empty.'
}
if (-not [string]::IsNullOrWhiteSpace($environmentName)) {
    $existingEnvironmentLocation = & az containerapp env show `
        --name $environmentName --resource-group $environmentResourceGroup --query location --output tsv
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace([string]$existingEnvironmentLocation)) {
        throw 'The configured existing Container Apps environment could not be read.'
    }
    if ([string]$existingEnvironmentLocation -ne $location) {
        throw "The deployment location must match the existing Container Apps environment location."
    }
}

if ($ConfigurationOnly) {
    Invoke-ExternalCommand -FilePath 'az' -Arguments @(
        'group', 'show', '--name', $resourceGroup, '--output', 'none'
    ) -ErrorMessage "ConfigurationOnly requires the existing resource group '$resourceGroup'."

    $existingAppKeys = @('SQL_HOST', 'REDIS_HOST', 'REDMINE_DB_HOST', 'REDMINE_HOST', 'AUTH_HOST', 'CORE_HOST', 'APP_HOST')
    if ([bool]$settings.parameters.deployAgent.value) { $existingAppKeys += 'AGENT_HOST' }
    foreach ($appKey in $existingAppKeys) {
        $appName = [string]$runtimeConfiguration[$appKey]
        Invoke-ExternalCommand -FilePath 'az' -Arguments @(
            'containerapp', 'show', '--name', $appName, '--resource-group', $resourceGroup, '--output', 'none'
        ) -ErrorMessage "ConfigurationOnly requires existing Container App '$appName'."
    }
}
else {
    Invoke-ExternalCommand -FilePath 'az' -Arguments @(
        'group', 'create', '--name', $resourceGroup, '--location', $location, '--output', 'none'
    ) -ErrorMessage 'Resource group creation failed.'
}

function Invoke-InfrastructureDeployment {
    param([bool]$DeployApplications)

    $parameterValues = [ordered]@{}
    foreach ($property in $settings.parameters.PSObject.Properties) {
        if ($property.Name -eq 'redmineEnabled') {
            Write-Warning "Ignoring obsolete azure.parameters.json property 'redmineEnabled'; use REDMINE_ENABLED in .env.azure."
            continue
        }
        $parameterValues[$property.Name] = $property.Value
    }
    $parameterValues['imageTag'] = @{ value = $ImageTag }
    $parameterValues['deployApplications'] = @{ value = $DeployApplications }
    $parameterValues['runtimeConfiguration'] = @{ value = $runtimeConfiguration }

    $document = [ordered]@{
        '$schema' = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
        contentVersion = '1.0.0.0'
        parameters = $parameterValues
    }

    $temporaryFile = [System.IO.Path]::GetTempFileName()
    try {
        $document | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $temporaryFile -Encoding UTF8
        $deploymentName = 'srm-{0}-{1}' -f (Get-Date -Format 'yyyyMMddHHmmss'), $(if ($DeployApplications) { 'full' } else { 'foundation' })
        Invoke-ExternalCommand -FilePath 'az' -Arguments @(
            'deployment', 'group', 'create',
            '--name', $deploymentName,
            '--resource-group', $resourceGroup,
            '--template-file', (Join-Path $PSScriptRoot 'azure/main.bicep'),
            '--parameters', "@$temporaryFile",
            '--output', 'table'
        ) -ErrorMessage "Azure deployment '$deploymentName' failed."
    }
    finally {
        Remove-Item -LiteralPath $temporaryFile -Force -ErrorAction SilentlyContinue
    }
}

if (-not $ConfigurationOnly) {
    Write-Host 'Provisioning the Azure foundation...' -ForegroundColor Cyan
    Invoke-InfrastructureDeployment -DeployApplications $false

    if (-not [string]::IsNullOrWhiteSpace($environmentName)) {
        Write-Host 'Attaching isolated Azure Files shares to the existing Container Apps environment...' -ForegroundColor Cyan
        $storageAccountKey = & az storage account keys list `
            --account-name $storageAccountName --resource-group $resourceGroup --query '[0].value' --output tsv
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace([string]$storageAccountKey)) {
            throw 'Could not obtain the new storage account key.'
        }

        $prefix = [string]$settings.parameters.prefix.value
        foreach ($mapping in @(
            @{ StorageName = "$prefix-sql-storage"; ShareName = 'sqlserver' },
            @{ StorageName = "$prefix-redis-storage"; ShareName = 'redis' },
            @{ StorageName = "$prefix-redmine-storage"; ShareName = 'redmine' }
        )) {
            Invoke-ExternalCommand -FilePath 'az' -Arguments @(
                'containerapp', 'env', 'storage', 'set',
                '--name', $environmentName,
                '--resource-group', $environmentResourceGroup,
                '--storage-name', $mapping.StorageName,
                '--azure-file-account-name', $storageAccountName,
                '--azure-file-account-key', [string]$storageAccountKey,
                '--azure-file-share-name', $mapping.ShareName,
                '--access-mode', 'ReadWrite',
                '--output', 'none'
            ) -ErrorMessage "Could not attach environment storage '$($mapping.StorageName)'."
        }
        $storageAccountKey = $null
    }

    $images = @(
        @{ Name = 'srm-auth'; Dockerfile = 'SRMAuth/Dockerfile' },
        @{ Name = 'srm-core'; Dockerfile = 'SRMCore/Dockerfile' },
        @{ Name = 'srm-app'; Dockerfile = 'SRMApp/Dockerfile' },
        @{ Name = 'srm-redmine'; Dockerfile = 'SRMRedmine/Dockerfile' }
    )
    if ([bool]$settings.parameters.deployAgent.value) {
        $images += @{ Name = 'srm-agent'; Dockerfile = 'SRMAgent/Dockerfile' }
    }

    foreach ($image in $images) {
        Write-Host "Building $($image.Name):$ImageTag in ACR..." -ForegroundColor Cyan
        Invoke-ExternalCommand -FilePath 'az' -Arguments @(
            'acr', 'build', '--registry', $registryName,
            '--image', "$($image.Name):$ImageTag",
            '--file', $image.Dockerfile,
            $SourcePath
        ) -ErrorMessage "ACR build failed for $($image.Name)."
    }
}

if ($ConfigurationOnly) {
    Write-Host "Applying Azure runtime configuration with existing image tag '$ImageTag'..." -ForegroundColor Cyan
}
else {
    Write-Host "Deploying Container Apps with immutable image tag '$ImageTag'..." -ForegroundColor Cyan
}
Invoke-InfrastructureDeployment -DeployApplications $true

if (-not $ConfigurationOnly -and [string]$runtimeConfiguration['REDMINE_ENABLED'] -ne 'true') {
    Write-Host 'Initializing the fresh Azure Redmine instance...' -ForegroundColor Cyan
    Initialize-AzureRedmine `
        -SettingsPath $SettingsPath `
        -SecretsPath $SecretsPath

    $runtimeConfiguration = Read-KeyValueFile -Path $SecretsPath
    if ([string]$runtimeConfiguration['REDMINE_ENABLED'] -ne 'true') {
        throw 'Redmine initialization did not enable the integration in the private Azure configuration.'
    }

    Write-Host 'Applying the generated Redmine API key to Core...' -ForegroundColor Cyan
    Invoke-InfrastructureDeployment -DeployApplications $true
}
if ($ConfigurationOnly) {
    Write-Host 'Azure configuration update completed without building images or initializing Redmine.' -ForegroundColor Green
}
else {
    Write-Host 'Azure deployment completed.' -ForegroundColor Green
}
