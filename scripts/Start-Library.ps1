param([int]$Port=5088,[switch]$OpenBrowser)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
if (!(Test-Path (Join-Path $projectRoot 'LibraryOnline\Web.Local.config'))) { & (Join-Path $PSScriptRoot 'Configure-Demo.ps1') }
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$vsPath=& $vswhere -latest -products Microsoft.VisualStudio.Product.Community -property installationPath
if (!$vsPath) { throw 'Cần Visual Studio Community với workload ASP.NET and web development.' }
$msbuild=Join-Path $vsPath 'MSBuild\Current\Bin\MSBuild.exe'
& $msbuild (Join-Path $projectRoot 'LibraryOnline.sln') /restore /p:Configuration=Debug /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'Build thất bại.' }
$iis=Join-Path $env:ProgramFiles 'IIS Express\iisexpress.exe'
if (!(Test-Path $iis)) { throw 'Cần cài IIS Express qua Visual Studio Installer.' }
$web=Join-Path $projectRoot 'LibraryOnline'
Write-Host "Website: http://localhost:$Port — dùng Ctrl+C để dừng."
if ($OpenBrowser) { Start-Process "http://localhost:$Port" }
& $iis "/path:$web" "/port:$Port"
