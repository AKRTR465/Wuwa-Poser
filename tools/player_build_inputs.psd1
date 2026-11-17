@{
    # Native physics uses the observers in ordinary playback; the compilers
    # prepare motion, expression and camera tracks.
    NativeBuilds = @('probes/v20_recon/build_observers.bat', 'tools/build_compilers.bat')
    NativeOutputs = @(
        'probes/v20_recon/hook_install_v20.exe', 'probes/v20_recon/native_identity_bootstrap.dll', 'probes/v20_recon/native_graph_discovery.dll',
        'tools/mmd_outputs/mmd_vmd_cli.exe', 'tools/mmd_outputs/mmd_face_compile.exe', 'tools/mmd_outputs/mmd_camera_compile.exe'
    )
    OwnedRuntimeFiles = @(
        'ownedmmd_lite_observer.dll', 'ownedmmd_lite_installer.exe', 'WwPoser.ColdLoad.ManagedBridge.OwnedMmdLite.dll'
    )
    Files = @(
        'tools/recording_bin/ffmpeg.exe', 'tools/recording_bin/ffprobe.exe',
        'tools/recording_bin/LICENSE.txt', 'tools/recording_bin/README.txt', 'tools/recording_bin/source.json',
        'data/bone_chains.json',
        'data/native_graphs/index.json', 'data/native_graphs/owned_ui_source_certificate.json',
        'data/dependencies/ffmpeg.json', 'data/dependencies/ffmpeg.README.txt',
        'tools/MMD_PLAYER.md', 'playback/CONTROL_COMMANDS.md',
        'README.md', 'CHANGELOG.md', 'THIRD_PARTY_NOTICES.md',
        'docs/BUILDING.md', 'docs/CHARACTER_FORMS.md', 'docs/GAME_COMPATIBILITY.md'
    )
    Directories = @('licenses', 'data/characters', 'data/runtime_contracts', 'data/game_compat', 'data/native_graphs/runtime')
}
