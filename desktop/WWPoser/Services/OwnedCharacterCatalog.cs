namespace WWPoser.Services;

public sealed record OwnedCharacterSelection(string CharacterId, string DisplayName, int RoleId, int SkinId, string ModelId, string ModelAssetName, string ProfileId);

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
        return new(Contract.S(data, "default_character_id"), characters);
    }

    public OwnedCharacterSelection Select(string? characterId = null)
    {
        var id = characterId ?? DefaultCharacterId;
        return Characters.FirstOrDefault(character => character.CharacterId == id) ?? throw new InvalidDataException("角色不在目录中：" + id);
    }
}
