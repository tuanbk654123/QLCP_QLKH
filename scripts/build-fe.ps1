$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Force -Path 'D:\Temp' | Out-Null
New-Item -ItemType Directory -Force -Path 'D:\Temp\npm-cache' | Out-Null
New-Item -ItemType Directory -Force -Path 'D:\Temp\fe-build' | Out-Null

$env:TEMP = 'D:\Temp'
$env:TMP = 'D:\Temp'
$env:npm_config_cache = 'D:\Temp\npm-cache'
$env:BUILD_PATH = 'D:\Temp\fe-build'
$env:GENERATE_SOURCEMAP = 'false'
$env:NODE_OPTIONS = '--max_old_space_size=4096'

Push-Location (Join-Path $PSScriptRoot '..\Qu-n-l-kh-ch-h-ng-qu-n-l-t-i-ch-nh')
try {
  npm run build
} finally {
  Pop-Location
}

