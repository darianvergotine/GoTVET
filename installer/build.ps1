param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\GoTVET\GoTVET.csproj"
$icon = Join-Path $root "src\GoTVET\GoTVET-icon.ico"
$issSource = Join-Path $PSScriptRoot "GoTVET.iss"
$outputDir = Join-Path $root "dist"
$toolsDir = Join-Path $PSScriptRoot ".tools"
$innoVersion = "7.1.0"
$workRoot = Join-Path $env:TEMP "GoTVET-installer-build"
$publishDir = Join-Path $workRoot "publish"
$issWorkDir = Join-Path $workRoot "iss"
$compilerOut = Join-Path $workRoot "dist"

if (-not (Test-Path -LiteralPath $project)) {
    throw "GoTVET project not found: $project"
}

New-Item -ItemType Directory -Force -Path $publishDir, $outputDir, $toolsDir, $issWorkDir, $compilerOut | Out-Null

Write-Host "Publishing self-contained GoTVET $Version..."
dotnet publish $project `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -o $publishDir `
    -p:Version=$Version `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

Copy-Item -LiteralPath $issSource -Destination (Join-Path $issWorkDir "GoTVET.iss") -Force
Copy-Item -LiteralPath $icon -Destination (Join-Path $issWorkDir "GoTVET-icon.ico") -Force

$iss = Join-Path $issWorkDir "GoTVET.iss"
$issText = Get-Content -LiteralPath $iss -Raw
$issText = $issText.Replace("SetupIconFile=..\src\GoTVET\GoTVET-icon.ico", "SetupIconFile=GoTVET-icon.ico")
Set-Content -LiteralPath $iss -Value $issText -Encoding UTF8

function Find-Iscc {
    $candidates = @(
        (Join-Path $toolsDir "ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 7\ISCC.exe")
    )
    Get-ChildItem -LiteralPath $toolsDir -Filter "ISCC.exe" -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object { $candidates += $_.FullName }
    foreach ($path in $candidates) {
        if ($path -and (Test-Path -LiteralPath $path)) {
            return $path
        }
    }
    return $null
}

$iscc = Find-Iscc
if (-not $iscc) {
    Write-Host "Downloading Inno Setup compiler $innoVersion..."
    $nupkg = Join-Path $toolsDir "Tools.InnoSetup.$innoVersion.nupkg"
    $zip = Join-Path $toolsDir "Tools.InnoSetup.$innoVersion.zip"
    $extractDir = Join-Path $toolsDir "innosetup"
    Invoke-WebRequest -Uri "https://www.nuget.org/api/v2/package/Tools.InnoSetup/$innoVersion" -OutFile $nupkg
    Copy-Item -LiteralPath $nupkg -Destination $zip -Force
    if (Test-Path -LiteralPath $extractDir) {
        Remove-Item -LiteralPath $extractDir -Recurse -Force
    }
    Expand-Archive -LiteralPath $zip -DestinationPath $extractDir -Force
    $iscc = Find-Iscc
}

if (-not $iscc) {
    throw "Could not find ISCC.exe after downloading Tools.InnoSetup."
}

Write-Host "Compiling installer with $iscc..."
Push-Location $issWorkDir
try {
    & $iscc "GoTVET.iss" `
        "/DMyAppVersion=$Version" `
        "/DPublishDir=$publishDir" `
        "/DOutputDir=$compilerOut"
    if ($LASTEXITCODE -ne 0) {
        throw "ISCC failed."
    }
}
finally {
    Pop-Location
}

$setupName = "GoTVET-Setup-$Version.exe"
$compiled = Join-Path $compilerOut $setupName
if (-not (Test-Path -LiteralPath $compiled)) {
    throw "Installer was not created: $compiled"
}

Copy-Item -LiteralPath $compiled -Destination (Join-Path $outputDir $setupName) -Force
Get-Item -LiteralPath (Join-Path $outputDir $setupName) | Select-Object FullName, Length, LastWriteTime
