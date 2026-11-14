[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$tls=Join-Path $repo '.artifacts/messaging/tls'
$trust=Join-Path $repo 'docker/trust'
$envFile=Join-Path $repo '.env'
if(-not(Test-Path -LiteralPath $envFile)){throw 'Run Start-FullStack.ps1 to create the private local environment first.'}
$settings=Get-Content -LiteralPath $envFile -Raw
if($settings -notmatch '(?m)^RABBIT_PASSWORD='){
 $password=[Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
 Add-Content -LiteralPath $envFile -Value "`nRABBIT_PASSWORD=$password" -Encoding utf8
}
New-Item -ItemType Directory -Force -Path $tls,$trust | Out-Null
$existing=@('ca.crt','server.crt','server.key') | Where-Object {Test-Path -LiteralPath (Join-Path $tls $_)}
if($existing.Count -gt 0 -and $existing.Count -ne 3){throw 'Incomplete local messaging TLS files. Inspect them; they will not be silently replaced.'}
if($existing.Count -eq 0){
 $caKey=[Security.Cryptography.RSA]::Create(3072)
 $serverKey=[Security.Cryptography.RSA]::Create(3072)
 try {
  $caRequest=[Security.Cryptography.X509Certificates.CertificateRequest]::new('CN=BrokerHub local RabbitMQ CA',$caKey,[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pkcs1)
  $caRequest.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new($true,$false,0,$true))
  $caRequest.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new([Security.Cryptography.X509Certificates.X509KeyUsageFlags]::KeyCertSign -bor [Security.Cryptography.X509Certificates.X509KeyUsageFlags]::CrlSign,$true))
  $now=[DateTimeOffset]::UtcNow
  $ca=$caRequest.CreateSelfSigned($now.AddMinutes(-5),$now.AddYears(5))
  $request=[Security.Cryptography.X509Certificates.CertificateRequest]::new('CN=rabbitmq',$serverKey,[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pkcs1)
  $san=[Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new(); $san.AddDnsName('rabbitmq'); $request.CertificateExtensions.Add($san.Build())
  $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new($false,$false,0,$true))
  $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new([Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature -bor [Security.Cryptography.X509Certificates.X509KeyUsageFlags]::KeyEncipherment,$true))
  $oids=[Security.Cryptography.OidCollection]::new(); [void]$oids.Add([Security.Cryptography.Oid]::new('1.3.6.1.5.5.7.3.1'))
  $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($oids,$true))
  $server=$request.Create($ca,$now.AddMinutes(-5),$now.AddYears(2),[Security.Cryptography.RandomNumberGenerator]::GetBytes(16))
  [IO.File]::WriteAllText((Join-Path $tls 'ca.crt'),$ca.ExportCertificatePem())
  [IO.File]::WriteAllText((Join-Path $tls 'server.crt'),$server.ExportCertificatePem())
  [IO.File]::WriteAllText((Join-Path $tls 'server.key'),$serverKey.ExportPkcs8PrivateKeyPem())
 } finally {$caKey.Dispose(); $serverKey.Dispose(); if($ca){$ca.Dispose()}; if($server){$server.Dispose()}}
}
$cert=[Security.Cryptography.X509Certificates.X509Certificate2]::CreateFromPemFile((Join-Path $tls 'server.crt'),(Join-Path $tls 'server.key'))
try {if($cert.NotAfter.ToUniversalTime() -lt [DateTime]::UtcNow.AddDays(7)){throw 'Local RabbitMQ certificate needs explicit renewal; existing material has been preserved.'}} finally {$cert.Dispose()}
Copy-Item -LiteralPath (Join-Path $tls 'ca.crt') -Destination (Join-Path $trust 'local-rabbitmq-ca.crt') -Force
Write-Output 'Local messaging TLS and private broker credential ready; existing credentials/certificates preserved. Only the public CA enters the API image.'
