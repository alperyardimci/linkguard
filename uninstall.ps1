# LinkGuard kaldırma. Önce varsayılan tarayıcıyı tekrar Chrome/Edge yapın.
Remove-Item 'HKCU:\Software\Classes\LinkGuardURL' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item 'HKCU:\Software\Clients\StartMenuInternet\LinkGuard' -Recurse -Force -ErrorAction SilentlyContinue
Remove-ItemProperty 'HKCU:\Software\RegisteredApplications' 'LinkGuard' -ErrorAction SilentlyContinue
Remove-Item (Join-Path $env:LOCALAPPDATA 'LinkGuard') -Recurse -Force -ErrorAction SilentlyContinue
Write-Host 'LinkGuard kaldırıldı.'
Start-Process 'ms-settings:defaultapps'
