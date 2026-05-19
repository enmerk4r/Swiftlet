[CmdletBinding()]
param(
    [string]$Target = "rhino8",
    [string]$Configuration = "Release",
    [string]$Branch,
    [string]$Workflow = "bridge-aot-artifacts.yml",
    [string]$CiArtifactRoot = "artifacts/ci-bridge",
    [string]$PrebuiltBridgeRoot = "artifacts/prebuilt-bridge",
    [switch]$SkipTests,
    [switch]$NoRestore,
    [switch]$AllowDirty,
    [int]$RunDiscoveryTimeoutSeconds = 120,
    [int]$RunPollIntervalSeconds = 10
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

function Assert-CommandExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommandName
    )

    if ($null -eq (Get-Command $CommandName -ErrorAction SilentlyContinue)) {
        throw "Required command was not found on PATH: $CommandName"
    }
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    Write-Host ""
    Write-Host "$FilePath $($Arguments -join ' ')"
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE"
    }
}

function Invoke-NativeCommandForOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }

    return ($output -join "`n").Trim()
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

function Get-WorkflowRunForHead {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkflowName,

        [Parameter(Mandatory = $true)]
        [string]$BranchName,

        [Parameter(Mandatory = $true)]
        [string]$HeadSha,

        [Parameter(Mandatory = $true)]
        [DateTime]$MinCreatedAtUtc
    )

    $json = Invoke-NativeCommandForOutput `
        -FilePath "gh" `
        -Arguments @(
            "run", "list",
            "--workflow", $WorkflowName,
            "--branch", $BranchName,
            "--event", "workflow_dispatch",
            "--limit", "20",
            "--json", "databaseId,headSha,status,conclusion,createdAt"
        )

    if ([string]::IsNullOrWhiteSpace($json)) {
        return $null
    }

    $runs = @($json | ConvertFrom-Json)
    return $runs |
        Where-Object { $_.headSha -eq $HeadSha -and ([DateTime]$_.createdAt) -ge $MinCreatedAtUtc } |
        Sort-Object { [DateTime]$_.createdAt } -Descending |
        Select-Object -First 1
}

function Wait-WorkflowRunCreated {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkflowName,

        [Parameter(Mandatory = $true)]
        [string]$BranchName,

        [Parameter(Mandatory = $true)]
        [string]$HeadSha,

        [Parameter(Mandatory = $true)]
        [DateTime]$MinCreatedAtUtc,

        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $run = Get-WorkflowRunForHead `
            -WorkflowName $WorkflowName `
            -BranchName $BranchName `
            -HeadSha $HeadSha `
            -MinCreatedAtUtc $MinCreatedAtUtc

        if ($null -ne $run) {
            return $run
        }

        Start-Sleep -Seconds 5
    }
    while ([DateTime]::UtcNow -lt $deadline)

    throw "Timed out waiting for a workflow_dispatch run for $WorkflowName on $BranchName at $HeadSha."
}

function Get-CompletedWorkflowRun {
    param(
        [Parameter(Mandatory = $true)]
        [int]$RunId
    )

    $json = Invoke-NativeCommandForOutput `
        -FilePath "gh" `
        -Arguments @("run", "view", [string]$RunId, "--json", "status,conclusion,url")

    return $json | ConvertFrom-Json
}

function Wait-WorkflowRunCompleted {
    param(
        [Parameter(Mandatory = $true)]
        [int]$RunId,

        [Parameter(Mandatory = $true)]
        [int]$PollIntervalSeconds
    )

    while ($true) {
        $run = Get-CompletedWorkflowRun -RunId $RunId
        Write-Host "Workflow run $RunId status: $($run.status)"

        if ($run.status -eq "completed") {
            if ($run.conclusion -ne "success") {
                throw "Workflow run $RunId completed with conclusion '$($run.conclusion)': $($run.url)"
            }

            return $run
        }

        Start-Sleep -Seconds $PollIntervalSeconds
    }
}

function Expand-BridgeArtifact {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DownloadedArtifactDirectory,

        [Parameter(Mandatory = $true)]
        [string]$ArtifactName,

        [Parameter(Mandatory = $true)]
        [string]$DestinationDirectory
    )

    $archivePath = Join-Path $DownloadedArtifactDirectory "$ArtifactName.tar.gz"
    $checksumPath = Join-Path $DownloadedArtifactDirectory "$ArtifactName.tar.gz.sha256"

    if (-not (Test-Path $archivePath)) {
        throw "Downloaded artifact archive was not found: $archivePath"
    }

    if (-not (Test-Path $checksumPath)) {
        throw "Downloaded artifact checksum was not found: $checksumPath"
    }

    $expectedHash = ((Get-Content -Path $checksumPath -Raw).Trim() -split '\s+')[0].ToUpperInvariant()
    $actualHash = (Get-FileHash -Algorithm SHA256 -Path $archivePath).Hash.ToUpperInvariant()
    if ($actualHash -ne $expectedHash) {
        throw "Checksum mismatch for $archivePath. Expected $expectedHash but got $actualHash."
    }

    New-CleanDirectory -Path $DestinationDirectory
    Invoke-NativeCommand -FilePath "tar" -Arguments @("-xzf", $archivePath, "-C", $DestinationDirectory)

    $bridgePath = Join-Path $DestinationDirectory "SwiftletBridge"
    if (-not (Test-Path $bridgePath)) {
        throw "Extracted bridge executable was not found: $bridgePath"
    }
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-RepoPath -BasePath $scriptDirectory -RelativePath "..\.."
$publishScriptPath = Resolve-RepoPath -BasePath $repoRoot -RelativePath "build\packaging\Publish-Target.ps1"
$ciArtifactRootPath = Resolve-RepoPath -BasePath $repoRoot -RelativePath $CiArtifactRoot
$prebuiltBridgeRootPath = Resolve-RepoPath -BasePath $repoRoot -RelativePath $PrebuiltBridgeRoot

