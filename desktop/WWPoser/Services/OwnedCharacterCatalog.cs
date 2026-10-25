namespace WWPoser.Services;

public sealed record OwnedCharacterSelection(string CharacterId, string DisplayName, int RoleId, int SkinId, string ModelId, string ModelAssetName, string ProfileId,
    JsonObject? Form = null, bool IndependentInstance = false);

public sealed class OwnedCharacterCatalog
{
    public const string RelativePath = "data/characters/owned_character_catalog.json";
    public string DefaultCharacterId { get; }
    public IReadOnlyList<OwnedCharacterSelection> Characters { get; }
    public int CharacterConfigurationCount => Characters.Select(character => character.RoleId).Distinct().Count();

    private OwnedCharacterCatalog(string defaultCharacterId, List<OwnedCharacterSelection> characters)
    {
        DefaultCharacterId = defaultCharacterId; Characters = characters.AsReadOnly();
    }

    public static OwnedCharacterCatalog Load(string repoRoot)
    {
        var data = JsonData.ReadObject(Path.Combine(Path.GetFullPath(repoRoot), RelativePath));
        var characters = Contract.A(data, "characters").Select(row => new OwnedCharacterSelection(Contract.S(row!, "character_id"), Contract.S(row!, "display_name"),
            checked((int)Contract.I(row!, "role_id")), checked((int)Contract.I(row!, "skin_id")), Contract.S(row!, "model_id"), Contract.S(row!, "model_asset_name"), Contract.S(row!, "profile_id"))).ToList();
        var formsPath = Path.Combine(Path.GetFullPath(repoRoot), "data/characters/character_forms.json");
        if (File.Exists(formsPath))
        {
            var registry = new CharacterRegistry(repoRoot);
            foreach (var row in Contract.A(JsonData.ReadObject(formsPath), "forms"))
            {
                var parentId = Contract.S(row!, "base_character_id");
                var parentIndex = characters.FindIndex(character => character.CharacterId == parentId);
                Contract.Require(parentIndex >= 0, "形态的基础角色不存在：" + parentId);
                var parent = characters[parentIndex];
                var form = Contract.O(row!, "load");
                Contract.Require(Contract.S(form, "strategy") is "role_morph" or "mesh_parts", "未知形态加载方式。");
                var profile = registry.GetProfile(Contract.S(row!, "profile_id"));
                Contract.Require(Contract.A(profile, "mesh_asset_names").Any(name => name!.GetValue<string>() == Contract.S(row!, "model_asset_name")), "形态网格未登记。");
                // Defaults and variants of a multi-form role all get their own
                // actor; the player's combat state must never choose the form.
                characters[parentIndex] = parent with { IndependentInstance = true };
                characters.Insert(parentIndex + 1, new(Contract.S(row!, "character_id"), Contract.S(row!, "display_name"), parent.RoleId, parent.SkinId,
                    Contract.S(row!, "model_id"), Contract.S(row!, "model_asset_name"), Contract.S(row!, "profile_id"),
                    (JsonObject)form.DeepClone(), true));
            }
        }
        Contract.Require(characters.Select(character => character.CharacterId).Distinct().Count() == characters.Count, "角色与形态 ID 重复。");
        return new(Contract.S(data, "default_character_id"), characters);
    }

    public OwnedCharacterSelection Select(string? characterId = null)
    {
        var id = characterId ?? DefaultCharacterId;
        return Characters.FirstOrDefault(character => character.CharacterId == id) ?? throw new InvalidDataException("角色不在目录中：" + id);
    }
}
