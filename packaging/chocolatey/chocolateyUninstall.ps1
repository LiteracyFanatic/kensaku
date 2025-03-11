$installPath = "$env:ProgramFiles\kensaku"

# Remove the installation directory
if (Test-Path $installPath) {
    Remove-Item -Path $installPath -Recurse -Force
}

Uninstall-ChocolateyPath "$installPath"

Write-Host "kensaku has been removed from $installPath"
