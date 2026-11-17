namespace WWPoser.Services;

public static class BundleValidation
{
    public static void Validate(string root) => InspectBundle(root, writeHashes: false);
    internal static void WriteManifest(string root) => InspectBundle(root, writeHashes: true);

    private static void InspectBundle(string root, bool writeHashes)
    {
        _ = Contract.A(new CharacterRegistry(root).Index(), "profiles");
        _ = OwnedCharacterCatalog.Load(root);
        ValidateManifest(root, writeHashes);
    }

    internal static void ValidateManifest(string root, bool writeHashes = false)
    {
        root = Path.GetFullPath(root);
        var manifestPath = Path.Combine(root, "manifest.json");
        // Source checkouts have no bundle manifest. Portable packages do.
        if (!File.Exists(manifestPath))
        {
            Contract.Require(!writeHashes, "打包文件清单缺失。");
            return;
        }
        var manifest = JsonData.ReadObject(manifestPath);
        Contract.Require(Contract.I(manifest, "schema") == 4, "包清单版本不受支持。");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in Contract.A(manifest, "files"))
        {
            var relative = Contract.S(row!, "path").Replace('\\', '/');
            Contract.Require(!Path.IsPathRooted(relative) && !relative.Contains(':') &&
                relative.Split('/').All(part => part.Length > 0 && part is not "." and not "..") &&
                seen.Add(relative), "包清单路径无效或重复：" + relative);
            var file = new FileInfo(Path.Combine(root, relative));
            Contract.Require(file.Exists, "包文件缺失：" + relative);
            if (writeHashes)
            {
                Contract.Require(!row!.AsObject().ContainsKey("sha256"), "包文件已有 SHA-256：" + relative);
                row["bytes"] = file.Length;
                row["sha256"] = JsonData.Digest(file.FullName);
                continue;
            }
            Contract.Require(file.Length == Contract.I(row!, "bytes"), "包文件大小不符：" + relative);
            var digest = Contract.S(row!, "sha256");
            Contract.Require(digest.Length == 64 && digest.All(Uri.IsHexDigit), "包清单 SHA-256 无效：" + relative);
            Contract.Require(string.Equals(JsonData.Digest(file.FullName), digest, StringComparison.OrdinalIgnoreCase),
                "包文件 SHA-256 不符：" + relative);
        }
        Contract.Require(seen.Count > 0, "包清单没有文件。");
        if (writeHashes) JsonData.Write(manifestPath, manifest);
    }
}
