namespace WWPoser.Services;

public static class BundleValidation
{
    public static void Validate(string root)
    {
        var registry = new CharacterRegistry(root);
        var profiles = Contract.A(registry.Index(), "profiles");
        var catalog = OwnedCharacterCatalog.Load(root);
        ValidateManifest(root);
        JsonData.Write(Path.Combine(root, "bundle_validation.json"), new JsonObject { ["state"] = "validated", ["profiles"] = profiles.Count,
            ["character_configurations"] = catalog.CharacterConfigurationCount, ["character_skin_selections"] = catalog.Characters.Count(character => character.Form is null),
            ["additional_forms"] = catalog.Characters.Count(character => character.Form is not null), ["character_form_selections"] = catalog.Characters.Count, ["default_mmd_backend"] = "game_native" });
    }

    internal static void ValidateManifest(string root)
    {
        root = Path.GetFullPath(root);
        var manifestPath = Path.Combine(root, "manifest.json");
        // Source checkouts have no bundle manifest. Portable packages do.
        if (!File.Exists(manifestPath)) return;
        var manifest = JsonData.ReadObject(manifestPath);
        var requireHashes = Contract.I(manifest, "schema") >= 4;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in Contract.A(manifest, "files"))
        {
            var relative = Contract.S(row!, "path").Replace('\\', '/');
            Contract.Require(!Path.IsPathRooted(relative) && !relative.Contains(':') &&
                relative.Split('/').All(part => part.Length > 0 && part is not "." and not "..") &&
                seen.Add(relative), "包清单路径无效或重复：" + relative);
            var file = new FileInfo(Path.Combine(root, relative));
            Contract.Require(file.Exists, "包文件缺失：" + relative);
            Contract.Require(file.Length == Contract.I(row!, "bytes"), "包文件大小不符：" + relative);
            var digest = row!["sha256"]?.GetValue<string>();
            Contract.Require(!requireHashes || digest is not null, "包清单缺少 SHA-256：" + relative);
            if (digest is not null)
            {
                Contract.Require(digest.Length == 64 && digest.All(Uri.IsHexDigit), "包清单 SHA-256 无效：" + relative);
                Contract.Require(string.Equals(JsonData.Digest(file.FullName), digest, StringComparison.OrdinalIgnoreCase),
                    "包文件 SHA-256 不符：" + relative);
            }
        }
        Contract.Require(seen.Count > 0, "包清单没有文件。");
    }
}
