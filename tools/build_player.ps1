[CmdletBinding()]
param(
    [string]$OutputDirectory,
    [switch]$Rebuild,
    [string]$DotNetExecutable = 'dotnet',
    [string]$OwnedMmdPackageDirectory,
    [string]$ReferenceDirectory,
    [string]$PublishDirectory,
    [switch]$PublicRelease,
    [switch]$SkipOfflineChecks
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$buildInputs = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'player_build_inputs.psd1')
function Get-SourceRevision {
    $revision = (& git -C $repo rev-parse HEAD 2>$null)
    if ($LASTEXITCODE -ne 0) { return 'uncommitted' }
    return $revision
}
function Test-CleanSource { return @(& git -C $repo status --porcelain --untracked-files=normal).Count -eq 0 }

$sourceRevision = Get-SourceRevision
if ($PublicRelease) {
    if (-not $Rebuild) { throw 'Public releases require -Rebuild so every native artifact matches the published source.' }
    if ($sourceRevision -eq 'uncommitted' -or -not (Test-CleanSource)) { throw 'Public releases require a clean committed source checkout.' }
}
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo ('dist\ww-mmd-player-csharp_' + (Get-Date -Format 'yyyyMMdd_HHmmss')) }
$destination = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $destination) { throw 'Bundle directory already exists; choose a fresh destination.' }
if (-not $OwnedMmdPackageDirectory) { $OwnedMmdPackageDirectory = Join-Path $repo 'build\owned_mmd' }
if ($Rebuild) {
    & (Join-Path $PSScriptRoot 'build_owned_mmd.ps1') -PackageDirectory $OwnedMmdPackageDirectory -ReferenceDirectory $ReferenceDirectory
    foreach ($script in $buildInputs.NativeBuilds) {
        & (Join-Path $repo $script)
        if ($LASTEXITCODE -ne 0) { throw ('Native build failed: ' + $script) }
    }
}
$releaseRevision = if ($PublicRelease) { $sourceRevision } else { $null }
& (Join-Path $PSScriptRoot 'build_owned_mmd.ps1') -PackageDirectory $OwnedMmdPackageDirectory -Check -ExpectedSourceRevision $releaseRevision
& (Join-Path $PSScriptRoot 'prepare_player_dependencies.ps1') -Check
if ($PublicRelease -and -not $SkipOfflineChecks) {
    & (Join-Path $PSScriptRoot 'run_offline_checks.ps1') -DotNetExecutable $DotNetExecutable -SkipNativeBuild
}
if (-not $PublishDirectory) { $PublishDirectory = Join-Path $repo 'desktop\WWPoser\publish' }
$publishDirectory = [IO.Path]::GetFullPath($PublishDirectory)
& $DotNetExecutable publish (Join-Path $repo 'desktop\WWPoser\WWPoser.csproj') -c Release --self-contained true -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true -p:NuGetAudit=false `
    --source 'https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json' -o $publishDirectory --nologo
if ($LASTEXITCODE -ne 0) { throw 'C# desktop publish failed.' }

$files = @($buildInputs.NativeOutputs) + @($buildInputs.Files)
foreach ($directory in $buildInputs.Directories) {
    $files += @(Get-ChildItem -LiteralPath (Join-Path $repo $directory) -Recurse -File | ForEach-Object { $_.FullName.Substring($repo.Length + 1) })
}
foreach ($file in $files) {
    if (-not (Test-Path -LiteralPath (Join-Path $repo $file) -PathType Leaf)) { throw ('Required artifact missing: ' + $file) }
}
New-Item -ItemType Directory -Path $destination | Out-Null
Copy-Item -LiteralPath (Join-Path $publishDirectory 'WWPoser.exe') -Destination (Join-Path $destination 'WWPoser.exe')
foreach ($file in ($files | Select-Object -Unique)) {
    $target = Join-Path $destination $file
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $repo $file) -Destination $target
}

$ownedSource = [IO.Path]::GetFullPath($OwnedMmdPackageDirectory)
$ownedManifestPath = Join-Path $ownedSource 'runtime_package.json'
$ownedManifest = Get-Content -LiteralPath $ownedManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$ownedTarget = Join-Path $destination 'runtime\owned_mmd_lite'
New-Item -ItemType Directory -Path $ownedTarget | Out-Null
# build_owned_mmd.ps1 -Check above verified the manifest's file names and digests.
foreach ($relative in $ownedManifest.files) { Copy-Item -LiteralPath (Join-Path $ownedSource $relative) -Destination (Join-Path $ownedTarget $relative) }
Copy-Item -LiteralPath $ownedManifestPath -Destination (Join-Path $ownedTarget 'runtime_package.json')
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE') -Destination (Join-Path $destination 'LICENSE')
$launcher = @'
@echo off
cd /d "%~dp0"
start "" "%~dp0WWPoser.exe" %*
'@
[IO.File]::WriteAllText((Join-Path $destination 'start.cmd'), $launcher, [Text.ASCIIEncoding]::new())

$sourceUrl = 'https://github.com/AKRTR465/Wuwa-Poser'
if ($PublicRelease -and ((Get-SourceRevision) -cne $sourceRevision -or -not (Test-CleanSource))) {
    throw 'The source changed during the release build; discard the incomplete package and rebuild.'
}
$sourceLine = if ($PublicRelease) { "Source: $sourceUrl/tree/$sourceRevision" } else { "Source workspace: $repo" }
$notice = "Wuwa Poser - Windows x64 portable package (AGPL-3.0).`n$sourceLine`nProject origins and dependencies: THIRD_PARTY_NOTICES.md.`n" +
    "Python is not required. .NET Windows Desktop runtime is included.`nUser VMD, music and game reference assemblies are not included.`n"
[IO.File]::WriteAllText((Join-Path $destination 'SOURCE_NOTICE.txt'), $notice, [Text.UTF8Encoding]::new($false))
$entries = Get-ChildItem -LiteralPath $destination -Recurse -File | ForEach-Object {
    [ordered]@{
        path = $_.FullName.Substring($destination.Length + 1).Replace('\', '/')
        bytes = $_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}
$manifest = [ordered]@{
    schema = 4; state = 'local_bundle_staged'; created_at = (Get-Date).ToString('o')
    source_url = $sourceUrl; source_revision = $sourceRevision; license = 'AGPL-3.0'
    entrypoint = 'WWPoser.exe'; runtime = 'csharp-net10-wpf'; python_required = $false; self_contained = $true; files = @($entries)
}
if (-not $PublicRelease) { $manifest.source_root = $repo }
[IO.File]::WriteAllText((Join-Path $destination 'manifest.json'), ($manifest | ConvertTo-Json -Depth 12), [Text.UTF8Encoding]::new($false))
$validation = Start-Process -FilePath (Join-Path $destination 'WWPoser.exe') -ArgumentList '--validate-bundle' -WorkingDirectory $destination -WindowStyle Hidden -PassThru -Wait
if ($validation.ExitCode -ne 0) { throw 'C# bundle validation failed; keep the output for diagnostics.' }
& (Join-Path $PSScriptRoot 'validate_owned_character_catalog.ps1') -Root $destination
& (Join-Path $PSScriptRoot 'validate_character_forms.ps1') -Root $destination
[ordered]@{ state = 'local_bundle_ready'; directory = $destination; entrypoint = (Join-Path $destination 'WWPoser.exe'); python_required = $false; published = $false } | ConvertTo-Json -Compress
