<#
.SYNOPSIS
    Creates a Yak package from the build output.

.DESCRIPTION
    This script is called as a post-build step to create Yak packages for distribution.
    It creates a dist-X.X.X folder, copies the necessary files, generates manifest.yml
    from the template, and runs yak build to create the .yak package.

    NOTE: Swiftlet is Windows-only due to System.Drawing (GDI+) and WinForms dependencies.

.PARAMETER Configuration
    The build configuration (e.g., Release-Rhino6, Release-Rhino7, Release-Rhino8)

.PARAMETER Version
    The version of the plugin (e.g., 0.2.0)

.PARAMETER OutputDir
    The build output directory containing the compiled files

.PARAMETER ProjectDir
    The project directory (where the .csproj is located)

.EXAMPLE
    .\Build-YakPackage.ps1 -Configuration "Release-Rhino7" -Version "0.2.0" -OutputDir "bin\Release-Rhino7" -ProjectDir "C:\path\to\Swiftlet"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Configuration,

    [Parameter(Mandatory=$true)]
    [string]$Version,

    [Parameter(Mandatory=$true)]
    [string]$OutputDir,

    [Parameter(Mandatory=$true)]
    [string]$ProjectDir
)

# Only run for Release configurations
if (-not $Configuration.StartsWith("Release")) {
    Write-Host "Skipping Yak package creation for Debug configuration."
    exit 0
}

