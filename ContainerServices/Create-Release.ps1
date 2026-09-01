[CmdletBinding()]
param(
    [switch]$NoWait,
    [switch]$NoPause
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-GitHubCli {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [Parameter(Mandatory)]
        [string]$ErrorMessage
    )

    $output = & $script:GitHubCliPath @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "$ErrorMessage`n$($output -join [Environment]::NewLine)"
    }

    return @($output)
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$locationChanged = $false
$exitCode = 0

try {
    Write-Host 'Starting SRM release creation...' -ForegroundColor Cyan

    $githubCliCommand = Get-Command gh -ErrorAction SilentlyContinue
    if ($null -ne $githubCliCommand) {
        $script:GitHubCliPath = if ([string]::IsNullOrWhiteSpace($githubCliCommand.Source)) {
            $githubCliCommand.Name
        }
        else {
            $githubCliCommand.Source
        }
    }
    else {
        $githubCliCandidates = @(
            (Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'GitHub CLI\gh.exe'),
            (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\GitHub CLI\gh.exe')
        )
        $script:GitHubCliPath = $githubCliCandidates |
            Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
            Select-Object -First 1
    }

    if ([string]::IsNullOrWhiteSpace($script:GitHubCliPath)) {
        throw 'GitHub CLI (gh) is required. Install it and run "gh auth login" first.'
    }

    Push-Location $repositoryRoot
    $locationChanged = $true

    $repository = (Invoke-GitHubCli `
        -Arguments @('repo', 'view', '--json', 'nameWithOwner', '--jq', '.nameWithOwner') `
        -ErrorMessage 'Could not determine the GitHub repository.').Trim()

    $previousRunsJson = (Invoke-GitHubCli `
        -Arguments @(
            'run', 'list',
            '--repo', $repository,
            '--workflow', 'create-release.yml',
            '--event', 'workflow_dispatch',
            '--limit', '1',
            '--json', 'databaseId'
        ) `
        -ErrorMessage 'Could not read existing Create Release workflow runs.') -join "`n"
    $previousRunsValue = $previousRunsJson | ConvertFrom-Json
    $previousRuns = @($previousRunsValue | ForEach-Object { $_ })
    $previousRunId = if ($previousRuns.Count -eq 0) { 0 } else { [long]$previousRuns[0].databaseId }

    Invoke-GitHubCli `
        -Arguments @('workflow', 'run', 'create-release.yml', '--repo', $repository, '--ref', 'master') `
        -ErrorMessage 'Failed to start the Create Release workflow.' | Out-Null

    Write-Host "Create Release was dispatched for $repository on master." -ForegroundColor Cyan

    if (-not $NoWait) {
        $run = $null
        for ($attempt = 1; $attempt -le 30 -and $null -eq $run; $attempt++) {
            Start-Sleep -Seconds 2
            $runsJson = (Invoke-GitHubCli `
                -Arguments @(
                    'run', 'list',
                    '--repo', $repository,
                    '--workflow', 'create-release.yml',
                    '--event', 'workflow_dispatch',
                    '--limit', '1',
                    '--json', 'databaseId,url'
                ) `
                -ErrorMessage 'Could not find the dispatched Create Release workflow run.') -join "`n"
            $runsValue = $runsJson | ConvertFrom-Json
            $runs = @($runsValue | ForEach-Object { $_ })
            if ($runs.Count -gt 0 -and [long]$runs[0].databaseId -ne $previousRunId) {
                $run = $runs[0]
            }
        }

        if ($null -eq $run) {
            throw 'The workflow was dispatched, but its run did not appear within 60 seconds. Check GitHub Actions.'
        }

        Write-Host "Following workflow run $($run.url)" -ForegroundColor Cyan
        & $script:GitHubCliPath run watch ([string]$run.databaseId) --repo $repository --exit-status
        if ($LASTEXITCODE -ne 0) {
            throw "Create Release failed. Review the workflow log: $($run.url)"
        }

        Write-Host "Release created successfully. Workflow: $($run.url)" -ForegroundColor Green
    }
}
catch {
    $exitCode = 1
    Write-Host "Release creation failed: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    if ($locationChanged) {
        Pop-Location
    }

    if (-not $NoPause) {
        [void](Read-Host 'Press Enter to close this window')
    }
}

exit $exitCode
