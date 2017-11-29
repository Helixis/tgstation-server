$bf = $Env:APPVEYOR_BUILD_FOLDER


function CodeSign
{
	param($file)
	&'C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe' sign /f "$bf/Tools/tgstation13.org.pfx" /p "$env:snk_passphrase" /t http://timestamp.verisign.com/scripts/timstamp.dll "$file"
}

#Sign the output files
if (Test-Path env:snk_passphrase)
{
	CodeSign "$bf/TGS.Tests/bin/Release/TGS.Tests.dll"
	CodeSign "$bf/TGS.Server.Console/bin/Release/TGS.Server.Console.exe"
	CodeSign "$bf/TGS.Installer.UI/bin/Release/TG Station Server Installer.exe"
	$env:snk_passphrase = ""
}

$src = "$bf\TGS.Installer.UI\bin\Release"
$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$src\TG Station Server Installer.exe").FileVersion

$destination = "$bf\TGS3-Server-v$version.exe"

Move-Item -Path "$src\TG Station Server Installer.exe" -Destination "$destination"

Add-Type -assembly "system.io.compression.filesystem"

$destination_md5sha = "$bf\MD5-SHA1-Server-v$version.txt"

$src2 = "$bf\ClientApps"
[system.io.directory]::CreateDirectory($src2)
Copy-Item "$bf\TGS.CommandLine\bin\Release\TGCommandLine.exe" "$src2\TGCommandLine.exe"
Copy-Item "$bf\TGS.ControlPanel\bin\Release\TGControlPanel.exe" "$src2\TGControlPanel.exe"
Copy-Item "$bf\TGS.ControlPanel\bin\Release\Octokit.dll" "$src2\Octokit.dll"
Copy-Item "$bf\TGS.Interface\bin\Release\TGServiceInterface.dll" "$src2\TGServiceInterface.dll"
Copy-Item "$bf\TGS.Interface\bin\Release\Newtonsoft.Json.dll" "$src2\Newtonsoft.Json.dll"

$dest2 = "$bf\TGS3-Client-v$version.zip"

[io.compression.zipfile]::CreateFromDirectory($src2, $dest2) 

$src3 = "$bf\ServerConsole"
[system.io.directory]::CreateDirectory($src3)
Copy-Item "$bf\TGS.CommandLine\bin\Release\TGCommandLine.exe" "$src3\TGCommandLine.exe"
Copy-Item "$bf\TGS.ControlPanel\bin\Release\TGControlPanel.exe" "$src3\TGControlPanel.exe"
Copy-Item "$bf\TGS.ControlPanel\bin\Release\Octokit.dll" "$src3\Octokit.dll"
Copy-Item "$bf\TGS.Interface\bin\Release\TGServiceInterface.dll" "$src3\TGServiceInterface.dll"
Copy-Item "$bf\TGS.Server\bin\Release\TGS.Server.dll" "$src3\TGS.Server.dll"
Copy-Item "$bf\TGS.Server\bin\Release\Discord.Net.Core.dll" "$src3\Discord.Net.Core.dll"
Copy-Item "$bf\TGS.Server\bin\Release\Discord.Net.Rest.dll" "$src3\Discord.Net.Rest.dll"
Copy-Item "$bf\TGS.Server\bin\Release\Discord.Net.WebSocket.dll" "$src3\Discord.Net.WebSocket.dll"
Copy-Item "$bf\TGS.Server\bin\Release\LibGit2Sharp.dll" "$src3\LibGit2Sharp.dll"
Copy-Item "$bf\TGS.Server\bin\Release\Meebey.SmartIrc4net.dll" "$src3\Meebey.SmartIrc4net.dll"
Copy-Item "$bf\TGS.Server\bin\Release\Newtonsoft.Json.dll" "$src3\Newtonsoft.Json.dll"
Copy-Item "$bf\TGS.Server\bin\Release\System.Collections.Immutable.dll" "$src3\System.Collections.Immutable.dll"
Copy-Item "$bf\TGS.Server\bin\Release\System.Interactive.Async.dll" "$src3\System.Interactive.Async.dll"
Copy-Item "$bf\TGS.Interface.Bridge\bin\x86\Release\TGDreamDaemonBridge.dll" "$src3\TGDreamDaemonBridge.dll"
[system.io.directory]::CreateDirectory("$src3\lib\win32\x86")
Copy-Item "$bf\TGS.Server\bin\Release\lib\win32\x86\git2-ssh-baa87df.dll" "$src3\lib\win32\x86\git2-ssh-baa87df.dll"
Copy-Item "$bf\TGS.Server\bin\Release\lib\win32\x86\libssh2.dll" "$src3\lib\win32\x86\libssh2.dll"
Copy-Item "$bf\TGS.Server\bin\Release\lib\win32\x86\zlib.dll" "$src3\lib\win32\x86\zlib.dll"
[system.io.directory]::CreateDirectory("$src3\lib\win32\x64")
Copy-Item "$bf\TGS.Server\bin\Release\lib\win32\x64\git2-ssh-baa87df.dll" "$src3\lib\win32\x64\git2-ssh-baa87df.dll"
Copy-Item "$bf\TGS.Server\bin\Release\lib\win32\x64\libssh2.dll" "$src3\lib\win32\x64\libssh2.dll"
Copy-Item "$bf\TGS.Server\bin\Release\lib\win32\x64\zlib.dll" "$src3\lib\win32\x86\zlib.dll"
Copy-Item "$bf\TGS.Server.Console\bin\Release\TGS.Server.Console.exe" "$src3\TGS.Server.Console.exe"

$dest3 = "$bf\TGS3-ServerConsole-v$version.zip"

[io.compression.zipfile]::CreateFromDirectory($src3, $dest3) 

$destination_md5sha2 = "$bf\MD5-SHA1-Client-v$version.txt"
$destination_md5sha3 = "$bf\MD5-SHA1-ServerConsole-v$version.txt"

& fciv -both $destination > $destination_md5sha
& fciv -both $dest2 > $destination_md5sha2
& fciv -both $dest3 > $destination_md5sha3
