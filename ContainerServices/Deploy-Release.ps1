param(
    [string]$Tag,
    [string]$SettingsPath = (Join-Path $PSScriptRoot 'azure.parameters.json'),
    [string]$SecretsPath = (Join-Path $PSScriptRoot '.env.azure'),
    [string]$RemoteName = 'origin',
    [switch]$SkipCiVerification
)

. (Join-Path $PSScriptRoot 'Deployment.Library.ps1')

Assert-CommandAvailable -Name 'git'
Assert-CommandAvailable -Name 'gh'
Assert-CommandAvailable -Name 'az'

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    $remoteUrl = (& git remote get-url $RemoteName | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $remoteUrl -notmatch 'github\.com[:/](?<owner>[^/]+)/(?<repo>[^/.]+)(?:\.git)?$') {
        throw "Could not resolve a GitHub repository from remote '$RemoteName'."
    }
    $owner = $Matches.owner
    $repository = $Matches.repo

    if ([string]::IsNullOrWhiteSpace($Tag)) {
        $releaseJson = & gh api "repos/$owner/$repository/releases/latest"
    } else {
        $releaseJson = & gh api "repos/$owner/$repository/releases/tags/$Tag"
    }
    if ($LASTEXITCODE -ne 0) { throw 'Could not resolve the requested GitHub release.' }
    $release = ($releaseJson | Out-String) | ConvertFrom-Json
    $releaseTag = [string]$release.tag_name

    & git fetch $RemoteName "refs/tags/$releaseTag`:refs/tags/$releaseTag" --force
    if ($LASTEXITCODE -ne 0) { throw "Could not fetch release tag '$releaseTag'." }
    $sha = (& git rev-list -n 1 $releaseTag | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($sha)) { throw "Could not resolve '$releaseTag' to a commit." }

    if (-not $SkipCiVerification) {
        $successfulRuns = (& gh run list --workflow ci.yml --commit $sha --status success --limit 1 --json databaseId --jq 'length' | Out-String).Trim()
        if ($LASTEXITCODE -ne 0 -or $successfulRuns -ne '1') {
            throw "Commit $sha has no successful ci.yml run. Deployment stopped."
        }
    }

    $worktreePath = Join-Path $PSScriptRoot '_deploy_worktree'
    if (Test-Path -LiteralPath $worktreePath) {
        & git worktree remove --force $worktreePath
        if ($LASTEXITCODE -ne 0) { throw "Could not remove stale worktree '$worktreePath'." }
    }

    & git worktree add --detach $worktreePath $sha
    if ($LASTEXITCODE -ne 0) { throw 'Could not create the release worktree.' }
    try {
        & (Join-Path $PSScriptRoot 'Release-Azure.ps1') `
            -SettingsPath $SettingsPath `
            -SecretsPath $SecretsPath `
            -ImageTag $sha `
            -SourcePath $worktreePath
        if ($LASTEXITCODE -ne 0) { throw 'Release deployment failed.' }
    }
    finally {
        & git worktree remove --force $worktreePath
    }

    Write-Host "Release $releaseTag ($sha) deployed successfully." -ForegroundColor Green
}
finally {
    Pop-Location
}
