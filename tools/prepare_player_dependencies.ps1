[CmdletBinding()]
param([string]$FfmpegArchive,[switch]$Download,[switch]$Check)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lock=Get-Content -LiteralPath (Join-Path $repo 'data\dependencies\ffmpeg.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$destination=Join-Path $PSScriptRoot 'recording_bin'
function Verify-Dependency([string]$Path,$Record){if((Get-Item -LiteralPath $Path).Length -ne $Record.bytes -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Record.sha256){throw ('Pinned dependency mismatch: '+$Path)}}
if(@($PSBoundParameters.Keys | Where-Object {$_ -in @('FfmpegArchive','Download','Check')}).Count -ne 1){throw 'Choose exactly one: -Check, -FfmpegArchive or -Download.'}
if(-not $Check){
 if($Download){$FfmpegArchive=Join-Path $env:TEMP 'wwposer-ffmpeg.zip';Invoke-WebRequest -Uri $lock.archive.url -OutFile $FfmpegArchive}
 Verify-Dependency $FfmpegArchive $lock.archive
 Add-Type -AssemblyName System.IO.Compression.FileSystem
 $package=[IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($FfmpegArchive))
 try{
  New-Item -ItemType Directory -Path $destination -Force|Out-Null
  foreach($property in $lock.files.PSObject.Properties){$target=Join-Path $destination $property.Name;$entry=$package.GetEntry($property.Value.archive_member);if(-not $entry){throw 'Pinned ZIP member missing'};[IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$target,$true);Verify-Dependency $target $property.Value}
 }finally{$package.Dispose()}
 Copy-Item -LiteralPath (Join-Path $repo 'data\dependencies\ffmpeg.README.txt') -Destination (Join-Path $destination 'README.txt')
 Copy-Item -LiteralPath (Join-Path $repo 'data\dependencies\ffmpeg.json') -Destination (Join-Path $destination 'source.json')
}
foreach($property in $lock.files.PSObject.Properties){Verify-Dependency (Join-Path $destination $property.Name) $property.Value}
[ordered]@{state='dependencies_verified';directory=$destination}|ConvertTo-Json -Compress
