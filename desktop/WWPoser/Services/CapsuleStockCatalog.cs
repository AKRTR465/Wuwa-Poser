namespace WWPoser.Services;

internal sealed class CapsuleStockCatalog(string repositoryRoot)
{
    private const string RelativePath = "data/characters/capsule_stock.json";
    private readonly Lazy<JsonObject?> catalog = new(() => Load(repositoryRoot));

    public IReadOnlyList<CapsuleDraftGroup> Read(OwnedCharacterSelection character, IReadOnlyList<PhysicsGroup> groups)
    {
        if (catalog.Value is not { } catalogData ||
            Contract.O(catalogData, "characters")[character.CharacterId] is not JsonObject entry ||
            !string.Equals(Contract.S(entry, "profile_id"), character.ProfileId, StringComparison.Ordinal))
            return [];

        var layout = Contract.O(Contract.O(catalogData, "layouts"), Contract.S(entry, "layout"));
        var variant = Contract.S(layout, "variant");
        var rows = Contract.A(layout, "groups");
        var stock = CapsuleAuthoring.ReadGroups(rows);
        var result = new List<CapsuleDraftGroup>();
        for (var i = 0; i < stock.Count; i++)
        {
            var node = Contract.S(rows[i]!, "node");
            var group = stock[i];
            var matchingGroups = groups.Where(candidate => candidate.Source == variant &&
                CapsuleAuthoring.Matches(group, candidate)).ToArray();
            if (matchingGroups.Length == 1 && matchingGroups[0].Node == node)
                result.Add(group with { StockBaseline = group.Shapes });
        }
        return result;
    }

    private static JsonObject? Load(string repositoryRoot)
    {
        var path = Path.Combine(repositoryRoot, RelativePath);
        if (!File.Exists(path)) return null;
        var data = JsonData.ReadObject(path);
        return Contract.I(data, "schema") == 1 &&
            Contract.S(data, "source_build_sha256") == GameCompat.SourceBuildSha256 ? data : null;
    }
}
