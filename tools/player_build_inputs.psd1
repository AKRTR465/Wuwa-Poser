@{
    # Native physics uses these observers in ordinary playback.
    Observers = @(
        @{ Build = 'probes/v20_recon/build_hook_install.bat'; Output = 'probes/v20_recon/hook_install_v20.exe' }
        @{ Build = 'probes/v20_recon/build_native_identity_bootstrap.bat'; Output = 'probes/v20_recon/native_identity_bootstrap.dll' }
        @{ Build = 'probes/v20_recon/build_native_graph_discovery.bat'; Output = 'probes/v20_recon/native_graph_discovery.dll' }
    )
    Compilers = @(
        @{ Build = 'tools/mmd_build_vmd_cli.bat'; Output = 'tools/mmd_outputs/mmd_vmd_cli.exe' }
        @{ Build = 'tools/mmd_build_face.bat'; Output = 'tools/mmd_outputs/mmd_face_compile.exe' }
        @{ Build = 'tools/mmd_build_camera.bat'; Output = 'tools/mmd_outputs/mmd_camera_compile.exe' }
    )
    OwnedRuntimeFiles = @(
        'ownedmmd_lite_observer.dll', 'ownedmmd_lite_installer.exe',
        'owned_mmd_layout.json', 'WwPoser.ColdLoad.ManagedBridge.OwnedMmdLite.dll'
    )
    Files = @(
        'tools/recording_bin/ffmpeg.exe', 'tools/recording_bin/ffprobe.exe',
        'tools/recording_bin/LICENSE.txt', 'tools/recording_bin/README.txt', 'tools/recording_bin/source.json',
        'data/bone_chains.json', 'data/camera_runtime_contract.json', 'data/native_identity_registration_contract.json',
        'data/native_graphs/index.json', 'data/native_graphs/owned_ui_source_certificate.json',
        'data/dependencies/ffmpeg.json', 'data/dependencies/ffmpeg.README.txt',
        'tools/MMD_PLAYER.md', 'tools/MMD_SQUAD.md', 'playback/CONTROL_COMMANDS.md',
        'README.md', 'CHANGELOG.md', 'THIRD_PARTY_NOTICES.md',
        'docs/BUILDING.md', 'docs/CHARACTER_FORMS.md', 'docs/GAME_COMPATIBILITY.md'
    )
    Directories = @('licenses', 'data/characters', 'data/runtime_contracts', 'data/game_compat', 'data/native_graphs/runtime')
}
