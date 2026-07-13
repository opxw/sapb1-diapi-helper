param(
    [string]$Configuration = "Release",
    [string]$PackageOutputPath,
    [string]$MSBuildPath = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe",
    [switch]$SkipBuild,
    [switch]$NoVersionIncrement
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..")
$projectPath = Join-Path $repoRoot "src\SAPB1.DIAPI.Helper.csproj"

if ([string]::IsNullOrWhiteSpace($PackageOutputPath)) {
    $PackageOutputPath = Join-Path $repoRoot "artifacts\packages"
}

if (-not (Test-Path -LiteralPath $MSBuildPath)) {
    $vswhere = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path -LiteralPath $vswhere) {
        $found = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
        if (-not [string]::IsNullOrWhiteSpace($found)) {
            $MSBuildPath = $found
        }
    }
}

if (-not (Test-Path -LiteralPath $MSBuildPath)) {
    throw "MSBuild was not found. Pass -MSBuildPath or install Visual Studio Build Tools."
}

New-Item -ItemType Directory -Force -Path $PackageOutputPath | Out-Null

function Update-ProjectVersion {
    param([string]$Path)

    [xml]$project = Get-Content -LiteralPath $Path
    $propertyGroup = @($project.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1)[0]
    if (-not $propertyGroup) {
        throw "Could not find <Version> in $Path"
    }

    $currentVersion = [version]$propertyGroup.Version
    $nextVersion = [version]::new($currentVersion.Major, $currentVersion.Minor, $currentVersion.Build + 1)
    $versionText = $nextVersion.ToString()

    $propertyGroup.Version = $versionText
    if ($propertyGroup.AssemblyVersion) {
        $propertyGroup.AssemblyVersion = $versionText
    }
    if ($propertyGroup.FileVersion) {
        $propertyGroup.FileVersion = $versionText
    }

    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.OmitXmlDeclaration = $true
    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $project.Save($writer)
    }
    finally {
        $writer.Dispose()
    }

    [PSCustomObject]@{
        Previous = $currentVersion.ToString()
        Current = $versionText
    }
}

$versionInfo = $null
if (-not $NoVersionIncrement) {
    $versionInfo = Update-ProjectVersion -Path $projectPath
}

Write-Host "Project           : $projectPath"
Write-Host "Configuration     : $Configuration"
Write-Host "PackageOutputPath : $PackageOutputPath"
Write-Host "MSBuild           : $MSBuildPath"
if ($versionInfo) {
    Write-Host "Version           : $($versionInfo.Previous) -> $($versionInfo.Current)"
}
else {
    Write-Host "Version           : unchanged"
}
Write-Host ""

if (-not $SkipBuild) {
    & $MSBuildPath $projectPath /t:Restore,Build /p:Configuration=$Configuration /v:minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
}

& $MSBuildPath $projectPath /t:Pack /p:Configuration=$Configuration /p:PackageOutputPath=$PackageOutputPath /v:minimal
if ($LASTEXITCODE -ne 0) {
    throw "Pack failed with exit code $LASTEXITCODE"
}

Write-Host ""
Write-Host "Generated packages:"
Get-ChildItem -LiteralPath $PackageOutputPath -Filter "*.nupkg" |
    Sort-Object LastWriteTime -Descending |
    Select-Object FullName, Length, LastWriteTime |
    Format-Table -AutoSize
