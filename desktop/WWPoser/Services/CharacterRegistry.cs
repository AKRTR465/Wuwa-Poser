namespace WWPoser.Services;
public sealed class CharacterRegistry(string repoRoot)
{
    private readonly string root = Path.Combine(Path.GetFullPath(repoRoot), "data", "characters");
    public JsonObject Index() => JsonData.ReadObject(Path.Combine(root, "index.json"));

    public JsonObject GetProfile(string id)
    {
        var row = Contract.A(Index(), "profiles").FirstOrDefault(x => Contract.S(x!, "profile_id") == id) ?? throw new InvalidDataException("未知角色：" + id);
        var path = Path.Combine(root, Contract.S(row, "profile_path"));
        var profile = JsonData.ReadObject(path);
        profile["_profile_path"] = path;
        return profile;
    }

    public JsonObject Resolve(string mesh, string skeleton)
    {
        var row = Contract.A(Index(), "profiles").FirstOrDefault(x => Contract.A(x!, "mesh_asset_names").Any(a => a!.GetValue<string>() == mesh) && Contract.S(x!, "skeleton_asset_name") == skeleton) ?? throw new InvalidDataException("网格与骨架未登记：" + mesh);
        return GetProfile(Contract.S(row, "profile_id"));
    }

    public JsonObject LoadProfile(string idOrPath) => GetProfile(File.Exists(idOrPath) ? Contract.S(JsonData.ReadObject(idOrPath), "profile_id") : idOrPath);
    public string ProfilePath(JsonObject profile) => Contract.S(profile, "_profile_path");
    public string SkeletonPath(JsonObject profile) => Path.Combine(root, Contract.S(profile, "skeleton_path"));
    public JsonArray MorphNames(JsonObject profile, string mesh) => (JsonArray)(mesh == Contract.S(profile, "model_id") ? Contract.A(profile, "morph_names") : Contract.O(profile, "mesh_morph_names")[mesh]!.AsArray()).DeepClone();
}
