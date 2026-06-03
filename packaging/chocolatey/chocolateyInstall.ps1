$toolsPath = Get-ToolsLocation
$installPath = "$toolsPath\kensaku"

$githubReleaseUrl = "https://github.com/LiteracyFanatic/kensaku/releases/download/v<VERSION>"
Get-ChocolateyWebFile `
  -PackageName kensaku `
  -FileFullPath "$installPath\kensaku.exe" `
  -Url "$githubReleaseUrl/kensaku-win-x64.exe" `
  -Checksum '<EXE_SHA256>' `
  -ChecksumType sha256

Get-ChocolateyWebFile `
  -PackageName kensaku `
  -FileFullPath "$installPath\kensaku.db" `
  -Url "$githubReleaseUrl/kensaku.db" `
  -Checksum '<DB_SHA256>' `
  -ChecksumType sha256

$wrapperPath = Join-Path $PSScriptRoot "kensaku.bat"
Copy-Item -Force $wrapperPath "$installPath\kensaku.bat"
Install-BinFile -Name kensaku -Path "$installPath\kensaku.bat"
