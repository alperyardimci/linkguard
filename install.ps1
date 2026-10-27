# LinkGuard kurulumu: derler, %LOCALAPPDATA%\LinkGuard altına kopyalar ve
# kullanıcı (HKCU) düzeyinde tarayıcı olarak kaydeder. Yönetici yetkisi gerekmez.
$ErrorActionPreference = 'Stop'

$dest = Join-Path $env:LOCALAPPDATA 'LinkGuard'
$exe  = Join-Path $dest 'LinkGuard.exe'
$csc  = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

New-Item -ItemType Directory -Force $dest | Out-Null
& $csc /nologo /codepage:65001 /target:winexe /out:$exe `
    /r:System.Management.dll /r:System.Web.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll `
    (Join-Path $PSScriptRoot 'src\LinkGuard.cs')
if ($LASTEXITCODE -ne 0) { throw 'Derleme başarısız.' }

# Var olan anahtarı silmeden oluşturur (New-Item -Force mevcut değerleri siler)
function Set-Reg($path, $name, $value) {
    if (-not (Test-Path $path)) { New-Item $path -Force | Out-Null }
    Set-ItemProperty $path $name $value
}

$cls = 'HKCU:\Software\Classes\LinkGuardURL'
Set-Reg $cls '(default)' 'LinkGuard URL'
Set-Reg $cls 'URL Protocol' ''
Set-Reg "$cls\DefaultIcon" '(default)' "`"$exe`",0"
Set-Reg "$cls\shell\open\command" '(default)' "`"$exe`" `"%1`""

$cl = 'HKCU:\Software\Clients\StartMenuInternet\LinkGuard'
Set-Reg $cl '(default)' 'LinkGuard'
Set-Reg "$cl\shell\open\command" '(default)' "`"$exe`""
Set-Reg "$cl\Capabilities" 'ApplicationName' 'LinkGuard'
Set-Reg "$cl\Capabilities" 'ApplicationDescription' 'Outlook bağlantılarını açmadan önce tam adresi gösterir'
Set-Reg "$cl\Capabilities\URLAssociations" 'http' 'LinkGuardURL'
Set-Reg "$cl\Capabilities\URLAssociations" 'https' 'LinkGuardURL'

Set-Reg 'HKCU:\Software\RegisteredApplications' 'LinkGuard' 'Software\Clients\StartMenuInternet\LinkGuard\Capabilities'

Write-Host "Kuruldu: $exe"
Write-Host 'Açılan ayarlar sayfasında HTTP ve HTTPS için LinkGuard seçin.'
Start-Process 'ms-settings:defaultapps?registeredAppUser=LinkGuard'
