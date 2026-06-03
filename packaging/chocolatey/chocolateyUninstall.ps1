$toolsPath = Get-ToolsLocation
$installPath = "$toolsPath\kensaku"

Uninstall-BinFile -Name kensaku -Path "$installPath\kensaku.bat"

if (Test-Path $installPath) {
    Remove-Item -Path $installPath -Recurse -Force
}
