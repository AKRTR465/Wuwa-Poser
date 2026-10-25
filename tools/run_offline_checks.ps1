[CmdletBinding()]
param([string]$DotNetExecutable = 'dotnet', [switch]$SkipCompilerBuild, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $IsWindows) { throw 'The full offline suite requires Windows; use PortableServiceChecks and the pure C++ tests on other systems.' }
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo 'test_outputs/offline' }
[IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
function Run-Native([string]$Relative) {
    & (Join-Path $repo $Relative)
    if ($LASTEXITCODE -ne 0) { throw ('Offline native check failed: ' + $Relative) }
}
function Run-Managed([string]$Project, [string[]]$CheckArguments = @()) {
    & $DotNetExecutable run --project (Join-Path $repo $Project) -c Release -- @CheckArguments
    if ($LASTEXITCODE -ne 0) { throw ('Offline managed check failed: ' + $Project) }
}

# These compilers only process synthetic motion data; they do not load the game.
if (-not $SkipCompilerBuild) {
    foreach ($script in @('tools/mmd_build_vmd_cli.bat', 'tools/mmd_build_face.bat', 'tools/mmd_build_camera.bat')) { Run-Native $script }
}
foreach ($script in @(
    'tools/tests/run_unified_transport_tests.bat',
    'tools/tests/run_owned_recovery_tests.bat',
    'tools/tests/run_local_clip_tests.bat',
    'tools/tests/run_camera_diagnostics_tests.bat',
    'tools/tests/run_unified_player_features_tests.bat',
    'tools/tests/run_dynamic_actor_tests.bat',
    'tools/tests/run_session_retirement_tests.bat'
)) { Run-Native $script }
Run-Managed 'tools/tests/ManagedActorBorrowing/ManagedActorBorrowing.csproj'
# The WPF projects share their referenced desktop output and must run serially.
Run-Managed 'tools/tests/UnifiedServiceChecks/ServiceChecks.csproj' @('--prepare-actors', $repo)
Run-Managed 'tools/tests/UnifiedUiChecks/UnifiedUiChecks.csproj' @($repo, (Join-Path $OutputDirectory 'ui'))
& (Join-Path $PSScriptRoot 'validate_owned_character_catalog.ps1') -Root $repo -ReportPath (Join-Path $OutputDirectory 'catalog.json')
& (Join-Path $PSScriptRoot 'validate_character_forms.ps1') -Root $repo -ReportPath (Join-Path $OutputDirectory 'forms.json')
& (Join-Path $PSScriptRoot 'tests/runtime_manifest_checks.ps1')
Write-Output 'PASS: offline regression suite. No game process or real window capture was used.'
