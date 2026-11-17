[CmdletBinding()]
param([string]$DotNetExecutable = 'dotnet', [switch]$SkipNativeBuild, [switch]$SkipCatalogChecks, [string]$OutputDirectory)
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
function Run-Native([string]$Relative, [string[]]$NativeArguments = @()) {
    & (Join-Path $repo $Relative) @NativeArguments
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) { throw ('Offline native check failed: ' + $Relative + ' (exit code: ' + $exitCode + ')') }
}
function Run-Managed([string]$Project, [string[]]$CheckArguments = @()) {
    & $DotNetExecutable run --project (Join-Path $repo $Project) -c Release -- @CheckArguments
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) { throw ('Offline managed check failed: ' + $Project + ' (exit code: ' + $exitCode + ')') }
}

# The compilers process only synthetic motion data, and the service checks
# exercise the observers' cancellation protocol. Building loads no game process.
if (-not $SkipNativeBuild) {
    foreach ($script in $buildInputs.NativeBuilds) { Run-Native $script }
}
Run-Native 'tools/tests/run_native_tests.bat' @('unified_transport', 'owned_recovery', 'local_clip', 'foot_calibration', 'hand_mapping',
    'camera_diagnostics', 'unified_player_features', 'vmd_name', 'dynamic_actor', 'session_retirement', 'game_compat_resolution',
    'native_function_witness', 'game_object_anchors')
Run-Managed 'tools/tests/ManagedActorBorrowing/ManagedActorBorrowing.csproj'
# The WPF projects share their referenced desktop output and must run serially.
Run-Managed 'tools/tests/UnifiedServiceChecks/ServiceChecks.csproj' @('--prepare-actors', $repo)
Run-Managed 'tools/tests/UnifiedUiChecks/UnifiedUiChecks.csproj' @($repo, (Join-Path $OutputDirectory 'ui'))
if (-not $SkipCatalogChecks) {
    & (Join-Path $PSScriptRoot 'validate_owned_character_catalog.ps1') -Root $repo -ReportPath (Join-Path $OutputDirectory 'catalog.json')
    & (Join-Path $PSScriptRoot 'validate_character_forms.ps1') -Root $repo -ReportPath (Join-Path $OutputDirectory 'forms.json')
}
& (Join-Path $PSScriptRoot 'tests/runtime_manifest_checks.ps1')
Write-Output 'PASS: offline regression suite. No game process or real window capture was used.'
