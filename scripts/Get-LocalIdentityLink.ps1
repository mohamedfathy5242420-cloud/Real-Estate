[CmdletBinding()]
param([Parameter(Mandatory)][string]$Recipient,[ValidateSet('confirmation','password-reset')][string]$Kind='confirmation')
$ErrorActionPreference='Stop'
if($Recipient -notmatch '^[^\s@]+@[^\s@]+\.(test|invalid)$'){throw 'Use an explicitly configured local .test/.invalid recipient.'}
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$temporary=Join-Path ([IO.Path]::GetTempPath()) ('brokerhub-local-links-'+[guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($temporary)|Out-Null
Push-Location $repo
try {
 & docker compose cp api:/data/identity/. $temporary *> $null
 if($LASTEXITCODE -ne 0){throw 'Unable to read the private identity capture from the running local API.'}
 $match=Get-ChildItem -LiteralPath $temporary -Filter '*.json' -File|Sort-Object LastWriteTimeUtc -Descending|ForEach-Object {Get-Content -LiteralPath $_.FullName -Raw|ConvertFrom-Json}|Where-Object {$_.Recipient -eq $Recipient -and $_.Kind -eq $Kind}|Select-Object -First 1
 if(-not $match){throw 'No matching local link exists. Register/resend/recover using a configured address first.'}
 Write-Output $match.Link
} finally {
 Pop-Location
 # Only exact files copied by this invocation; never recurse through external paths/links.
 Get-ChildItem -LiteralPath $temporary -File | ForEach-Object {Remove-Item -LiteralPath $_.FullName -Force}
 Remove-Item -LiteralPath $temporary -Force
}
