[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Target = "rhino8",
    [string]$Version,
    [string]$ArtifactsRoot = "artifacts/publish",
    [switch]$NoRestore,
    [string]$PluginOutputDir,
    [switch]$SkipBridgePublish
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,

        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $RelativePath))
}

function New-CleanDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (Test-Path $Path) {
        Remove-Item -Path $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Copy-DirectoryContents {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    Get-ChildItem -Path $Source -Force | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $Destination -Recurse -Force
    }
}

function Invoke-Dotnet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    Write-Host ""
    Write-Host "dotnet $($Arguments -join ' ')"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code $LASTEXITCODE"
    }
}

function Get-VersionFromProject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    [xml]$projectXml = Get-Content -Path $ProjectPath
    $versionNode = $projectXml.Project.PropertyGroup.Version | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($versionNode)) {
        throw "Could not find a <Version> element in $ProjectPath"
    }

    return $versionNode.Trim()
}

function Remove-BridgeArtifacts {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Directory
    )

    $bridgeArtifactNames = @(
        "SwiftletBridge.exe",
        "SwiftletBridge",
        "SwiftletBridge.dll",
        "SwiftletBridge.deps.json",
        "SwiftletBridge.runtimeconfig.json",
        "createdump.exe",
        "createdump"
    )

    foreach ($artifactName in $bridgeArtifactNames) {
        $artifactPath = Join-Path $Directory $artifactName
        if (Test-Path $artifactPath) {
            Remove-Item -Path $artifactPath -Force
        }
    }
}

function Get-VersionFromProps {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PropsPath
    )

    [xml]$propsXml = Get-Content -Path $PropsPath
    $versionNode = $propsXml.Project.PropertyGroup.SwiftletVersion | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($versionNode)) {
        throw "Could not find a <SwiftletVersion> element in $PropsPath"
    }

    return $versionNode.Trim()
}

function Get-PackageReferenceVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$PackageId
    )

    [xml]$projectXml = Get-Content -Path $ProjectPath
    $packageNode = $projectXml.Project.ItemGroup.PackageReference |
        Where-Object { $_.Include -eq $PackageId } |
        Select-Object -First 1

    if ($null -eq $packageNode) {
        throw "Could not find a PackageReference for $PackageId in $ProjectPath"
    }

    $version = [string]$packageNode.Version
    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "Could not resolve a version for PackageReference $PackageId in $ProjectPath"
    }

    return $version.Trim()
}

function Resolve-PackageLibraryPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$PackageId,

        [Parameter(Mandatory = $true)]
        [string]$LibraryRelativePath
    )

    $version = Get-PackageReferenceVersion -ProjectPath $ProjectPath -PackageId $PackageId
    $packageRoot = Join-Path $env:USERPROFILE ".nuget\packages\$($PackageId.ToLowerInvariant())\$version"
    $packageLibraryPath = Join-Path $packageRoot $LibraryRelativePath
    if (-not (Test-Path $packageLibraryPath)) {
        throw "Required package dependency was not found: $packageLibraryPath"
    }

    return $packageLibraryPath
}

function Copy-RuntimeDependencies {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DestinationDirectory,

        [Parameter(Mandatory = $true)]
        [object[]]$RuntimeDependencies
    )

    foreach ($runtimeDependency in $RuntimeDependencies) {
        $runtimeDependencyPath = Resolve-PackageLibraryPath `
            -ProjectPath $runtimeDependency.ProjectPath `
            -PackageId $runtimeDependency.PackageId `
            -LibraryRelativePath $runtimeDependency.LibraryRelativePath

        Copy-Item -Path $runtimeDependencyPath -Destination (Join-Path $DestinationDirectory ([System.IO.Path]::GetFileName($runtimeDependencyPath))) -Force
    }
}

function New-YakManifest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TemplatePath,

        [Parameter(Mandatory = $true)]
        [string]$ManifestPath,

        [Parameter(Mandatory = $true)]
        [string]$VersionValue
    )

    $content = Get-Content -Path $TemplatePath -Raw
    $content = $content -replace '\{\{VERSION\}\}', $VersionValue
    Set-Content -Path $ManifestPath -Value $content -NoNewline
}

function Write-Utf8NoBomWithLf {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Content
    )

    $normalizedContent = $Content -replace "`r`n", "`n" -replace "`r", "`n"
    [System.IO.File]::WriteAllText($Path, $normalizedContent, [System.Text.UTF8Encoding]::new($false))
}

