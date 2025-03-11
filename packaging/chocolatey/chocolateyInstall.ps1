$installPath = "$env:ProgramFiles\kensaku"

if (!(Test-Path $installPath)) {
    New-Item -Path $installPath -ItemType Directory | Out-Null
}

$githubReleaseUrl = "https://github.com/LiteracyFanatic/kensaku/releases/download/v<VERSION>"
Invoke-WebRequest -Uri "$githubReleaseUrl/kensaku-win-x64.exe" -OutFile "$installPath\kensaku.exe"
Invoke-WebRequest -Uri "$githubReleaseUrl/kensaku.db" -OutFile "$installPath\kensaku.db"

Install-ChocolateyPath "$installPath"

Write-Host "kensaku installed at $installPath"
