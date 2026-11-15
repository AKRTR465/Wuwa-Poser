namespace WWPoser.Services;

internal static class PartGravityCatalog
{
    public static JsonArray Build(string root, OwnedCharacterSelection character, PhysicsPartProfile? profile = null)
    {
        profile ??= new PhysicsPartCatalog(root).Read(character);
        var bones = profile.Bones;
        var indices = bones.Select((bone, index) => (bone.Name, index))
            .ToDictionary(entry => entry.Name, entry => entry.index, StringComparer.OrdinalIgnoreCase);
        var result = ReadConfigured(root, character.ProfileId);
        var occupied = new HashSet<int>();
        var anchors = new HashSet<int>();
        foreach (var part in result.OfType<JsonObject>())
        {
            occupied.UnionWith(Members(part, bones));
            foreach (var name in Roots(part)) anchors.Add(bones[indices[name]].Parent);
        }

        foreach (var rootBone in profile.Groups.SelectMany(group => group.Chains).Select(chain => indices.GetValueOrDefault(chain.Root, -1))
                     .Where(index => index > 0 && bones[index].Parent >= 0).Distinct().Order())
        {
            var rootName = bones[rootBone].Name;
            var anchor = bones[rootBone].Parent;
            if (occupied.Contains(rootBone) || occupied.Contains(anchor)) continue;
            var part = new JsonObject
            {
                ["id"] = 100 + rootBone,
                ["name"] = rootName,
                ["root"] = rootName,
                ["anchor"] = bones[anchor].Name,
                ["continuation"] = new JsonArray(),
                ["capsules"] = new JsonArray(),
                ["from_chain"] = true
            };
            var members = Members(part, bones);
            if (members.Overlaps(occupied) || members.Overlaps(anchors) ||
                members.Any(index => bones[index].Name.StartsWith("Bip001", StringComparison.OrdinalIgnoreCase))) continue;
            result.Add(part);
            occupied.UnionWith(members);
            anchors.Add(anchor);
        }
        return result;
    }

    internal static HashSet<int> Members(JsonObject part, IReadOnlyList<PhysicsBone> bones)
    {
        var roots = Roots(part).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var members = new HashSet<int>();
        for (var i = 0; i < bones.Count; i++)
            if (roots.Contains(bones[i].Name) || members.Contains(bones[i].Parent)) members.Add(i);
        return members;
    }

    private static IEnumerable<string> Roots(JsonObject part) =>
        (part["transition_roots"] as JsonArray ?? []).Select(node => node!.GetValue<string>()).Prepend(Contract.S(part, "root"));

    private static JsonArray ReadConfigured(string root, string profile)
    {
        var path = Path.Combine(root, "data", "characters", "part_gravity.json");
        if (!File.Exists(path)) return [];
        var catalog = JsonData.ReadObject(path);
        Contract.Require(Contract.I(catalog, "schema") == 1, "部件重力目录版本不匹配。");
        return (Contract.O(catalog, "profiles")[profile]?.DeepClone() as JsonArray) ?? [];
    }
}
