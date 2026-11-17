[CmdletBinding()]
param(
 [string]$PackageDirectory,
 [string]$ReferenceDirectory,
 [string]$Compiler='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe',
 [switch]$Check,
 [string]$ExpectedSourceRevision
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compatibilityId=& (Join-Path $PSScriptRoot 'generate_game_compat.ps1') -Check:$Check -PassIdentity
& (Join-Path $PSScriptRoot 'generate_playback_control.ps1') -Check:$Check
if(-not $PackageDirectory){$PackageDirectory=Join-Path $repo 'build\owned_mmd'}
$packageRoot=[IO.Path]::GetFullPath($PackageDirectory)
$purpose='owned_mmd_scene'
$files=(Import-PowerShellDataFile (Join-Path $PSScriptRoot 'player_build_inputs.psd1')).OwnedRuntimeFiles
$manifestPath=Join-Path $packageRoot 'runtime_package.json'

function Assert-SourceRevision([string]$Revision,[bool]$Dirty) {
 if($ExpectedSourceRevision -and ($Revision -cne $ExpectedSourceRevision -or $Dirty)){
  throw 'Owned MMD runtime was not built from the expected clean source revision; rebuild it.'
 }
}
function Read-RuntimeFile([string]$File) {
 $item=Get-Item -LiteralPath (Join-Path $packageRoot $File)
 if($item.PSIsContainer){throw ('Owned MMD runtime artifact must be a file: '+$File)}
 [ordered]@{path=$File;bytes=$item.Length;sha256=(Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
}
function Assert-RuntimeManifest {
 if(-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)){throw 'Owned MMD runtime manifest missing; run tools/build_owned_mmd.ps1.'}
 $manifest=Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
 if($manifest.schema -ne 3 -or $manifest.purpose -ne $purpose){throw 'Owned MMD runtime requires the current schema 3 manifest; rebuild the runtime package.'}
 if($manifest.compatibility_id -cne $compatibilityId){throw 'Owned MMD runtime compatibility ID is stale; rebuild it.'}
 Assert-SourceRevision $manifest.source_revision $manifest.source_dirty
 $artifacts=@($manifest.files)
 if($artifacts.Count -ne $files.Count){throw 'Owned MMD runtime digest list is incomplete.'}
 foreach($file in $files){
  $record=@($artifacts | Where-Object path -CEQ $file)
  if($record.Count -ne 1){throw ('Owned MMD runtime digest is missing or duplicated: '+$file)}
  $actual=Read-RuntimeFile $file
  if($actual.bytes -ne $record[0].bytes -or $actual.sha256 -cne $record[0].sha256){
   throw ('Owned MMD runtime integrity check failed: '+$file)
  }
 }
}

if($Check){
 Assert-RuntimeManifest
}else{
 $buildRevision=(& git -C $repo rev-parse HEAD 2>$null)
 if($LASTEXITCODE -ne 0){$buildRevision='uncommitted'}
 $buildDirty=@(& git -C $repo status --porcelain --untracked-files=normal 2>$null).Count -ne 0
 Assert-SourceRevision $buildRevision $buildDirty
 if(-not $ReferenceDirectory){$ReferenceDirectory=$env:WWPOSER_MANAGED_REFERENCE_DIR}
 if(-not $ReferenceDirectory){$ReferenceDirectory=Join-Path $repo 'local\owned_mmd_references'}
 $referenceRoot=[IO.Path]::GetFullPath($ReferenceDirectory)
 $nativeDirectory=Join-Path $repo 'runtime\owned_mmd\native'
 $managedDirectory=Join-Path $repo 'runtime\owned_mmd\managed'
 $compilerPath=[IO.Path]::GetFullPath($Compiler)
 $references=@(Get-Content -LiteralPath (Join-Path $managedDirectory 'references.json') -Raw -Encoding UTF8 | ConvertFrom-Json)
 if($references.Count -ne 6 -or @($references | Select-Object -Unique).Count -ne 6){throw 'Owned MMD helper requires six distinct reference DLL names.'}
 foreach($name in $references){
  if($name -isnot [string] -or $name -notmatch '^[^/\\:]+\.dll$' -or [IO.Path]::GetFileName($name) -ne $name){throw ('Reference must be a DLL basename: '+$name)}
 }
 $referencePaths=@($references | ForEach-Object {Join-Path $referenceRoot $_})
 $sourcePath=Join-Path $managedDirectory 'ManagedBridge.cs'
 $generatedSourcePath=Join-Path $repo 'runtime/generated/GameCompat.generated.cs'
 $nativeScript=Join-Path $nativeDirectory 'build_observer.bat'
 foreach($path in @($compilerPath,$sourcePath,$generatedSourcePath,$nativeScript)+$referencePaths){
  if(-not (Test-Path -LiteralPath $path -PathType Leaf)){throw ('Owned MMD build input missing: '+$path)}
 }
 New-Item -ItemType Directory -Path $packageRoot -Force|Out-Null
 # A failed rebuild must not leave a previous success manifest.
 if(Test-Path -LiteralPath $manifestPath -PathType Leaf){Remove-Item -LiteralPath $manifestPath}
 $compileDirectory=Join-Path $repo 'build\owned_mmd_compile\managed'
 New-Item -ItemType Directory -Path $compileDirectory -Force|Out-Null
 $outputPath=Join-Path $packageRoot 'WwPoser.ColdLoad.ManagedBridge.OwnedMmdLite.dll'
 $arguments=@('/nologo','/utf8output','/target:library','/platform:anycpu','/langversion:latest','/nullable:disable','/nostdlib+','/optimize+','/deterministic+','/unsafe-','/warn:4','/warnaserror+',('/out:"'+$outputPath+'"'))
 $arguments+=@($referencePaths | ForEach-Object {'/reference:"'+$_+'"'})
 $arguments+='"'+$sourcePath+'"'
 $arguments+='"'+$generatedSourcePath+'"'
 $responsePath=Join-Path $compileDirectory 'compile.rsp'
 [IO.File]::WriteAllLines($responsePath,$arguments,[Text.UTF8Encoding]::new($false))
 & $compilerPath /noconfig ('@'+$responsePath)
 if($LASTEXITCODE -ne 0){throw 'Owned MMD helper build failed.'}
 & $nativeScript $packageRoot
 if($LASTEXITCODE -ne 0){throw 'Owned MMD native build failed.'}
 $artifacts=@($files | ForEach-Object { Read-RuntimeFile $_ })
 $endRevision=(& git -C $repo rev-parse HEAD 2>$null)
 if($LASTEXITCODE -ne 0){$endRevision='uncommitted'}
 $buildDirty=$buildDirty -or $endRevision -cne $buildRevision -or
     @(& git -C $repo status --porcelain --untracked-files=normal 2>$null).Count -ne 0
 Assert-SourceRevision $buildRevision $buildDirty
 $manifest=[ordered]@{schema=3;purpose=$purpose;source_revision=$buildRevision;source_dirty=$buildDirty;compatibility_id=$compatibilityId;files=$artifacts}
 [IO.File]::WriteAllText($manifestPath,($manifest|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
}
[ordered]@{status=if($Check){'owned_mmd_runtime_files_present'}else{'owned_mmd_runtime_built'};files=$files.Count;runtime_executed=$false;package=$packageRoot}|ConvertTo-Json -Compress
