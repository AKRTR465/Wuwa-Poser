@{
    # Native physics uses the observers in ordinary playback; the compilers
    # prepare motion, expression and camera tracks.
    NativeBuilds = @('runtime/game_integration/build_observers.bat', 'tools/build_compilers.bat')
    NativeOutputs = @(
        'runtime/game_integration/observer_installer.exe', 'runtime/game_integration/native_identity_bootstrap.dll', 'runtime/game_integration/native_graph_discovery.dll',
        'tools/mmd_outputs/mmd_body_compile.exe', 'tools/mmd_outputs/mmd_face_compile.exe', 'tools/mmd_outputs/mmd_camera_compile.exe'
    )
    OwnedRuntimeFiles = @(
        'scene_playback_observer.dll', 'scene_playback_installer.exe', 'WwPoser.ScenePlayback.ManagedBridge.dll'
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
