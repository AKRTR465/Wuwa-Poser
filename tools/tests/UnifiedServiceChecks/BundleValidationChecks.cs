using System.IO;
using System.Text.Json.Nodes;
using WWPoser.Services;

internal static class BundleValidationChecks
{
    internal static int Run(string fixture)
    {
        var root = Path.Combine(fixture, "bundle-integrity");
        Directory.CreateDirectory(root);
        var asset = Path.Combine(root, "payload.dat");
        File.WriteAllText(asset, "original");
        var manifest = new JsonObject
        {
            ["schema"] = 4,
            ["files"] = new JsonArray(new JsonObject
            {
                ["path"] = "payload.dat", ["bytes"] = new FileInfo(asset).Length,
                ["sha256"] = JsonData.Digest(asset)
            })
        };
        var manifestPath = Path.Combine(root, "manifest.json");
        void Save() => JsonData.Write(manifestPath, manifest);
        int checks = 0;
        void Rejected(string label)
        {
            Save();
            try { BundleValidation.ValidateManifest(root); }
            catch (InvalidDataException) { checks++; return; }
            throw new Exception("Bundle validation accepted " + label);
        }

        Save();
        BundleValidation.ValidateManifest(root);
        checks++;
        File.WriteAllText(asset, "modified");
        Rejected("same-length corruption");
        File.WriteAllText(asset, "original");
        var entry = manifest["files"]![0]!;
        entry["bytes"] = 1;
        Rejected("wrong size");
        entry["bytes"] = new FileInfo(asset).Length;
        entry.AsObject().Remove("sha256");
        Rejected("missing schema-4 digest");
        entry["sha256"] = JsonData.Digest(asset);
        entry["path"] = "../payload.dat";
        Rejected("parent traversal");
        entry["path"] = "C:/payload.dat";
        Rejected("absolute path");
        entry["path"] = "payload.dat";
        manifest["files"]!.AsArray().Add(entry.DeepClone());
        Rejected("duplicate paths");
        manifest["files"]!.AsArray().RemoveAt(1);
        File.Delete(asset);
        Rejected("missing payload");
        return checks;
    }
}