function Get-ReferencedAssemblyVersion {
    param(
        [Parameter(Mandatory=$true)]
        [string]$AssemblyPath,

        [Parameter(Mandatory=$true)]
        [string]$ReferenceName
    )

    if (-not (Test-Path $AssemblyPath)) {
        throw "Assembly not found: $AssemblyPath"
    }

    $assembly = [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($AssemblyPath)
    $reference = $assembly.GetReferencedAssemblies() | Where-Object { $_.Name -eq $ReferenceName } | Select-Object -First 1
    if ($null -eq $reference) {
        throw "Assembly '$AssemblyPath' does not reference '$ReferenceName'."
    }

    return $reference.Version
}

# Determine Rhino version from configuration
if ($Configuration -match "Rhino(\d+)") {
    $RhinoMajor = $Matches[1]
} else {
    Write-Error "Could not determine Rhino version from configuration: $Configuration"
    exit 1
}

# The Rhino version (rh6_x, rh7_0, rh8_0) is automatically inferred by yak
# from the RhinoCommon version embedded in the compiled .gha assembly.
# NOTE: Using "win" because Swiftlet has Windows-only dependencies:
#   - System.Drawing (GDI+) for bitmap components
#   - System.Windows.Forms for clipboard/menu operations
#   - Windows-specific process APIs for OAuth browser launch
$Platform = "win"

# Use version as-is (keep full semver format)
$NormalizedVersion = $Version

# Find yak.exe in common Rhino installation paths
$YakExe = $null
$YakSearchPaths = @(
    "C:\Program Files\Rhino 8\System\Yak.exe",
    "C:\Program Files\Rhino 7\System\Yak.exe",
    "C:\Program Files\Rhino 6\System\Yak.exe"
)

# First check if yak is in PATH
$yakInPath = Get-Command "yak" -ErrorAction SilentlyContinue
if ($yakInPath) {
    $YakExe = $yakInPath.Source
} else {
    # Search common installation paths
    foreach ($path in $YakSearchPaths) {
        if (Test-Path $path) {
            $YakExe = $path
            break
        }
    }
}

if (-not $YakExe) {
    Write-Warning "============================================"
    Write-Warning "Yak CLI tool not found!"
    Write-Warning "Please install Yak or add it to your PATH."
    Write-Warning "Yak is typically found in: C:\Program Files\Rhino X\System\Yak.exe"
    Write-Warning "Files have been copied to dist folder, but .yak package was not created."
    Write-Warning "You can run 'yak build --platform=win' manually from the dist folder."
    Write-Warning "============================================"
    exit 0  # Exit with success so build doesn't fail
}

Write-Host "Using Yak: $YakExe"

# Paths
$YakDir = Join-Path $ProjectDir "..\Yak"
$DistRootDir = Join-Path $YakDir "dist-$NormalizedVersion"
$DistDir = Join-Path $DistRootDir "rhino$RhinoMajor"
$TemplateFile = Join-Path $YakDir "manifest-template.yml"
$ManifestFile = Join-Path $DistDir "manifest.yml"
$BridgeProjectDir = Join-Path $ProjectDir "..\SwiftletBridge"
$PluginAssemblyPath = Join-Path $OutputDir "Swiftlet.gha"

Write-Host "============================================"
Write-Host "Building Yak Package"
Write-Host "============================================"
Write-Host "Configuration: $Configuration"
Write-Host "Version: $NormalizedVersion"
Write-Host "Rhino Version: $RhinoMajor (auto-detected from RhinoCommon in .gha)"
Write-Host "Platform: $Platform"
Write-Host "Output Dir: $OutputDir"
Write-Host "Dist Root: $DistRootDir"
Write-Host "Dist Dir: $DistDir"
Write-Host "============================================"

# Validate that the built plugin references the expected Rhino major version.
try {
    $rhinoCommonVersion = Get-ReferencedAssemblyVersion -AssemblyPath $PluginAssemblyPath -ReferenceName "RhinoCommon"
    $grasshopperVersion = Get-ReferencedAssemblyVersion -AssemblyPath $PluginAssemblyPath -ReferenceName "Grasshopper"
} catch {
    Write-Error $_
    exit 1
}

Write-Host "RhinoCommon reference: $rhinoCommonVersion"
Write-Host "Grasshopper reference: $grasshopperVersion"

if ($rhinoCommonVersion.Major.ToString() -ne $RhinoMajor -or $grasshopperVersion.Major.ToString() -ne $RhinoMajor) {
    Write-Error "Built plugin references Rhino $($rhinoCommonVersion.Major)/Grasshopper $($grasshopperVersion.Major), but configuration requested Rhino $RhinoMajor."
    exit 1
}

# Recreate the per-Rhino staging folder so staged artifacts cannot bleed between Rhino versions.
if (Test-Path $DistDir) {
    Write-Host "Removing existing staging directory: $DistDir"
    Remove-Item -Recurse -Force $DistDir
}

Write-Host "Creating directory: $DistDir"
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

# Clean up old mcp folder and macOS files from previous builds
$McpDir = Join-Path $DistDir "mcp"
if (Test-Path $McpDir) {
    Write-Host "Removing old mcp folder..."
    Remove-Item -Recurse -Force $McpDir
}
$oldMacFiles = @("SwiftletBridge-macos-x64", "SwiftletBridge-macos-arm64")
foreach ($oldFile in $oldMacFiles) {
    $oldPath = Join-Path $DistDir $oldFile
    if (Test-Path $oldPath) {
        Write-Host "Removing old file: $oldFile"
        Remove-Item -Force $oldPath
    }
}

# Files to copy (relative to OutputDir)
$FilesToCopy = @(
    "Swiftlet.gha",
    "HtmlAgilityPack.dll",
    "Newtonsoft.Json.dll",
    "QRCoder.dll"
)

# Copy files to dist folder
Write-Host "Copying files to dist folder..."
foreach ($file in $FilesToCopy) {
    $sourcePath = Join-Path $OutputDir $file
    if (Test-Path $sourcePath) {
        Write-Host "  Copying: $file"
        Copy-Item $sourcePath $DistDir -Force
    } else {
        Write-Warning "  File not found: $sourcePath"
    }
}

# ==================== Build SwiftletBridge (Windows only) ====================
Write-Host ""
Write-Host "============================================"
Write-Host "Building SwiftletBridge MCP Bridge"
Write-Host "============================================"

$BridgeCsproj = Join-Path $BridgeProjectDir "SwiftletBridge.csproj"

if (Test-Path $BridgeCsproj) {
    $publishDir = Join-Path $BridgeProjectDir "bin\publish\win-x64"

    Write-Host "  Publishing SwiftletBridge for win-x64..."

    # Publish as self-contained single file
    $publishArgs = @(
        "publish",
        $BridgeCsproj,
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:PublishTrimmed=true",
        "-p:EnableCompressionInSingleFile=true",
        "-o", $publishDir
    )

    $process = Start-Process -FilePath "dotnet" -ArgumentList $publishArgs -Wait -NoNewWindow -PassThru

    if ($process.ExitCode -eq 0) {
        $sourceExe = Join-Path $publishDir "SwiftletBridge.exe"

        if (Test-Path $sourceExe) {
            $destPath = Join-Path $DistDir "SwiftletBridge.exe"
            Copy-Item $sourceExe $destPath -Force
            Write-Host "    Created: SwiftletBridge.exe"
        } else {
            Write-Warning "    Published executable not found: $sourceExe"
        }
    } else {
        Write-Warning "    Failed to publish SwiftletBridge (exit code: $($process.ExitCode))"
    }
} else {
    Write-Warning "SwiftletBridge project not found at: $BridgeCsproj"
    Write-Warning "MCP bridge will not be included in the package."
}

# ==================== Generate manifest.yml ====================
Write-Host ""
Write-Host "Generating manifest.yml from template..."
if (Test-Path $TemplateFile) {
    $manifestContent = Get-Content $TemplateFile -Raw
    $manifestContent = $manifestContent -replace '\{\{VERSION\}\}', $NormalizedVersion
    Set-Content -Path $ManifestFile -Value $manifestContent -NoNewline
    Write-Host "  Manifest created: $ManifestFile"
} else {
    Write-Error "Template file not found: $TemplateFile"
    exit 1
}

# ==================== Run yak build or create ZIP ====================
Write-Host ""

if ($RhinoMajor -eq "6") {
    # Rhino 6 doesn't have Yak CLI - create a ZIP file instead
    Write-Host "Building ZIP package for Rhino 6 (Yak CLI not available)..."

    $zipFileName = "swiftlet-$NormalizedVersion-rh6_18-win.zip"
    $zipFilePath = Join-Path $DistDir $zipFileName

    # Remove existing zip if present
    if (Test-Path $zipFilePath) {
        Remove-Item $zipFilePath -Force
    }

    # Create ZIP from dist folder contents
    $filesToZip = Get-ChildItem -Path $DistDir -Exclude "*.zip", "*.yak"
    Compress-Archive -Path $filesToZip.FullName -DestinationPath $zipFilePath -Force

    Write-Host "ZIP package created successfully!"
    Write-Host "  Created: $zipFileName"
    Write-Host ""
    Write-Host "Note: Rename .zip to .yak before publishing to Yak."
} else {
    # Rhino 7+ has Yak CLI
    Write-Host "Building Yak package..."
    Push-Location $DistDir
    try {
        # Build the yak package with the specified platform
        # The Rhino version (rh6_x, rh7_0, rh8_0) is auto-detected from the .gha assembly
        $yakArgs = @("build", "--platform=$Platform")
        Write-Host "  Running: yak $($yakArgs -join ' ')"

        $process = Start-Process -FilePath $YakExe -ArgumentList $yakArgs -Wait -NoNewWindow -PassThru

        if ($process.ExitCode -eq 0) {
            Write-Host "Yak package created successfully!"

            # List the created .yak files
            $yakFiles = Get-ChildItem -Path $DistDir -Filter "*.yak"
            foreach ($yakFile in $yakFiles) {
                Write-Host "  Created: $($yakFile.Name)"
            }
        } else {
            Write-Error "Yak build failed with exit code: $($process.ExitCode)"
            exit $process.ExitCode
        }
    } finally {
        Pop-Location
    }
}

Write-Host "============================================"
Write-Host "Yak package build complete!"
Write-Host "============================================"
Write-Host ""