function Resolve-YakExecutable {
    $yakInPath = Get-Command "yak" -ErrorAction SilentlyContinue
    if ($null -ne $yakInPath) {
        return $yakInPath.Source
    }

    $yakSearchPaths = @(
        "C:\Program Files\Rhino 9\System\Yak.exe",
        "C:\Program Files\Rhino 8\System\Yak.exe",
        "C:\Program Files\Rhino 7\System\Yak.exe"
    )

    foreach ($candidate in $yakSearchPaths) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    return $null
}

function Invoke-YakBuild {
    param(
        [Parameter(Mandatory = $true)]
        [string]$YakExecutable,

        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,

        [Parameter(Mandatory = $true)]
        [string]$Platform
    )

    $existingPackagePaths = @(
        Get-ChildItem -Path $WorkingDirectory -Filter "*.yak" -File -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty FullName
    )

    Write-Host ""
    Write-Host "yak build --platform=$Platform"
    Push-Location $WorkingDirectory
    try {
        & $YakExecutable build "--platform=$Platform" | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "yak build failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }

    $newPackage = Get-ChildItem -Path $WorkingDirectory -Filter "*.yak" -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notin $existingPackagePaths } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $newPackage) {
        $newPackage = Get-ChildItem -Path $WorkingDirectory -Filter "*.yak" -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1
    }

    if ($null -eq $newPackage) {
        throw "yak build completed but no .yak package was produced in $WorkingDirectory"
    }

    return $newPackage.FullName
}

function Set-YakDistributionAppVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackagePath,

        [Parameter(Mandatory = $true)]
        [string]$AppVersion
    )

    $fileName = [System.IO.Path]::GetFileName($PackagePath)
    if ($fileName -notmatch '^(?<prefix>.+)-(?<app>(?:rh\d+(?:_\d+)?)|any)-(?<platform>win|mac|any)\.yak$') {
        throw "Could not retag Yak package because the distribution tag could not be parsed: $fileName"
    }

    $retaggedPath = Join-Path (Split-Path -Parent $PackagePath) "$($Matches.prefix)-$AppVersion-$($Matches.platform).yak"
    if ($retaggedPath -ieq $PackagePath) {
        return $PackagePath
    }

    if (Test-Path $retaggedPath) {
        Remove-Item -Path $retaggedPath -Force
    }

    Move-Item -Path $PackagePath -Destination $retaggedPath
    return $retaggedPath
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-RepoPath -BasePath $scriptDirectory -RelativePath "..\.."
$targetMetadataPath = Resolve-RepoPath -BasePath $repoRoot -RelativePath "build\targets\$Target.json"

if (-not (Test-Path $targetMetadataPath)) {
    throw "Target metadata file not found: $targetMetadataPath"
}

$targetMetadata = Get-Content -Path $targetMetadataPath -Raw | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace($Version)) {
    if (-not [string]::IsNullOrWhiteSpace([string]$targetMetadata.versionSourceFile)) {
        $versionPropsPath = Resolve-RepoPath -BasePath $repoRoot -RelativePath $targetMetadata.versionSourceFile
        $Version = Get-VersionFromProps -PropsPath $versionPropsPath
    }
    elseif (-not [string]::IsNullOrWhiteSpace([string]$targetMetadata.versionSourceProject)) {
        $versionProjectPath = Resolve-RepoPath -BasePath $repoRoot -RelativePath $targetMetadata.versionSourceProject
        $Version = Get-VersionFromProject -ProjectPath $versionProjectPath
    }
    else {
        throw "Target metadata must define either versionSourceFile or versionSourceProject."
    }
}

