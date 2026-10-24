# Dynamic actor MMD runtime

This host now manages a dynamic mixture of borrowed current-team actors and newly created actors. Requests use one actor list, timeline, camera and native-physics hub. The installed catalog provides 64 role configurations and 80 role/skin selections. The historical build locations `build/owned_mmd` / `runtime/owned_mmd_lite` and package identifier `single_owned_character_native_mmd` remain for packaging compatibility; they no longer imply one actor or created-only playback. New scene preparations use `mixed_actor_mmd`.

## Build

Run these commands from the repository root in PowerShell 7. The build uses the existing Visual Studio 2022 Build Tools C++ environment and Roslyn `csc.exe`; Python is not required for building or running this backend.

```powershell
./tools/build_owned_mmd.ps1
./tools/build_owned_mmd.ps1 -Check
```

Place the six DLLs named in `managed/references.json` in `local/owned_mmd_references`, or select an existing local reference directory explicitly:

```powershell
./tools/build_owned_mmd.ps1 -ReferenceDirectory 'D:\owned-mmd-references'
./tools/build_owned_mmd.ps1 -ReferenceDirectory 'D:\owned-mmd-references' -PackageDirectory 'D:\owned-mmd-runtime'
./tools/build_owned_mmd.ps1 -PackageDirectory 'D:\owned-mmd-runtime' -Check
```

`WWPOSER_MANAGED_REFERENCE_DIR` can supply the reference directory when `-ReferenceDirectory` is omitted. `-Compiler` selects a different Roslyn `csc.exe`; the C++ compiler setup uses the repository's `tools/msvc_env.bat` and its `WW_VCVARS64` override. Reference assemblies remain local compile inputs and are never included in the runtime package.

The package contains four files at its root plus `runtime_package.json`:

- `ownedmmd_lite_observer.dll`
- `ownedmmd_lite_installer.exe`
- `WwPoser.ColdLoad.ManagedBridge.OwnedMmdLite.dll`
- `owned_mmd_layout.json`

Compiler intermediate files go to the ignored `build/owned_mmd_compile` directory. `-Check` verifies the existing manifest and four runtime files without compiling or loading them.

To rebuild and stage the self-contained desktop player, including the flat owned runtime package:

```powershell
./tools/build_player.ps1 -Rebuild -ReferenceDirectory 'D:\owned-mmd-references'
```

Use `-OwnedMmdPackageDirectory` on `build_player.ps1` to select another runtime package directory. Without `-Rebuild`, the player build checks and copies existing runtime artifacts. The portable player includes its .NET runtime and does not require Python.

## Current behavior

The managed provider reads the current SceneTeam and borrows each matching real actor at most once. All remaining slots create independent instances. Each actor exports a fresh graph by slot and receives its own body, expression, placement and physics state. A single host starts playback after every required native input is ready. Stable actor storage and lookup indices are frozen before callbacks; there is no fixed 1/3/16 actor array.

Pause holds the common timeline while keeping body input. Non-looping playback holds the last frame until Stop or the selected time limit; zero is unlimited. The existing PCM hook also maintains borrowed actors' stage state. Cleanup restores body/face/physics before disabling that maintenance and returning borrowed actors to their original state; only actors actually created by this session are removed from the UI actor manager. Incomplete cleanup retains the dispatch route for retries.

Add any number of actors in the unified desktop; each row selects a role and skin. A legacy file-only CLI can still prepare a single catalog entry for the same host:

```powershell
WWPoser.exe --prepare-owned-mmd SOURCE.vmd NEW_OUTPUT_DIRECTORY 120000 --character-id role1206-skin81001206
WWPoser.exe --run-owned-mmd NEW_OUTPUT_DIRECTORY --result RESULT.json
```

That example selects Brant's default skin. Changing the character or source VMD requires preparing again; speed, loop and duration are written into the prepared `owned_mmd_config.json` at start.

Native body input is applied at the observed pre-physics converter while secondary bones preserve upstream local transforms. Curve groups use `force_when_zero`; Float/Bool groups use `observe_natural`. Direct pose, expression and scene camera reuse `probes/v20_recon/unified_player_features.h`.

Sessions run sequentially in the same game process, each with its own carrier copy. Mono retains the loaded managed helper, so BindSession selects the next session directory. Upgrading from an already-loaded older helper requires one game restart to load the new helper, including its fresh player/controller exports for the scene camera. This is distinct from requiring a restart between sessions of the same version.

See [managed lifecycle](managed/README.md) and the [build and offline checks guide](../../docs/BUILDING.md). Underlying engine capacity and actual multi-actor visual behavior remain live-validation items.
