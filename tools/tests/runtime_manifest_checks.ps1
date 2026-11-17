[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('wwposer-manifest-' + [Guid]::NewGuid().ToString('N'))
$builder = Join-Path $repo 'tools/build_owned_mmd.ps1'
$revision = '1111111111111111111111111111111111111111'
$files = @((Import-PowerShellDataFile (Join-Path $repo 'tools/player_build_inputs.psd1')).OwnedRuntimeFiles)
$script:wwManifestChecks = 0
function Reject([scriptblock]$Action, [string]$Label) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    if (-not $rejected) { throw ('Runtime manifest accepted ' + $Label) }
    $script:wwManifestChecks++
}
try {
    [IO.Directory]::CreateDirectory($fixture) | Out-Null
    foreach ($name in $files) { [IO.File]::WriteAllText((Join-Path $fixture $name), 'original') }
    $manifest = [ordered]@{
        schema = 3; purpose = 'owned_mmd_scene'
        source_revision = $revision; source_dirty = $false
        compatibility_id = & (Join-Path $repo 'tools/generate_game_compat.ps1') -IdentityOnly
        files = @($files | ForEach-Object {
            $path = Join-Path $fixture $_
            [ordered]@{ path = $_; bytes = (Get-Item $path).Length; sha256 = (Get-FileHash $path -Algorithm SHA256).Hash.ToLowerInvariant() }
        })
    }
    function Save-Manifest { [IO.File]::WriteAllText((Join-Path $fixture 'runtime_package.json'), ($manifest | ConvertTo-Json -Depth 6)) }
    Save-Manifest
    & $builder -PackageDirectory $fixture -Check -ExpectedSourceRevision $revision | Out-Null
    $script:wwManifestChecks++
    [IO.File]::WriteAllText((Join-Path $fixture $files[0]), 'modified')
    Reject { & $builder -PackageDirectory $fixture -Check -ExpectedSourceRevision $revision } 'same-length corruption'
    [IO.File]::WriteAllText((Join-Path $fixture $files[0]), 'original')
    Reject { & $builder -PackageDirectory $fixture -Check -ExpectedSourceRevision ('2' * 40) } 'another source revision'
    $manifest.source_dirty = $true; Save-Manifest
    Reject { & $builder -PackageDirectory $fixture -Check -ExpectedSourceRevision $revision } 'dirty release provenance'
    $manifest.source_dirty = $false
    $compatibilityId = $manifest.compatibility_id
    $manifest.compatibility_id = 'stale'; Save-Manifest
    Reject { & $builder -PackageDirectory $fixture -Check } 'a stale compatibility ID'
    $manifest.compatibility_id = $compatibilityId
    $manifest.files[1].path = $files[0]; Save-Manifest
    Reject { & $builder -PackageDirectory $fixture -Check } 'duplicate digest entries'
    $manifest.files[1].path = $files[1]
    $manifest.schema = 2; Save-Manifest
    Reject { & $builder -PackageDirectory $fixture -Check } 'a legacy runtime without compatibility provenance'
    Write-Output ("PASS: {0} runtime manifest checks; no runtime artifact was executed." -f $script:wwManifestChecks)
} finally {
    $fixtureFull = [IO.Path]::GetFullPath($fixture)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not $fixtureFull.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Manifest fixture cleanup escaped the temporary directory.' }
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}
