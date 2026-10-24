[CmdletBinding()]
param([string]$OutputDirectory,[switch]$Rebuild,[string]$DotNetExecutable='dotnet',[string]$OwnedMmdPackageDirectory,[string]$ReferenceDirectory,[string]$PublishDirectory,[switch]$PublicRelease)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(-not $OutputDirectory){$OutputDirectory=Join-Path $repo ('dist\ww-mmd-player-csharp_'+(Get-Date -Format 'yyyyMMdd_HHmmss'))}
$destination=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $destination){throw 'Bundle directory already exists; choose a fresh destination.'}
if(-not $OwnedMmdPackageDirectory){$OwnedMmdPackageDirectory=Join-Path $repo 'build\owned_mmd'}
if($Rebuild){
 & (Join-Path $PSScriptRoot 'build_owned_mmd.ps1') -PackageDirectory $OwnedMmdPackageDirectory -ReferenceDirectory $ReferenceDirectory
 foreach($script in @('probes\v20_recon\build.bat','probes\v20_recon\build_accessory_recon.bat','probes\v20_recon\build_native_graph_discovery.bat','probes\v20_recon\build_native_identity_bootstrap.bat','probes\v20_recon\build_publish_probe.bat','probes\v20_recon\build_puerts_scene_sampler.bat','probes\v20_recon\build_stage_residency.bat','probes\v20_recon\build_mmd_squad_residency.bat','tools\mmd_build_vmd_cli.bat','tools\mmd_build_face.bat','tools\mmd_build_camera.bat')){
  & (Join-Path $repo $script)
  if($LASTEXITCODE -ne 0){throw ('Native build failed: '+$script)}
 }
}
& (Join-Path $PSScriptRoot 'build_owned_mmd.ps1') -PackageDirectory $OwnedMmdPackageDirectory -Check
if(-not $PublishDirectory){$PublishDirectory=Join-Path $repo 'desktop\WWPoser\publish'}
$publishDirectory=[IO.Path]::GetFullPath($PublishDirectory)
& $DotNetExecutable publish (Join-Path $repo 'desktop\WWPoser\WWPoser.csproj') -c Release --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:NuGetAudit=false --source 'https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json' -o $publishDirectory --nologo
if($LASTEXITCODE -ne 0){throw 'C# desktop publish failed.'}
$files=@(
 'probes\v20_recon\mmd_squad_residency.dll','probes\v20_recon\stage_residency.dll','probes\v20_recon\puerts_scene_sampler.dll',
 'probes\v20_recon\hook_install_v20.exe','probes\v20_recon\recon_carrier_v20.dll','probes\v20_recon\accessory_recon.dll',
 'probes\v20_recon\publish_probe_v21.dll','probes\v20_recon\native_graph_discovery.dll','probes\v20_recon\native_identity_bootstrap.dll',
 'tools\mmd_outputs\mmd_vmd_cli.exe','tools\mmd_outputs\mmd_face_compile.exe','tools\mmd_outputs\mmd_camera_compile.exe',
 'tools\recording_bin\ffmpeg.exe','tools\recording_bin\ffprobe.exe','tools\recording_bin\LICENSE.txt','tools\recording_bin\README.txt','tools\recording_bin\source.json',
 'data\puerts_sampler_camera_contract.json','data\mono_managed_readonly_contract.json','data\csharp_scene_team_readonly_contract.json',
 'data\stage_identity_property_contract.json','data\stage_identity_entity_contract.json','data\stage_native_runtime_contract.json',
 'data\bone_chains.json','data\morph_targets.json','data\camera_runtime_contract.json','data\native_identity_registration_contract.json',
 'data\native_pose_empty_channel_contract.json',
 'tools\MMD_PLAYER.md','tools\MMD_SQUAD.md','docs\mmd_squad_session_contract.md','docs\unified_player_20261023.md','docs\dynamic_actor_player_20261023.md',
 'docs\owned_mmd_integration_20261022.md','docs\owned_mmd_source_cleanup_20261022.md','docs\character_cold_load_progress_20261022.md',
 'docs\owned_character_catalog_20261023.md','docs\shared_native_physics_implementation_20261023.md','docs\native_target_handshake_review_20261023.md',
 'docs\all_characters_native_mmd_20261023.md','docs\all_characters_native_mmd_offline_20261023.json','docs\native_mmd_resume_20261023.md'
 'docs\native_physics_catalog_adaptation_20261023.md',
 'README.md','CHANGELOG.md','THIRD_PARTY_NOTICES.md','docs\BUILDING.md',
 'data\dependencies\ffmpeg.json','data\dependencies\ffmpeg.README.txt'
)
foreach($directory in @('licenses')){$files+=@(Get-ChildItem -LiteralPath (Join-Path $repo $directory) -Recurse -File|ForEach-Object {$_.FullName.Substring($repo.Length+1)})}
foreach($directory in @('data\characters','data\runtime_contracts')){$files+=@(Get-ChildItem -LiteralPath (Join-Path $repo $directory) -Recurse -File|ForEach-Object {$_.FullName.Substring($repo.Length+1)})}
$files+='data\native_graphs\index.json'
$files+='data\native_graphs\owned_ui_source_certificate.json'
$files+=@(Get-ChildItem -LiteralPath (Join-Path $repo 'data\native_graphs\runtime') -File|ForEach-Object {$_.FullName.Substring($repo.Length+1)})
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
$sourceLine=if($PublicRelease){"Source: $sourceUrl/tree/$sourceRevision"}else{"Source workspace: $repo"}
$notice="Wuwa Poser - Windows x64 portable package (AGPL-3.0).`n$sourceLine`nProject origins and dependencies: THIRD_PARTY_NOTICES.md.`nPython is not required. .NET Windows Desktop runtime is included.`nUser VMD, music and game reference assemblies are not included.`n"
[IO.File]::WriteAllText((Join-Path $destination 'SOURCE_NOTICE.txt'),$notice,[Text.UTF8Encoding]::new($false))
$entries=Get-ChildItem -LiteralPath $destination -Recurse -File|ForEach-Object {[ordered]@{path=$_.FullName.Substring($destination.Length+1).Replace('\','/');bytes=$_.Length}}
$manifest=[ordered]@{schema=3;state='local_bundle_staged';created_at=(Get-Date).ToString('o');source_url=$sourceUrl;source_revision=$sourceRevision;license='AGPL-3.0';entrypoint='WWPoser.exe';runtime='csharp-net10-wpf';python_required=$false;self_contained=$true;files=@($entries)}
if(-not $PublicRelease){$manifest.source_root=$repo}
[IO.File]::WriteAllText((Join-Path $destination 'manifest.json'),($manifest|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
$validation=Start-Process -FilePath (Join-Path $destination 'WWPoser.exe') -ArgumentList '--validate-bundle' -WorkingDirectory $destination -WindowStyle Hidden -PassThru -Wait
if($validation.ExitCode -ne 0){throw 'C# bundle validation failed; keep the output for diagnostics.'}
[ordered]@{state='local_bundle_ready';directory=$destination;entrypoint=(Join-Path $destination 'WWPoser.exe');python_required=$false;published=$false}|ConvertTo-Json -Compress
