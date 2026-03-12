$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Force -Path 'D:\Temp' | Out-Null
New-Item -ItemType Directory -Force -Path 'D:\Temp\msbuild\obj' | Out-Null
New-Item -ItemType Directory -Force -Path 'D:\Temp\msbuild\bin' | Out-Null
New-Item -ItemType Directory -Force -Path 'D:\Temp\nuget-packages' | Out-Null
New-Item -ItemType Directory -Force -Path 'D:\Temp\nuget-http-cache' | Out-Null
New-Item -ItemType Directory -Force -Path 'D:\Temp\dotnet-home' | Out-Null

$env:TEMP = 'D:\Temp'
$env:TMP = 'D:\Temp'
$env:DOTNET_CLI_HOME = 'D:\Temp\dotnet-home'
$env:NUGET_PACKAGES = 'D:\Temp\nuget-packages'
$env:NUGET_HTTP_CACHE_PATH = 'D:\Temp\nuget-http-cache'

$dotnetDir = 'c:\Users\NMT\Desktop\New folder\.dotnet'
if (Test-Path (Join-Path $dotnetDir 'dotnet.exe')) {
  $env:DOTNET_ROOT = $dotnetDir
  $env:PATH = "$dotnetDir;$env:PATH"
}

Push-Location (Join-Path $PSScriptRoot '..\BE_QLKH')
try {
  dotnet build -c Release -p:BaseIntermediateOutputPath=D:\Temp\msbuild\obj\ -p:BaseOutputPath=D:\Temp\msbuild\bin\ -p:NuGetAudit=false
} finally {
  Pop-Location
}