$pluginProjectPath = Resolve-RepoPath -BasePath $repoRoot -RelativePath $targetMetadata.pluginProject
$bridgeProjectPath = Resolve-RepoPath -BasePath $repoRoot -RelativePath $targetMetadata.bridgeProject
$imagingProjectPath = Resolve-RepoPath -BasePath $repoRoot -RelativePath "src\Swiftlet.Imaging\Swiftlet.Imaging.csproj"
$artifactsBase = Resolve-RepoPath -BasePath $repoRoot -RelativePath $ArtifactsRoot
$stageRoot = Join-Path $artifactsBase (Join-Path $Target $Version)
$pluginBuildDirectory = Join-Path $stageRoot "intermediate\plugin-build"
$pluginStageRoot = Join-Path $stageRoot "plugin"
$bridgeStageRoot = Join-Path $stageRoot "bridge"
$linuxPackageDirectory = Join-Path $stageRoot "linux\compute"
$linuxPackagesDirectory = Join-Path $linuxPackageDirectory "packages"
$pluginStageDirectories = @{
    win = (Join-Path $pluginStageRoot "windows")
    mac = (Join-Path $pluginStageRoot "mac")
    any = (Join-Path $pluginStageRoot "any")
}
$yakStageDirectories = @{
    win = (Join-Path $stageRoot "yak\win")
    mac = (Join-Path $stageRoot "yak\mac")
    any = (Join-Path $stageRoot "yak\any")
}
$manifestTemplatePath = Resolve-RepoPath -BasePath $repoRoot -RelativePath "Yak\manifest-template.yml"
$summaryPath = Join-Path $stageRoot "artifact-manifest.json"

Write-Host "============================================"
Write-Host "Publishing Swiftlet target artifacts"
Write-Host "============================================"
Write-Host "Target: $Target"
Write-Host "Configuration: $Configuration"
Write-Host "Version: $Version"
if ($null -ne $targetMetadata.minimumRhinoMajor) {
    Write-Host "Minimum Rhino major: $($targetMetadata.minimumRhinoMajor)"
}
if ($null -ne $targetMetadata.supportedRhinoMajors) {
    Write-Host "Supported Rhino majors: $([string]::Join(', ', $targetMetadata.supportedRhinoMajors))"
}
if ($null -ne $targetMetadata.yakRhinoMajor) {
    Write-Host "Yak Rhino major: $($targetMetadata.yakRhinoMajor)"
}
Write-Host "Stage root: $stageRoot"
Write-Host "============================================"

New-CleanDirectory -Path $pluginBuildDirectory
foreach ($pluginStageDirectory in $pluginStageDirectories.Values) {
    New-CleanDirectory -Path $pluginStageDirectory
}
New-CleanDirectory -Path $bridgeStageRoot
New-CleanDirectory -Path $linuxPackageDirectory
foreach ($yakStageDirectory in $yakStageDirectories.Values) {
    New-CleanDirectory -Path $yakStageDirectory
}

if (-not [string]::IsNullOrWhiteSpace($PluginOutputDir)) {
    $resolvedPluginOutputDir = if ([System.IO.Path]::IsPathRooted($PluginOutputDir)) {
        $PluginOutputDir
    }
    else {
        Resolve-RepoPath -BasePath $repoRoot -RelativePath $PluginOutputDir
    }

    if (-not (Test-Path $resolvedPluginOutputDir)) {
        throw "Plugin output directory was not found: $resolvedPluginOutputDir"
    }

    Copy-DirectoryContents -Source $resolvedPluginOutputDir -Destination $pluginBuildDirectory
}
else {
    $pluginBuildArguments = @(
        "build",
        $pluginProjectPath,
        "-c", $Configuration,
        "-o", $pluginBuildDirectory,
        "-p:StageBridgeOnBuild=false"
    )

    if ($NoRestore) {
        $pluginBuildArguments += "--no-restore"
    }

    Invoke-Dotnet -Arguments $pluginBuildArguments
}

Remove-BridgeArtifacts -Directory $pluginBuildDirectory

