[CmdletBinding()]
param([string]$OutputDirectory,[switch]$Rebuild,[string]$DotNetExecutable='dotnet',[string]$OwnedMmdPackageDirectory,[string]$ReferenceDirectory,[string]$PublishDirectory,[switch]$PublicRelease,[switch]$SkipOfflineChecks)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$buildInputs=Import-PowerShellDataFile (Join-Path $PSScriptRoot 'player_build_inputs.psd1')
$nativeTools=@($buildInputs.Observers)+@($buildInputs.Compilers)
$releaseRevision=$null
if($PublicRelease){
 if(-not $Rebuild){throw 'Public releases require -Rebuild so every native artifact matches the published source.'}
 $releaseRevision=(& git -C $repo rev-parse HEAD 2>$null)
 if($LASTEXITCODE -ne 0 -or @(& git -C $repo status --porcelain --untracked-files=normal).Count -ne 0){
  throw 'Public releases require a clean committed source checkout.'
 }
}
if(-not $OutputDirectory){$OutputDirectory=Join-Path $repo ('dist\ww-mmd-player-csharp_'+(Get-Date -Format 'yyyyMMdd_HHmmss'))}
$destination=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $destination){throw 'Bundle directory already exists; choose a fresh destination.'}
if(-not $OwnedMmdPackageDirectory){$OwnedMmdPackageDirectory=Join-Path $repo 'build\owned_mmd'}
if($Rebuild){
 & (Join-Path $PSScriptRoot 'build_owned_mmd.ps1') -PackageDirectory $OwnedMmdPackageDirectory -ReferenceDirectory $ReferenceDirectory
 foreach($tool in $nativeTools){
  & (Join-Path $repo $tool.Build)
  if($LASTEXITCODE -ne 0){throw ('Native build failed: '+$tool.Build)}
 }
}
& (Join-Path $PSScriptRoot 'build_owned_mmd.ps1') -PackageDirectory $OwnedMmdPackageDirectory -Check -ExpectedSourceRevision $releaseRevision
& (Join-Path $PSScriptRoot 'prepare_player_dependencies.ps1') -Check
if($PublicRelease -and -not $SkipOfflineChecks){
 & (Join-Path $PSScriptRoot 'run_offline_checks.ps1') -DotNetExecutable $DotNetExecutable -SkipCompilerBuild
}
if(-not $PublishDirectory){$PublishDirectory=Join-Path $repo 'desktop\WWPoser\publish'}
$publishDirectory=[IO.Path]::GetFullPath($PublishDirectory)
& $DotNetExecutable publish (Join-Path $repo 'desktop\WWPoser\WWPoser.csproj') -c Release --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:NuGetAudit=false --source 'https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json' -o $publishDirectory --nologo
if($LASTEXITCODE -ne 0){throw 'C# desktop publish failed.'}
$files=@($nativeTools | ForEach-Object Output)+@($buildInputs.Files)
foreach($directory in $buildInputs.Directories){$files+=@(Get-ChildItem -LiteralPath (Join-Path $repo $directory) -Recurse -File|ForEach-Object {$_.FullName.Substring($repo.Length+1)})}
foreach($file in $files){if(-not (Test-Path -LiteralPath (Join-Path $repo $file) -PathType Leaf)){throw ('Required artifact missing: '+$file)}}
New-Item -ItemType Directory -Path $destination|Out-Null
Copy-Item -LiteralPath (Join-Path $publishDirectory 'WWPoser.exe') -Destination (Join-Path $destination 'WWPoser.exe')
foreach($file in ($files|Select-Object -Unique)){$target=Join-Path $destination $file;New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force|Out-Null;Copy-Item -LiteralPath (Join-Path $repo $file) -Destination $target}
$ownedSource=[IO.Path]::GetFullPath($OwnedMmdPackageDirectory)
$ownedManifestPath=Join-Path $ownedSource 'runtime_package.json'
if(-not (Test-Path -LiteralPath $ownedManifestPath -PathType Leaf)){throw 'Owned MMD runtime package missing; run tools/build_owned_mmd.ps1.'}
$ownedManifest=Get-Content -LiteralPath $ownedManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
if($ownedManifest.purpose -ne 'single_owned_character_native_mmd'){throw 'Owned MMD runtime package purpose invalid.'}
$ownedTarget=Join-Path $destination 'runtime\owned_mmd_lite'
New-Item -ItemType Directory -Path $ownedTarget|Out-Null
foreach($relative in $ownedManifest.files){
 if([IO.Path]::IsPathFullyQualified($relative) -or $relative.Contains(':') -or ($relative.Replace('\','/').Split('/') | Where-Object {$_ -in @('','.', '..')})){throw ('Invalid owned MMD package path: '+$relative)}
 $sourcePath=[IO.Path]::GetFullPath((Join-Path $ownedSource $relative))
 if(-not $sourcePath.StartsWith($ownedSource.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $sourcePath -PathType Leaf)){throw ('Owned MMD package file missing: '+$relative)}
 $targetPath=Join-Path $ownedTarget $relative
 New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($targetPath)) -Force|Out-Null
 Copy-Item -LiteralPath $sourcePath -Destination $targetPath
}
Copy-Item -LiteralPath $ownedManifestPath -Destination (Join-Path $ownedTarget 'runtime_package.json')
# Preserve historical admission records when packaging; current releases also
# use the per-user shared ledger so portable copies cannot overlap sessions.
$ownedUsage=Join-Path $ownedSource 'session_usage'
if(-not $PublicRelease -and (Test-Path -LiteralPath $ownedUsage -PathType Container)){
 $targetUsage=Join-Path $ownedTarget 'session_usage'
 New-Item -ItemType Directory -Path $targetUsage|Out-Null
 foreach($marker in Get-ChildItem -LiteralPath $ownedUsage -File | Where-Object { $_.Name -match '_owned_mmd_(lite|session)\.json$' }){Copy-Item -LiteralPath $marker.FullName -Destination (Join-Path $targetUsage $marker.Name)}
}
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE') -Destination (Join-Path $destination 'LICENSE')
$launcher=@'
@echo off
cd /d "%~dp0"
start "" "%~dp0WWPoser.exe" %*
'@
[IO.File]::WriteAllText((Join-Path $destination 'start.cmd'),$launcher,[Text.ASCIIEncoding]::new())
$ownedLauncher=@'
@echo off
cd /d "%~dp0"
start "" "%~dp0WWPoser.exe" --owned-mmd %*
'@
[IO.File]::WriteAllText((Join-Path $destination 'start-owned-mmd.cmd'),$ownedLauncher,[Text.ASCIIEncoding]::new())
$sourceUrl='https://github.com/AKRTR465/Wuwa-Poser'
$sourceRevision=(& git -C $repo rev-parse HEAD 2>$null)
if($LASTEXITCODE -ne 0){$sourceRevision='uncommitted'}
if($PublicRelease -and ($sourceRevision -cne $releaseRevision -or @(& git -C $repo status --porcelain --untracked-files=normal).Count -ne 0)){
 throw 'The source changed during the release build; discard the incomplete package and rebuild.'
}
$sourceLine=if($PublicRelease){"Source: $sourceUrl/tree/$sourceRevision"}else{"Source workspace: $repo"}
$notice="Wuwa Poser - Windows x64 portable package (AGPL-3.0).`n$sourceLine`nProject origins and dependencies: THIRD_PARTY_NOTICES.md.`nPython is not required. .NET Windows Desktop runtime is included.`nUser VMD, music and game reference assemblies are not included.`n"
[IO.File]::WriteAllText((Join-Path $destination 'SOURCE_NOTICE.txt'),$notice,[Text.UTF8Encoding]::new($false))
$entries=Get-ChildItem -LiteralPath $destination -Recurse -File|ForEach-Object {[ordered]@{path=$_.FullName.Substring($destination.Length+1).Replace('\','/');bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}}
$manifest=[ordered]@{schema=4;state='local_bundle_staged';created_at=(Get-Date).ToString('o');source_url=$sourceUrl;source_revision=$sourceRevision;license='AGPL-3.0';entrypoint='WWPoser.exe';runtime='csharp-net10-wpf';python_required=$false;self_contained=$true;files=@($entries)}
if(-not $PublicRelease){$manifest.source_root=$repo}
[IO.File]::WriteAllText((Join-Path $destination 'manifest.json'),($manifest|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
$validation=Start-Process -FilePath (Join-Path $destination 'WWPoser.exe') -ArgumentList '--validate-bundle' -WorkingDirectory $destination -WindowStyle Hidden -PassThru -Wait
if($validation.ExitCode -ne 0){throw 'C# bundle validation failed; keep the output for diagnostics.'}
& (Join-Path $PSScriptRoot 'validate_owned_character_catalog.ps1') -Root $destination
& (Join-Path $PSScriptRoot 'validate_character_forms.ps1') -Root $destination
[ordered]@{state='local_bundle_ready';directory=$destination;entrypoint=(Join-Path $destination 'WWPoser.exe');python_required=$false;published=$false}|ConvertTo-Json -Compress
