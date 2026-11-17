# Current native graph descriptors

`index.json` and `runtime/` contain the current registered character, skeleton, source and group signatures. All descriptors require fresh live discovery; they do not authorize writes by themselves.

Runtime matching uses exact node, root, curve and instance evidence. Static provenance labels describe the saved source and never replace the current observation.

In schema 2, each descriptor assigns local numeric IDs to its candidate sources. Candidate groups reference those IDs. Formal builds validate each descriptor against its entry in the index.