$runtimeDependencies = @(
    [pscustomobject]@{
        ProjectPath = $pluginProjectPath
        PackageId = "HtmlAgilityPack"
        LibraryRelativePath = "lib\netstandard2.0\HtmlAgilityPack.dll"
    },
    [pscustomobject]@{
        ProjectPath = $imagingProjectPath
        PackageId = "QRCoder"
        LibraryRelativePath = "lib\net6.0\QRCoder.dll"
    },
    [pscustomobject]@{
        ProjectPath = $imagingProjectPath
        PackageId = "SixLabors.ImageSharp"
        LibraryRelativePath = "lib\net6.0\SixLabors.ImageSharp.dll"
    }
)

Copy-RuntimeDependencies -DestinationDirectory $pluginBuildDirectory -RuntimeDependencies $runtimeDependencies
foreach ($pluginStageDirectory in $pluginStageDirectories.Values) {
    Copy-DirectoryContents -Source $pluginBuildDirectory -Destination $pluginStageDirectory
}

$pluginArtifacts = @{}
foreach ($platform in $pluginStageDirectories.Keys) {
    $pluginArtifactPath = Join-Path $pluginStageDirectories[$platform] $targetMetadata.pluginArtifactName
    if (-not (Test-Path $pluginArtifactPath)) {
        throw "Expected plugin artifact was not produced: $pluginArtifactPath"
    }

    $pluginArtifacts[$platform] = $pluginArtifactPath
}

$bridgePublishes = @()
if (-not $SkipBridgePublish) {
    foreach ($publishTarget in $targetMetadata.bridgePublishes) {
        $publishId = [string]$publishTarget.id
        $publishDirectory = Join-Path $bridgeStageRoot $publishId
        New-CleanDirectory -Path $publishDirectory

        $dotnetArguments = @(
            "publish",
            $bridgeProjectPath,
            "-c", $Configuration,
            "-o", $publishDirectory,
            "-r", [string]$publishTarget.rid,
            "--self-contained", ($publishTarget.selfContained.ToString().ToLowerInvariant())
        )

        if ([bool]$publishTarget.singleFile) {
            $dotnetArguments += @(
                "-p:PublishSingleFile=true",
                "-p:EnableCompressionInSingleFile=true"
            )
        }

        if ($NoRestore) {
            $dotnetArguments += "--no-restore"
        }

        Invoke-Dotnet -Arguments $dotnetArguments

        $bridgePublishes += [pscustomobject]@{
            id = $publishId
            rid = [string]$publishTarget.rid
            path = $publishDirectory
        }

        if (-not [string]::IsNullOrWhiteSpace([string]$publishTarget.copyIntoPlugin)) {
            $packagePlatform = [string]$publishTarget.packagePlatform
            if ([string]::IsNullOrWhiteSpace($packagePlatform) -or -not $pluginStageDirectories.ContainsKey($packagePlatform)) {
                throw "Bridge publish target '$publishId' must define a valid packagePlatform."
            }

            $sourceBridgePath = Join-Path $publishDirectory ([string]$publishTarget.copyIntoPlugin)
            if (-not (Test-Path $sourceBridgePath)) {
                throw "Bridge artifact was not produced: $sourceBridgePath"
            }

            Copy-Item -Path $sourceBridgePath -Destination (Join-Path $pluginStageDirectories[$packagePlatform] ([string]$publishTarget.copyIntoPlugin)) -Force
        }
    }
}
else {
    Write-Host ""
    Write-Host "Skipping bridge publish."
}

$linuxBridgePublish = $bridgePublishes | Where-Object { $_.id -eq "linux-x64" } | Select-Object -First 1
$linuxBridgeDirectory = Join-Path $linuxPackageDirectory "bridge"
$linuxExamplesDirectory = Join-Path $linuxPackageDirectory "examples"
$linuxReadmePath = Join-Path $linuxPackageDirectory "README.md"
$linuxInstallScriptPath = Join-Path $linuxPackageDirectory "install.sh"
$linuxComputeInstallScriptPath = Join-Path $linuxPackageDirectory "install-compute-plugin.sh"
$linuxConfigTemplatePath = Join-Path $linuxExamplesDirectory "claude-desktop.mcp.template.json"