Push-Location $repoRoot
try {
    Assert-CommandExists -CommandName "git"
    Assert-CommandExists -CommandName "gh"
    Assert-CommandExists -CommandName "dotnet"
    Assert-CommandExists -CommandName "tar"

    if ([string]::IsNullOrWhiteSpace($Branch)) {
        $Branch = Invoke-NativeCommandForOutput -FilePath "git" -Arguments @("branch", "--show-current")
        if ([string]::IsNullOrWhiteSpace($Branch)) {
            throw "Could not resolve the current branch. Pass -Branch explicitly."
        }
    }

    $dirtyStatus = Invoke-NativeCommandForOutput -FilePath "git" -Arguments @("status", "--porcelain")
    if (-not [string]::IsNullOrWhiteSpace($dirtyStatus) -and -not $AllowDirty) {
        throw "Working tree has uncommitted changes. Commit them first, or rerun with -AllowDirty if you intentionally want local packaging to differ from CI artifacts."
    }

    $headSha = Invoke-NativeCommandForOutput -FilePath "git" -Arguments @("rev-parse", "HEAD")
    Invoke-NativeCommand -FilePath "git" -Arguments @("fetch", "origin", "$($Branch):refs/remotes/origin/$Branch", "--quiet")
    $remoteHead = Invoke-NativeCommandForOutput -FilePath "git" -Arguments @("rev-parse", "refs/remotes/origin/$Branch")
    if ($remoteHead -ne $headSha) {
        throw "origin/$Branch is at $remoteHead, but local HEAD is $headSha. Push the branch before building release artifacts."
    }

    Write-Host "============================================"
    Write-Host "Building Swiftlet Yak package"
    Write-Host "============================================"
    Write-Host "Target: $Target"
    Write-Host "Configuration: $Configuration"
    Write-Host "Branch: $Branch"
    Write-Host "Commit: $headSha"
    Write-Host "Workflow: $Workflow"
    Write-Host "============================================"

    if (-not $SkipTests) {
        $testArguments = @("test", "Swiftlet.sln", "-c", $Configuration)
        if ($NoRestore) {
            $testArguments += "--no-restore"
        }

        Invoke-NativeCommand -FilePath "dotnet" -Arguments $testArguments
    }

    $workflowTriggerTimeUtc = [DateTime]::UtcNow.AddMinutes(-2)
    Invoke-NativeCommand -FilePath "gh" -Arguments @("workflow", "run", $Workflow, "--ref", $Branch)
    $run = Wait-WorkflowRunCreated `
        -WorkflowName $Workflow `
        -BranchName $Branch `
        -HeadSha $headSha `
        -MinCreatedAtUtc $workflowTriggerTimeUtc `
        -TimeoutSeconds $RunDiscoveryTimeoutSeconds

    $runId = [int]$run.databaseId
    Write-Host "Workflow run created: $runId"
    Wait-WorkflowRunCompleted -RunId $runId -PollIntervalSeconds $RunPollIntervalSeconds | Out-Null

    $downloadRoot = Join-Path $ciArtifactRootPath $runId
    New-CleanDirectory -Path $downloadRoot

    $artifactNames = @(
        "swiftlet-bridge-linux-x64",
        "swiftlet-bridge-osx-arm64"
    )

    foreach ($artifactName in $artifactNames) {
        Invoke-NativeCommand `
            -FilePath "gh" `
            -Arguments @("run", "download", [string]$runId, "--name", $artifactName, "--dir", $downloadRoot)
    }

    Expand-BridgeArtifact `
        -DownloadedArtifactDirectory $downloadRoot `
        -ArtifactName "swiftlet-bridge-linux-x64" `
        -DestinationDirectory (Join-Path $prebuiltBridgeRootPath "linux-x64")

    Expand-BridgeArtifact `
        -DownloadedArtifactDirectory $downloadRoot `
        -ArtifactName "swiftlet-bridge-osx-arm64" `
        -DestinationDirectory (Join-Path $prebuiltBridgeRootPath "osx-arm64")

    $publishArguments = @(
        "-ExecutionPolicy", "Bypass",
        "-File", $publishScriptPath,
        "-Target", $Target,
        "-Configuration", $Configuration,
        "-PrebuiltBridgeRoot", $prebuiltBridgeRootPath
    )

    if ($NoRestore) {
        $publishArguments += "-NoRestore"
    }

    Invoke-NativeCommand -FilePath "powershell" -Arguments $publishArguments
}
finally {
    Pop-Location
}
