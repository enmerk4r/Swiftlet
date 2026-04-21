[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Version,
    [string]$ArtifactsRoot = "artifacts/publish",
    [switch]$NoRestore,
    [string]$PluginOutputDir,
    [switch]$SkipBridgePublish
)

$publishScriptPath = Join-Path $PSScriptRoot "Publish-Target.ps1"
$arguments = @{
    Configuration = $Configuration
    Target = "rhino8"
    ArtifactsRoot = $ArtifactsRoot
}

if ($PSBoundParameters.ContainsKey("Version")) {
    $arguments.Version = $Version
}

if ($PSBoundParameters.ContainsKey("NoRestore")) {
    $arguments.NoRestore = $NoRestore
}

if ($PSBoundParameters.ContainsKey("PluginOutputDir")) {
    $arguments.PluginOutputDir = $PluginOutputDir
}

if ($PSBoundParameters.ContainsKey("SkipBridgePublish")) {
    $arguments.SkipBridgePublish = $SkipBridgePublish
}

& $publishScriptPath @arguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
