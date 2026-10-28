[CmdletBinding()]
param([string]$DotNetExecutable = 'dotnet', [switch]$SkipCompilerBuild, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $IsWindows) { throw 'The full offline suite requires Windows; use PortableServiceChecks and the pure C++ tests on other systems.' }
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$buildInputs = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'player_build_inputs.psd1')
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo 'test_outputs/offline' }
& (Join-Path $PSScriptRoot 'generate_game_compat.ps1') -Check
& (Join-Path $PSScriptRoot 'generate_playback_control.ps1') -Check
& (Join-Path $PSScriptRoot 'tests/game_compat_generation_checks.ps1')
[IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
function Run-Native([string]$Relative) {
    & (Join-Path $repo $Relative)
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) { throw ('Offline native check failed: ' + $Relative + ' (exit code: ' + $exitCode + ')') }
}
function Run-Managed([string]$Project, [string[]]$CheckArguments = @()) {
    & $DotNetExecutable run --project (Join-Path $repo $Project) -c Release -- @CheckArguments
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) { throw ('Offline managed check failed: ' + $Project + ' (exit code: ' + $exitCode + ')') }
}

# These compilers only process synthetic motion data; they do not load the game.
if (-not $SkipCompilerBuild) {
    foreach ($tool in $buildInputs.Compilers) { Run-Native $tool.Build }
}
# Compile the installer and both observer DLLs whose cancellation protocol is
# exercised by the service checks. Building them does not load a game process.
foreach ($tool in $buildInputs.Observers) { Run-Native $tool.Build }
foreach ($script in @(
    'tools/tests/run_unified_transport_tests.bat',
    'tools/tests/run_owned_recovery_tests.bat',
    'tools/tests/run_local_clip_tests.bat',
    'tools/tests/run_foot_calibration_tests.bat',
    'tools/tests/run_hand_mapping_tests.bat',
    'tools/tests/run_camera_diagnostics_tests.bat',
    'tools/tests/run_unified_player_features_tests.bat',
    'tools/tests/run_dynamic_actor_tests.bat',
    'tools/tests/run_session_retirement_tests.bat',
    'tools/tests/run_game_compat_resolution_tests.bat',
    'tools/tests/run_native_function_witness_tests.bat',
    'tools/tests/run_game_object_anchors_tests.bat'
)) { Run-Native $script }
Run-Managed 'tools/tests/ManagedActorBorrowing/ManagedActorBorrowing.csproj'
# The WPF projects share their referenced desktop output and must run serially.
Run-Managed 'tools/tests/UnifiedServiceChecks/ServiceChecks.csproj' @('--prepare-actors', $repo)
Run-Managed 'tools/tests/UnifiedUiChecks/UnifiedUiChecks.csproj' @($repo, (Join-Path $OutputDirectory 'ui'))
& (Join-Path $PSScriptRoot 'validate_owned_character_catalog.ps1') -Root $repo -ReportPath (Join-Path $OutputDirectory 'catalog.json')
& (Join-Path $PSScriptRoot 'validate_character_forms.ps1') -Root $repo -ReportPath (Join-Path $OutputDirectory 'forms.json')
& (Join-Path $PSScriptRoot 'tests/runtime_manifest_checks.ps1')
Write-Output 'PASS: offline regression suite. No game process or real window capture was used.'
