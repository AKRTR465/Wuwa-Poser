# Current native graph descriptors

`index.json` and `runtime/` contain the current registered character, skeleton, source and group signatures. All descriptors require fresh live discovery; they do not authorize writes by themselves.

Runtime matching uses exact node, root, curve and instance evidence. Static provenance labels describe the saved source and never replace the current observation.

The Python API is `load_registry()`, `get_profile(profile_id)` and `validate()`. Offline source reconstruction, generators and test entrypoints have been removed. Formal builds validate the retained runtime descriptors and their hashes.
