[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateRange(1, [int]::MaxValue)]
    [int]$BuildNumber,

    [string]$TargetRef = 'HEAD',

    [string]$RepositoryPath = (Split-Path -Parent $PSScriptRoot),

    [string]$RequiredMarker = 'Release Please'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Git {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $output = & git -C $RepositoryPath @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed: $($output -join [Environment]::NewLine)"
    }

    return @($output)
}

if (-not (Test-Path -LiteralPath $RepositoryPath -PathType Container)) {
    throw "Repository path does not exist: $RepositoryPath"
}

$targetSha = ([string]@(Invoke-Git -Arguments @('rev-parse', "$TargetRef^{commit}"))[0]).Trim()
$versionTagPattern = '^v(?<major>0|[1-9][0-9]*)\.(?<minor>0|[1-9][0-9]*)\.(?<patch>0|[1-9][0-9]*)$'

$versionTags = foreach ($tagValue in (Invoke-Git -Arguments @('tag', '--merged', $targetSha))) {
    $tag = ([string]$tagValue).Trim()
    if ($tag -match $versionTagPattern) {
        [pscustomobject]@{
            Tag     = $tag
            Version = [version]::new(
                [int]$Matches['major'],
                [int]$Matches['minor'],
                [int]$Matches['patch']
            )
        }
    }
}

$latest = $versionTags | Sort-Object Version -Descending | Select-Object -First 1
$range = if ($null -eq $latest) { $targetSha } else { "$($latest.Tag)..$targetSha" }
$commitShas = @(Invoke-Git -Arguments @('rev-list', '--reverse', $range))

if ($commitShas.Count -eq 0) {
    throw "There are no unreleased commits after $($latest.Tag)."
}

$highestBump = 0
foreach ($commitShaValue in $commitShas) {
    $commitSha = ([string]$commitShaValue).Trim()
    $message = (Invoke-Git -Arguments @('show', '--no-patch', '--format=%B', $commitSha)) -join "`n"
    $subject = ([string]@(Invoke-Git -Arguments @('show', '--no-patch', '--format=%s', $commitSha))[0]).Trim()

    if ($message.IndexOf($RequiredMarker, [StringComparison]::Ordinal) -lt 0) {
        throw "Commit $commitSha is missing the exact marker '$RequiredMarker'."
    }

    $bump = if ($subject -match '^[a-z][a-z0-9-]*(\([^)]+\))?!:') {
        3
    }
    elseif ($subject -match '^feat(\([^)]+\))?:') {
        2
    }
    elseif ($subject -match '^fix(\([^)]+\))?:') {
        1
    }
    else {
        throw "Commit $commitSha must start with fix:, feat:, or a Conventional Commit type followed by !:."
    }

    if ($bump -gt $highestBump) {
        $highestBump = $bump
    }
}

$major = if ($null -eq $latest) { 0 } else { $latest.Version.Major }
$minor = if ($null -eq $latest) { 0 } else { $latest.Version.Minor }

switch ($highestBump) {
    3 {
        $major++
        $minor = 0
    }
    2 {
        $minor++
    }
}

if ($null -ne $latest -and $BuildNumber -le $latest.Version.Build) {
    throw "Build number $BuildNumber must be greater than the previous release build number $($latest.Version.Build)."
}

$candidate = [version]::new($major, $minor, $BuildNumber)
if ($null -ne $latest -and $candidate -le $latest.Version) {
    throw "Calculated version v$candidate must be greater than $($latest.Tag)."
}

Write-Output "v$candidate"
