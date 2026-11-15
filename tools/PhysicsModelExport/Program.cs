using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Options;

if (args.Length != 4)
{
    Console.Error.WriteLine("Usage: PhysicsModelExport <Poser root> <local FModel CLI config> <local Oodle DLL> <model cache directory>");
    return 2;
}

Console.OutputEncoding = Encoding.UTF8;
var root = Path.GetFullPath(args[0]);
var config = JsonSerializer.Deserialize<ExportConfig>(File.ReadAllText(args[1]), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
var destination = Path.GetFullPath(args[3]);
var profiles = ReadSelections(root);
Directory.CreateDirectory(destination);
var temporary = Directory.CreateTempSubdirectory("wwposer-mesh-");
try
{
    OodleHelper.Initialize(Path.GetFullPath(args[2]));
    var paks = new DirectoryInfo(config.PaksDir);
    var patches = InstalledPatches(paks);
    using var provider = new DefaultFileProvider(paks, patches, SearchOption.AllDirectories,
        new VersionContainer(Enum.Parse<EGame>(config.UeVersion)), StringComparer.OrdinalIgnoreCase);
    provider.Initialize();
    provider.SubmitKey(new FGuid(), string.IsNullOrWhiteSpace(config.AesKey) ? new FAesKey(new byte[32]) : new FAesKey(config.AesKey.Trim()));
    if (!string.IsNullOrWhiteSpace(config.MappingsFile)) provider.MappingsContainer = new FileUsmapTypeMappingsProvider(config.MappingsFile);
    provider.PostMount();
    provider.LoadVirtualPaths();

    var options = new ExportOptions(meshFormat: EMeshFormat.Gltf2, exportMaterials: false, exportMorphTargets: false);
    var meshes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var assetPath in profiles.SelectMany(profile => profile.Assets).Distinct(StringComparer.OrdinalIgnoreCase))
    {
        try
        {
            var objectName = assetPath[(assetPath.LastIndexOf('.') + 1)..];
            var packagePath = "Client/Content/" + assetPath[6..assetPath.LastIndexOf('.')];
            var mesh = provider.LoadPackage(packagePath).GetExports().OfType<USkeletalMesh>()
                .Single(export => string.Equals(export.Name, objectName, StringComparison.OrdinalIgnoreCase));
            var results = await new ExportSession().Add(mesh).RunAsync(temporary.FullName, options);
            var output = results.Where(result => result.Success).SelectMany(result => result.DiskFilePaths ?? [])
                .FirstOrDefault(path => Path.GetExtension(path).Equals(".glb", StringComparison.OrdinalIgnoreCase));
            if (output is null) throw new InvalidDataException("No GLB was written: " + results.FirstOrDefault()?.Error);
            var fileName = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(assetPath.ToLowerInvariant())))[..24] + ".glb";
            File.Move(output, Path.Combine(destination, fileName), overwrite: true);
            meshes.Add(assetPath, fileName);
            Console.WriteLine($"[{meshes.Count}] {objectName}");
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"{assetPath}: {error.GetType().Name}: {error.Message}");
        }
    }

    var selections = profiles.Where(profile => profile.Assets.All(meshes.ContainsKey)).ToDictionary(profile => profile.CharacterId,
        profile => new { profile_id = profile.ProfileId, meshes = profile.Assets.Select(asset => meshes[asset]).ToArray() });
    var manifest = Path.Combine(destination, "index.json");
    var pending = manifest + "." + Guid.NewGuid().ToString("N");
    File.WriteAllText(pending, JsonSerializer.Serialize(new { schema = 1, selections }, new JsonSerializerOptions { WriteIndented = true }));
    File.Move(pending, manifest, overwrite: true);
    Console.WriteLine($"{selections.Count}/{profiles.Count} selections, {meshes.Count} unique meshes.");
    return selections.Count == profiles.Count ? 0 : 1;
}
finally { temporary.Delete(recursive: true); }

static DirectoryInfo[] InstalledPatches(DirectoryInfo paks)
{
    if (paks.Parent is not { Name: "Content", Parent: { } client }) return [];
    var resources = Path.Combine(client.FullName, "Saved", "Resources", "3.8.0");
    return new[] { Path.Combine(resources, "Resource"), Path.Combine(resources, "Resource_UHD"), Path.Combine(resources, "Resource_HD") }
        .Where(Directory.Exists).Select(path => new DirectoryInfo(path)).ToArray();
}

static List<Selection> ReadSelections(string root)
{
    var directory = Path.Combine(root, "data", "characters");
    using var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "owned_character_catalog.json")));
    using var forms = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "character_forms.json")));
    using var components = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "physics_model_parts.json")));
    var childMeshes = components.RootElement.GetProperty("selections");
    var selections = catalog.RootElement.GetProperty("characters").EnumerateArray()
        .Select(row => ReadSelection(row, [Required(row, "model_asset_path"), .. ReadComponents(row)])).ToList();
    foreach (var row in forms.RootElement.GetProperty("forms").EnumerateArray())
    {
        var load = row.GetProperty("load");
        selections.Add(ReadSelection(row, [Required(load, "main_mesh_path"), ..load.GetProperty("child_mesh_paths").EnumerateArray().Select(asset => asset.GetString()!)]));
    }
    return selections;

    IEnumerable<string> ReadComponents(JsonElement row) => childMeshes.TryGetProperty(Required(row, "character_id"), out var assets)
        ? assets.EnumerateArray().Select(asset => asset.GetString()!) : [];
}

static Selection ReadSelection(JsonElement row, string[] assets) => new(Required(row, "character_id"), Required(row, "profile_id"), assets);
static string Required(JsonElement row, string property) => row.GetProperty(property).GetString() ?? throw new InvalidDataException(property);

internal sealed record Selection(string CharacterId, string ProfileId, string[] Assets);
internal sealed record ExportConfig(string PaksDir, string UeVersion, string? MappingsFile, string? AesKey);