if ($null -ne $linuxBridgePublish) {
    Copy-DirectoryContents -Source $linuxBridgePublish.path -Destination $linuxBridgeDirectory
}
New-Item -ItemType Directory -Path $linuxExamplesDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $linuxPackagesDirectory -Force | Out-Null

$bridgeContents = if ($null -ne $linuxBridgePublish) {
@"
- `bridge/SwiftletBridge`
  - self-contained linux-x64 MCP bridge
- `install.sh`
  - marks the bridge as executable
"@
}
else {
@"
- bridge publish was skipped for this build
"@
}

$bridgeSetup = if ($null -ne $linuxBridgePublish) {
@"
## MCP Bridge Setup

1. Run `./install.sh`.
2. Update the example MCP config with the absolute path to `bridge/SwiftletBridge`.
3. Point the bridge at your Swiftlet MCP server URL, for example `http://127.0.0.1:3001/mcp/`.
"@
}
else {
    ""
}

$linuxReadme = @"
# Swiftlet Linux Compute Package

Version: $Version

This package contains:

- a local Yak package for the Swiftlet Grasshopper plug-in
- an install helper for Rhino.Compute on Linux / WSL
- the Linux-facing Swiftlet bridge for MCP and other headless workflows

## Contents

- packages/*.yak
  - local Swiftlet Yak package
- install-compute-plugin.sh
  - installs the local Yak package for the rhino-compute service user
$bridgeContents
- examples/claude-desktop.mcp.template.json
  - example MCP client config

## Rhino.Compute Setup

1. Copy this folder to the Linux machine.
2. Run `sudo ./install-compute-plugin.sh`.
3. Restart Rhino.Compute if the script did not already restart it.
4. Check `journalctl -u rhino-compute` for plug-in load errors.

$bridgeSetup

## Notes

- install-compute-plugin.sh installs Swiftlet into the home directory of the rhino-compute user via Yak.
- The script also forces RHINO_COMPUTE_LOAD_GRASSHOPPER=true in /etc/rhino-compute/environment for compatibility with older installs.
- OAuth and clipboard behavior in Linux headless environments are manual by design.
"@

$linuxInstallScript = @'
#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [[ ! -f "$SCRIPT_DIR/bridge/SwiftletBridge" ]]; then
  echo "This package does not include SwiftletBridge." >&2
  exit 1
fi

chmod +x "$SCRIPT_DIR/bridge/SwiftletBridge"
echo "SwiftletBridge is ready at $SCRIPT_DIR/bridge/SwiftletBridge"
'@

$linuxComputeInstallScript = @'
#!/usr/bin/env bash
set -euo pipefail

if [[ "${EUID}" -ne 0 ]]; then
  echo "Run this script with sudo." >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVICE_USER="${SERVICE_USER:-rhino-compute}"
COMPUTE_ENV_FILE="${COMPUTE_ENV_FILE:-/etc/rhino-compute/environment}"
RESTART_SERVICE="${RESTART_SERVICE:-true}"

YAK_PACKAGE="$(find "$SCRIPT_DIR/packages" -maxdepth 1 -type f -name '*.yak' | sort | head -n 1)"
if [[ -z "${YAK_PACKAGE}" ]]; then
  echo "No local .yak package found in $SCRIPT_DIR/packages" >&2
  exit 1
fi

if ! command -v yak >/dev/null 2>&1; then
  echo "yak CLI was not found on PATH." >&2
  exit 1
fi

SERVICE_HOME="$(getent passwd "$SERVICE_USER" | cut -d: -f6)"
if [[ -z "${SERVICE_HOME}" ]]; then
  echo "Could not resolve home directory for service user '$SERVICE_USER'." >&2
  exit 1
fi

mkdir -p "$SERVICE_HOME"
chown "$SERVICE_USER:$SERVICE_USER" "$SERVICE_HOME"

su -s /bin/bash "$SERVICE_USER" -c "HOME='$SERVICE_HOME' yak uninstall swiftlet >/dev/null 2>&1 || true"
su -s /bin/bash "$SERVICE_USER" -c "HOME='$SERVICE_HOME' yak install '$YAK_PACKAGE'"

if [[ -f "$COMPUTE_ENV_FILE" ]]; then
  if grep -Eq '^[#[:space:]]*RHINO_COMPUTE_LOAD_GRASSHOPPER=' "$COMPUTE_ENV_FILE"; then
    sed -i -E 's|^[#[:space:]]*RHINO_COMPUTE_LOAD_GRASSHOPPER=.*|RHINO_COMPUTE_LOAD_GRASSHOPPER=true|' "$COMPUTE_ENV_FILE"
  else
    printf '\nRHINO_COMPUTE_LOAD_GRASSHOPPER=true\n' >> "$COMPUTE_ENV_FILE"
  fi
fi

if [[ "$RESTART_SERVICE" == "true" ]] && command -v systemctl >/dev/null 2>&1; then
  systemctl restart rhino-compute
  echo "Restarted rhino-compute."
fi

echo "Swiftlet was installed from $YAK_PACKAGE for user $SERVICE_USER."
'@

$linuxConfigTemplate = @'
{
  "mcpServers": {
    "Swiftlet": {
      "command": "/absolute/path/to/SwiftletBridge",
      "args": [
        "http://127.0.0.1:3001/mcp/"
      ]
    }
  }
}
'@

Write-Utf8NoBomWithLf -Path $linuxReadmePath -Content $linuxReadme
Write-Utf8NoBomWithLf -Path $linuxInstallScriptPath -Content $linuxInstallScript
Write-Utf8NoBomWithLf -Path $linuxComputeInstallScriptPath -Content $linuxComputeInstallScript
Write-Utf8NoBomWithLf -Path $linuxConfigTemplatePath -Content $linuxConfigTemplate

$yakStageDirectory = $yakStageDirectories["win"]
$yakMacStageDirectory = $yakStageDirectories["mac"]
$yakAnyStageDirectory = $yakStageDirectories["any"]

Copy-DirectoryContents -Source $pluginStageDirectories["win"] -Destination $yakStageDirectory
New-YakManifest -TemplatePath $manifestTemplatePath -ManifestPath (Join-Path $yakStageDirectory "manifest.yml") -VersionValue $Version

Copy-DirectoryContents -Source $pluginStageDirectories["mac"] -Destination $yakMacStageDirectory
New-YakManifest -TemplatePath $manifestTemplatePath -ManifestPath (Join-Path $yakMacStageDirectory "manifest.yml") -VersionValue $Version

Copy-DirectoryContents -Source $pluginStageDirectories["any"] -Destination $yakAnyStageDirectory
New-YakManifest -TemplatePath $manifestTemplatePath -ManifestPath (Join-Path $yakAnyStageDirectory "manifest.yml") -VersionValue $Version

$yakExecutable = Resolve-YakExecutable
$yakWinPackagePath = $null
$yakMacPackagePath = $null
$yakAnyPackagePath = $null
if (-not [string]::IsNullOrWhiteSpace($yakExecutable)) {
    $yakWinPackagePath = Invoke-YakBuild -YakExecutable $yakExecutable -WorkingDirectory $yakStageDirectory -Platform "win"
    $yakMacPackagePath = Invoke-YakBuild -YakExecutable $yakExecutable -WorkingDirectory $yakMacStageDirectory -Platform "mac"
    $yakAnyPackagePath = Invoke-YakBuild -YakExecutable $yakExecutable -WorkingDirectory $yakAnyStageDirectory -Platform "any"

    if (-not [string]::IsNullOrWhiteSpace([string]$targetMetadata.yakDistributionAppVersionOverride)) {
        $yakWinPackagePath = Set-YakDistributionAppVersion -PackagePath $yakWinPackagePath -AppVersion ([string]$targetMetadata.yakDistributionAppVersionOverride)
        $yakMacPackagePath = Set-YakDistributionAppVersion -PackagePath $yakMacPackagePath -AppVersion ([string]$targetMetadata.yakDistributionAppVersionOverride)
        $yakAnyPackagePath = Set-YakDistributionAppVersion -PackagePath $yakAnyPackagePath -AppVersion ([string]$targetMetadata.yakDistributionAppVersionOverride)
    }
}
else {
    Write-Warning "Yak CLI tool not found. Yak stage folders were created, but .yak files were not built."
}

if (-not [string]::IsNullOrWhiteSpace($yakAnyPackagePath)) {
    Copy-Item -Path $yakAnyPackagePath -Destination (Join-Path $linuxPackagesDirectory ([System.IO.Path]::GetFileName($yakAnyPackagePath))) -Force
}

$linuxComputeYakPackagePath = $null
if (-not [string]::IsNullOrWhiteSpace($yakAnyPackagePath)) {
    $linuxComputeYakPackagePath = Join-Path $linuxPackagesDirectory ([System.IO.Path]::GetFileName($yakAnyPackagePath))
}

$summary = [pscustomobject]@{
    target = $Target
    minimumRhinoMajor = $targetMetadata.minimumRhinoMajor
    supportedRhinoMajors = $targetMetadata.supportedRhinoMajors
    yakRhinoMajor = $targetMetadata.yakRhinoMajor
    yakDistributionAppVersionOverride = $targetMetadata.yakDistributionAppVersionOverride
    configuration = $Configuration
    version = $Version
    generatedAtUtc = [DateTime]::UtcNow.ToString("o")
    plugin = [pscustomobject]@{
        win = [pscustomobject]@{
            path = $pluginStageDirectories["win"]
            artifact = $pluginArtifacts["win"]
        }
        mac = [pscustomobject]@{
            path = $pluginStageDirectories["mac"]
            artifact = $pluginArtifacts["mac"]
        }
        any = [pscustomobject]@{
            path = $pluginStageDirectories["any"]
            artifact = $pluginArtifacts["any"]
        }
    }
    bridgePublishes = $bridgePublishes
    linuxPackage = [pscustomobject]@{
        path = $linuxPackageDirectory
        bridge = (Join-Path $linuxPackageDirectory "bridge\SwiftletBridge")
        install = (Join-Path $linuxPackageDirectory "install.sh")
        computeInstall = (Join-Path $linuxPackageDirectory "install-compute-plugin.sh")
        package = $linuxComputeYakPackagePath
        configTemplate = (Join-Path $linuxPackageDirectory "examples\claude-desktop.mcp.template.json")
    }
    yakStage = [pscustomobject]@{
        path = $yakStageDirectory
        manifest = (Join-Path $yakStageDirectory "manifest.yml")
        package = $yakWinPackagePath
    }
    yakMacStage = [pscustomobject]@{
        path = $yakMacStageDirectory
        manifest = (Join-Path $yakMacStageDirectory "manifest.yml")
        package = $yakMacPackagePath
    }
    yakAnyStage = [pscustomobject]@{
        path = $yakAnyStageDirectory
        manifest = (Join-Path $yakAnyStageDirectory "manifest.yml")
        package = $yakAnyPackagePath
    }
}

$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryPath

Write-Host ""
Write-Host "Created artifacts:"
Write-Host "  Plugin (win): $($pluginArtifacts["win"])"
Write-Host "  Plugin (mac): $($pluginArtifacts["mac"])"
Write-Host "  Plugin (any): $($pluginArtifacts["any"])"
foreach ($publish in $bridgePublishes) {
    Write-Host "  Bridge ($($publish.rid)): $($publish.path)"
}
Write-Host "  Linux package: $linuxPackageDirectory"
Write-Host "  Yak stage (win): $yakStageDirectory"
Write-Host "  Yak stage (mac): $yakMacStageDirectory"
Write-Host "  Yak any stage: $yakAnyStageDirectory"
if ($null -ne $yakWinPackagePath) {
    Write-Host "  Yak package (win): $yakWinPackagePath"
}
if ($null -ne $yakMacPackagePath) {
    Write-Host "  Yak package (mac): $yakMacPackagePath"
}
if ($null -ne $yakAnyPackagePath) {
    Write-Host "  Yak package (any): $yakAnyPackagePath"
}
Write-Host "  Manifest: $summaryPath"
