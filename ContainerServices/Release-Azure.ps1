param(
    [Parameter(Mandatory)][string]$ImageTag,
    [string]$SettingsPath = (Join-Path $PSScriptRoot 'azure.parameters.json'),
    [string]$SecretsPath = (Join-Path $PSScriptRoot '.env.azure'),
    [string]$SourcePath = (Split-Path -Parent $PSScriptRoot),
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Deployment.Library.ps1')

Assert-CommandAvailable -Name 'az'

if ($ImageTag -notmatch '^[A-Za-z0-9][A-Za-z0-9_.-]{0,127}$') {
    throw "ImageTag '$ImageTag' is not a valid container image tag."
}
if (-not (Test-Path -LiteralPath $SettingsPath)) { throw "Missing Azure settings file: $SettingsPath" }
if (-not $SkipBuild -and -not (Test-Path -LiteralPath $SourcePath)) { throw "Source path not found: $SourcePath" }

$settingsJson = Get-Content -LiteralPath $SettingsPath -Raw
if ($settingsJson -match 'replace-with-') { throw "$SettingsPath still contains placeholder values." }
$settings = $settingsJson | ConvertFrom-Json
$resourceGroup = [string]$settings.resourceGroup
$registryName = [string]$settings.parameters.registryName.value
$deployAgent = [bool]$settings.parameters.deployAgent.value
if ([string]::IsNullOrWhiteSpace($resourceGroup) -or [string]::IsNullOrWhiteSpace($registryName)) {
    throw 'The Azure settings must include resourceGroup and registryName.'
}

$configuration = Read-KeyValueFile -Path $SecretsPath
$releaseImages = @(
    @{ AppKey = 'AUTH_HOST'; Container = 'auth'; Repository = 'srm-auth'; Dockerfile = 'SRMAuth/Dockerfile' },
    @{ AppKey = 'CORE_HOST'; Container = 'core'; Repository = 'srm-core'; Dockerfile = 'SRMCore/Dockerfile' },
    @{ AppKey = 'APP_HOST'; Container = 'app'; Repository = 'srm-app'; Dockerfile = 'SRMApp/Dockerfile' },
    @{ AppKey = 'REDMINE_HOST'; Container = 'redmine'; Repository = 'srm-redmine'; Dockerfile = 'SRMRedmine/Dockerfile' }
)
if ($deployAgent) {
    $releaseImages += @{ AppKey = 'AGENT_HOST'; Container = 'agent'; Repository = 'srm-agent'; Dockerfile = 'SRMAgent/Dockerfile' }
}

foreach ($releaseImage in $releaseImages) {
    [void](Get-RequiredValue -Values $configuration -Key $releaseImage.AppKey)
}
$redmineEnabled = Get-RequiredValue -Values $configuration -Key 'REDMINE_ENABLED'
if ($redmineEnabled -ne 'true') {
    throw 'The Azure environment has not completed its one-time Redmine initialization. Run Deployment-Azure.ps1 first.'
}

Invoke-ExternalCommand -FilePath 'az' -Arguments @('account', 'show', '--output', 'none') -ErrorMessage 'Azure CLI is not authenticated. Run az login first.'

# Validate every release target before changing any of them.
foreach ($releaseImage in $releaseImages) {
    $appName = [string]$configuration[$releaseImage.AppKey]
    Invoke-ExternalCommand -FilePath 'az' -Arguments @(
        'containerapp', 'show', '--name', $appName, '--resource-group', $resourceGroup, '--output', 'none'
    ) -ErrorMessage "Release target '$appName' does not exist. Run Deployment-Azure.ps1 to create the environment first."
}

if (-not $SkipBuild) {
    foreach ($releaseImage in $releaseImages) {
        Write-Host "Building $($releaseImage.Repository):$ImageTag in ACR..." -ForegroundColor Cyan
        Invoke-ExternalCommand -FilePath 'az' -Arguments @(
            'acr', 'build', '--registry', $registryName,
            '--image', "$($releaseImage.Repository):$ImageTag",
            '--file', $releaseImage.Dockerfile,
            $SourcePath
        ) -ErrorMessage "ACR build failed for $($releaseImage.Repository)."
    }
}

# Verify every immutable image exists before changing the first Container App.
foreach ($releaseImage in $releaseImages) {
    Invoke-ExternalCommand -FilePath 'az' -Arguments @(
        'acr', 'repository', 'show',
        '--name', $registryName,
        '--image', "$($releaseImage.Repository):$ImageTag",
        '--output', 'none'
    ) -ErrorMessage "Image $($releaseImage.Repository):$ImageTag does not exist in ACR. No Container Apps were updated."
}

$registryLoginServer = "$registryName.azurecr.io"
foreach ($releaseImage in $releaseImages) {
    $appName = [string]$configuration[$releaseImage.AppKey]
    $image = "$registryLoginServer/$($releaseImage.Repository):$ImageTag"
    Write-Host "Releasing $image to $appName..." -ForegroundColor Cyan
    Invoke-ExternalCommand -FilePath 'az' -Arguments @(
        'containerapp', 'update',
        '--name', $appName,
        '--resource-group', $resourceGroup,
        '--container-name', $releaseImage.Container,
        '--image', $image,
        '--output', 'none'
    ) -ErrorMessage "Image update failed for Container App '$appName'."

    $ready = $false
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        $stateJson = & az containerapp show --name $appName --resource-group $resourceGroup `
            --query '{latest:properties.latestRevisionName,ready:properties.latestReadyRevisionName}' --output json
        if ($LASTEXITCODE -ne 0) { throw "Could not read release status for '$appName'." }
        $state = ($stateJson | Out-String) | ConvertFrom-Json
        if (-not [string]::IsNullOrWhiteSpace([string]$state.latest) -and $state.latest -eq $state.ready) {
            $ready = $true
            break
        }
        Start-Sleep -Seconds 10
    }
    if (-not $ready) { throw "The new revision for '$appName' did not become ready within five minutes." }
}

Write-Host "Azure application release '$ImageTag' completed without reapplying infrastructure or initializing Redmine." -ForegroundColor Green
