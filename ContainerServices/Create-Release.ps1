[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw 'GitHub CLI (gh) is required.'
}

& gh workflow run create-release.yml --ref master
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to start the Create Release workflow.'
}

Write-Output 'The Create Release workflow was started for master.'
