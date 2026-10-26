[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('wwposer-manifest-' + [Guid]::NewGuid().ToString('N'))
$builder = Join-Path $repo 'tools/build_owned_mmd.ps1'
$revision = '1111111111111111111111111111111111111111'
$files = @('ownedmmd_lite_observer.dll', 'ownedmmd_lite_installer.exe', 'owned_mmd_layout.json', 'WwPoser.ColdLoad.ManagedBridge.OwnedMmdLite.dll')
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
        schema = 2; purpose = 'single_owned_character_native_mmd'
        source_revision = $revision; source_dirty = $false; files = $files
        compatibility_profile = (Get-Content -LiteralPath (Join-Path $repo 'data/game_compat/ww_runtime.json') -Raw | ConvertFrom-Json).profile_id
        compatibility_source_sha256 = (Get-FileHash -LiteralPath (Join-Path $repo 'data/game_compat/ww_runtime.json') -Algorithm SHA256).Hash.ToLowerInvariant()
        artifacts = @($files | ForEach-Object {
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
    $compatibilityDigest = $manifest.compatibility_source_sha256
    $manifest.compatibility_source_sha256 = '0' * 64; Save-Manifest
    Reject { & $builder -PackageDirectory $fixture -Check } 'a stale compatibility profile'
    $manifest.compatibility_source_sha256 = $compatibilityDigest
    $manifest.artifacts[1].path = $files[0]; Save-Manifest
    Reject { & $builder -PackageDirectory $fixture -Check } 'duplicate digest entries'
    $manifest.schema = 1; Save-Manifest
    Reject { & $builder -PackageDirectory $fixture -Check } 'a legacy runtime without compatibility provenance'
    Reject { & $builder -PackageDirectory $fixture -Check -ExpectedSourceRevision $revision } 'legacy unsigned release provenance'
    Write-Output ("PASS: {0} runtime manifest checks; no runtime artifact was executed." -f $script:wwManifestChecks)
} finally {
    $fixtureFull = [IO.Path]::GetFullPath($fixture)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not $fixtureFull.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Manifest fixture cleanup escaped the temporary directory.' }
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}
