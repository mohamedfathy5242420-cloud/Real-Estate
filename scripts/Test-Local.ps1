[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))

function Invoke-Docker([string[]]$Arguments) {
    & docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker command failed: $($Arguments[0])" }
}

Push-Location $repo
try {
    if (-not (Test-Path -LiteralPath '.env')) { throw 'Run scripts/Start-FullStack.ps1 first.' }
    Invoke-Docker @('compose', 'config', '--quiet')
    Invoke-Docker @('build', '--target', 'build', '-t', 'brokerhub-release-checks', '-f', 'frontend/Dockerfile', 'frontend')
    Invoke-Docker @('run', '--rm', 'brokerhub-release-checks', 'pnpm', 'exec', 'prettier', '--check', 'src')
    Invoke-Docker @('run', '--rm', 'brokerhub-release-checks', 'pnpm', 'test', '--watch=false')
    Invoke-Docker @('compose', 'run', '--rm', 'smoke')
}
finally { Pop-Location }
