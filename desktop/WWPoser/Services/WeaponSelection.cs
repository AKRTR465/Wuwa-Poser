using System.Text.RegularExpressions;

namespace WWPoser.Services;
public static class WeaponSelection
{
    private static readonly Regex WeaponLabel = new("weapon|sword|scabbard|sheath|blade|katana|pistol|rifle|cannon|spear|gauntlet|bracer|rectifier|broadblade|(?<![a-z])(?:bow|gun)", RegexOptions.IgnoreCase);
    private static readonly Regex SocketLabel = new("weapon|sword|scabbard|blade|gun|pistol|rifle|cannon|spear|bow|hand|grip|hold", RegexOptions.IgnoreCase);
    private static readonly Regex ExcludedLabel = new("effect|particle|niagara|trail|hair|cloth|skirt|ribbon|sleeve|facial|character|paraglider|soarwing|wingcase|(?:^|[_\\W])fx(?:$|[_\\W0-9])", RegexOptions.IgnoreCase);
    private static HashSet<string> Rule(JsonObject rules, string key) => rules[key] is JsonArray a ? a.Select(x => x!.GetValue<string>()).ToHashSet() : [];

    public static JsonArray Select(string scan, JsonObject profile)
    {
        var inventory = JsonData.ReadObject(Path.Combine(scan, "weapons_readonly.json"));
        var body = Contract.O(inventory, "body_identity");
        var pawn = Contract.O(inventory, "pawn_identity");
        var rules = profile["weapon_selection"] as JsonObject ?? new JsonObject();
        var weapons = new JsonArray();
        var selected = new HashSet<string>();
        foreach (var row in Contract.A(inventory, "components"))
        {
            if (row?["identity"] is not JsonObject identity || row["asset"] is not JsonObject asset || asset["name"]?.ToString() is not { } name ||
                identity["name"]?.ToString() is not { } component || row["socket_name"]?.ToString() is not { } socket || identity["address"]?.ToString() is not { } address)
                continue;
            var type = identity["class"]?.ToString();
            var offset = type is "SkeletalMeshComponent" or "SkinnedMeshComponent" ? "0x5d0" : type == "StaticMeshComponent" ? "0x5e0" : null;
            var weapon = name != "EParagliderMd10011" && !Rule(rules, "optional_prop_asset_names").Contains(name) && !Rule(rules, "excluded_asset_names").Contains(name) && !ExcludedLabel.IsMatch(name + " " + component) &&
                (WeaponLabel.IsMatch(name) || WeaponLabel.IsMatch(component) || Rule(rules, "weapon_asset_names").Contains(name) || Rule(rules, "weapon_component_names").Contains(component)) &&
                (SocketLabel.IsMatch(socket) || Rule(rules, "weapon_socket_names").Contains(socket)) &&
                offset != null && asset["pointer_offset"]?.ToString() == offset && asset["identity"]?["class"]?.ToString() == (offset == "0x5e0" ? "StaticMesh" : "SkeletalMesh") &&
                !Contract.Equal(identity["address"], body["address"]) && Contract.Equal(row["owner_address"], pawn["address"]) && Contract.Equal(row["attach_parent"], body["address"]) && row["attachment_chain"] is JsonArray { Count: 1 };
            if (!weapon || !selected.Add(address))
                continue;
            var target = (JsonObject)identity.DeepClone();
            foreach (var field in new[] { "owner_address", "attach_parent", "socket_name", "socket_fname_words" })
                target[field] = row[field]?.DeepClone();
            target["asset"] = asset.DeepClone();
            target["asset_offset"] = offset;
            weapons.Add(target);
        }

        return weapons;
    }
}
