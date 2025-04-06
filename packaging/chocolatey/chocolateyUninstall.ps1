$toolsPath = Get-ToolsLocation
$installPath = "$toolsPath\kensaku"

if (Test-Path $installPath) {
    Remove-Item -Path $installPath -Recurse -Force
}

Uninstall-BinFile -Name kensaku -Path "$installPath\kensaku.bat"
