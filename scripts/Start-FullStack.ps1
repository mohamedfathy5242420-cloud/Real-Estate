[CmdletBinding()]
param([ValidateRange(1024,65535)][int]$Port=8080,[switch]$NoBuild)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $repo
try {
 if(-not(Test-Path -LiteralPath '.env')){
  function New-LocalPassword { 'Bh!9aA'+[Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24)) }
  @("WEB_PORT=$Port","SQL_PASSWORD=$(New-LocalPassword)","ADMIN_EMAIL=admin@brokerhub.test","ADMIN_PASSWORD=$(New-LocalPassword)","SMOKE_PASSWORD=$(New-LocalPassword)",'BOOTSTRAP_ADMIN=true')|Set-Content -LiteralPath '.env' -Encoding utf8
  Write-Output 'Created local .env with generated credentials. Keep it private; do not regenerate it for existing SQL volumes.'
 }
 & (Join-Path $PSScriptRoot 'Initialize-LocalMessaging.ps1')
 & docker info --format '{{.ServerVersion}}'
 if($LASTEXITCODE -ne 0){throw 'Start Docker Desktop from the Windows Start menu, then rerun this command.'}
 & docker compose config --quiet
 if($LASTEXITCODE -ne 0){throw 'Compose configuration is invalid.'}
 if(-not $NoBuild){
  & docker compose build api
  if($LASTEXITCODE -ne 0){throw 'API image build failed.'}
  & docker compose build frontend
  if($LASTEXITCODE -ne 0){throw 'Frontend image build failed.'}
 }
 & docker compose up -d --wait --wait-timeout 240
 if($LASTEXITCODE -ne 0){throw 'Stack startup failed; inspect docker compose logs.'}
 $savedPort=((Get-Content '.env' | Where-Object {$_ -match '^WEB_PORT='}) -split '=',2)[1]
 Write-Output "Open http://localhost:$savedPort/catalog ; Swagger http://localhost:$savedPort/swagger ."
 Write-Output 'Admin credentials are in .env. After first login set BOOTSTRAP_ADMIN=false and rerun with -NoBuild. Stop with docker compose down (volumes are retained).'
} finally {Pop-Location}
