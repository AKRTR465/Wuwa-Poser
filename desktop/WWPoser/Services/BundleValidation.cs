namespace WWPoser.Services;

public static class BundleValidation
{
    public static void Validate(string root)
    {
        var registry = new CharacterRegistry(root);
        var profiles = Contract.A(registry.Index(), "profiles");
        var catalog = OwnedCharacterCatalog.Load(root);
        var manifestPath = Path.Combine(root, "manifest.json");
        if (File.Exists(manifestPath))
            foreach (var row in Contract.A(JsonData.ReadObject(manifestPath), "files"))
                Contract.Require(File.Exists(Path.Combine(root, Contract.S(row!, "path"))), "包文件缺失：" + Contract.S(row!, "path"));
        JsonData.Write(Path.Combine(root, "bundle_validation.json"), new JsonObject { ["state"] = "validated", ["profiles"] = profiles.Count,
            ["character_configurations"] = catalog.CharacterConfigurationCount, ["character_skin_selections"] = catalog.Characters.Count, ["default_mmd_backend"] = "game_native" });
    }
}
