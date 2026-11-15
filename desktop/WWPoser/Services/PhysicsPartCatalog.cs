using System.Numerics;

namespace WWPoser.Services;

internal sealed record PhysicsBone(string Name, int Parent, Vector3 Position);
internal sealed record PhysicsChain(string Root, string Part, IReadOnlyList<int> Bones);
internal sealed record PhysicsGroup(string Source, string Node, string? Curve, IReadOnlyList<PhysicsChain> Chains,
    double? Damping, double? Stiffness, double? LimitAngle);
internal sealed record PhysicsPartProfile(IReadOnlyList<PhysicsBone> Bones, IReadOnlyList<PhysicsGroup> Groups);

internal sealed class PhysicsPartCatalog(string repositoryRoot)
{
    private readonly NativeGraphService graphs = new(repositoryRoot);
    private readonly CharacterRegistry registry = new(repositoryRoot);
    private readonly Lazy<JsonArray> chainModels = new(() => Contract.A(
        JsonData.ReadObject(Path.Combine(repositoryRoot, "data", "bone_chains.json")), "characters"));

    public PhysicsPartProfile Read(OwnedCharacterSelection character)
    {
        var graph = graphs.RuntimeProfile(character.ProfileId);
        var profile = registry.GetProfile(character.ProfileId);
        var bones = ReadBones(registry.SkeletonPath(profile), profile);
        var indices = bones.Select((bone, index) => (bone.Name, index))
            .ToDictionary(bone => bone.Name, bone => bone.index, StringComparer.OrdinalIgnoreCase);
        var sources = Contract.A(graph, "candidate_sources").Where(row => row?["evidence_kind"]?.GetValue<string>() == "actual_nodes").ToArray();
        var skinSources = sources.Where(source => source!["ui_skin_ids"] is JsonArray ids &&
            ids.Any(id => id!.GetValue<int>() == character.SkinId)).ToArray();
        if (skinSources.Length > 0) sources = skinSources;

        var model = chainModels.Value.FirstOrDefault(row => Contract.S(row!, "model_id") == Contract.S(graph, "physics_model_id"));
        var exports = model?["kawaii_groups"] as JsonArray ?? [];
        var groups = new List<PhysicsGroup>();
        foreach (var source in sources)
        {
            var variant = Contract.S(source!, "variant");
            var assetGroups = exports.Where(asset => Contract.S(asset!, "variant") == variant)
                .SelectMany(asset => Contract.A(asset!, "groups")).ToArray();
            foreach (var group in Contract.A(source!, "group_signatures"))
            {
                var node = Contract.S(group!, "node_property");
                var roots = Contract.A(group!, "root_bones").Select(root => root!.GetValue<string>()).ToArray();
                var matches = assetGroups.Where(asset => Contract.S(asset!, "node") == node &&
                    roots.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(Contract.A(asset!, "root_bones").Select(root => root!.GetValue<string>()))).ToArray();
                var asset = matches.Length == 1 ? matches[0] : null;
                var chains = roots.Select(root => new PhysicsChain(root, Part(root), Descendants(root, bones, indices, asset))).ToArray();
                var settings = asset?["physics"];
                groups.Add(new PhysicsGroup(variant, node, group!["alpha_curve"]?.GetValue<string>(), chains,
                    settings?["Damping"]?.GetValue<double>(), settings?["Stiffness"]?.GetValue<double>(), settings?["LimitAngle"]?.GetValue<double>()));
            }
        }
        return new PhysicsPartProfile(bones, groups);
    }

    private static string Part(string root)
    {
        if (root.Contains("xiuzi", StringComparison.OrdinalIgnoreCase) || root.Contains("yixiu", StringComparison.OrdinalIgnoreCase) ||
            root.Contains("sleeve", StringComparison.OrdinalIgnoreCase)) return "袖子";
        if (root.Contains("hair", StringComparison.OrdinalIgnoreCase)) return "头发";
        if (root.Contains("skirt", StringComparison.OrdinalIgnoreCase)) return "裙摆";
        if (root.Contains("piao", StringComparison.OrdinalIgnoreCase)) return "飘带或衣摆";
        return "其他";
    }

    private static int[] Descendants(string root, IReadOnlyList<PhysicsBone> bones, Dictionary<string, int> indices, JsonNode? asset)
    {
        if (!indices.TryGetValue(root, out var rootIndex)) return [];
        var recorded = (asset?["chains"] as JsonArray ?? []).Where(chain =>
            string.Equals(Contract.S(chain!, "root"), root, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (recorded.Length > 0)
            return recorded.SelectMany(chain => Contract.A(chain!, "bones")).Select(bone => indices.GetValueOrDefault(bone!.GetValue<string>(), -1))
                .Where(index => index >= 0).Distinct().Order().ToArray();

        var included = new bool[bones.Count];
        included[rootIndex] = true;
        for (var i = rootIndex + 1; i < bones.Count; i++) included[i] = bones[i].Parent >= 0 && included[bones[i].Parent];
        return Enumerable.Range(rootIndex, bones.Count - rootIndex).Where(i => included[i]).ToArray();
    }

    private static PhysicsBone[] ReadBones(string path, JsonObject profile)
    {
        var exports = JsonNode.Parse(File.ReadAllText(path))!.AsArray();
        var skeleton = exports.Single(row => row?["ReferenceSkeleton"] is JsonObject)!["ReferenceSkeleton"]!;
        var info = skeleton["FinalRefBoneInfo"]!.AsArray();
        var poses = skeleton["FinalRefBonePose"]!.AsArray();
        var names = Contract.A(profile, "bone_names");
        var parents = Contract.A(profile, "bone_parents");
        // Runtime profiles may reorder bones relative to the source skeleton.
        var sourceIndices = info.Select((bone, index) => (Name: Contract.S(bone!, "Name"), index))
            .ToDictionary(bone => bone.Name, bone => bone.index, StringComparer.OrdinalIgnoreCase);
        var world = new Matrix4x4[names.Count];
        var output = new PhysicsBone[names.Count];
        for (var i = 0; i < output.Length; i++)
        {
            var name = names[i]!.GetValue<string>();
            var parent = parents[i]!.GetValue<int>();
            var pose = poses[sourceIndices[name]]!;
            var rotation = pose["Rotation"]!;
            var local = Matrix4x4.CreateScale(Vector(pose["Scale3D"]!)) *
                Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(new Quaternion(Component(rotation, "X"), Component(rotation, "Y"), Component(rotation, "Z"), Component(rotation, "W")))) *
                Matrix4x4.CreateTranslation(Vector(pose["Translation"]!));
            world[i] = parent < 0 ? local : local * world[parent];
            output[i] = new PhysicsBone(name, parent, world[i].Translation);
        }
        return output;
    }

    private static Vector3 Vector(JsonNode value) => new(Component(value, "X"), Component(value, "Y"), Component(value, "Z"));
    private static float Component(JsonNode value, string axis) => value[axis]!.GetValue<float>();
}
