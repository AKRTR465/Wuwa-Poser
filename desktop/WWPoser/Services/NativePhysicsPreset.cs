namespace WWPoser.Services;

internal static class NativePhysicsPreset
{
    internal readonly record struct Control(string Command, double Value, uint Group, uint Member);
    public static readonly string[] Commands = ["native_damping", "native_stiffness", "native_limit_angle"];
    public static readonly double[] Maxima = [1, 1, 180];
    public const string GravityCommand = "native_gravity";
    public const string PartGravityCommand = "part_gravity";
    public const double GravityMaximum = 1500;

    public static bool HasOverrides(JsonObject? preset) => preset?["groups"] is JsonArray { Count: > 0 } ||
        preset?["sleeves"] is JsonArray { Count: > 0 } || preset?["disabled_capsules"] is JsonArray { Count: > 0 } ||
        preset?["gravity"] is JsonArray { Count: > 0 } || preset?["part_gravity"] is JsonArray { Count: > 0 } ||
        preset?[CapsuleAuthoring.PropertyName] is JsonArray { Count: > 0 } || CapsulePresetEdits.HasEdits(preset);

    public static IReadOnlyList<CapsuleDraftGroup> ValidateDraft(JsonObject preset, string? characterId, string? profileId = null)
    {
        if (preset["schema"]?.GetValue<int>() != 1 || preset["kind"]?.GetValue<string>() != "native_physics_group_preset" ||
            string.IsNullOrEmpty(characterId) || preset["character_id"]?.GetValue<string>() != characterId ||
            string.IsNullOrEmpty(preset["profile_id"]?.GetValue<string>()) || profileId is not null && preset["profile_id"]?.GetValue<string>() != profileId)
            throw new InvalidDataException("物理预设属于其他角色、皮肤或预设版本。");
        foreach (var entry in Contract.A(preset, "groups"))
        {
            ValidateTarget(entry!);
            if (entry!["root"] is { } root) _ = root.GetValue<string>();
            ReadValues(Contract.A(entry, "parameters"));
        }
        foreach (var entry in preset["sleeves"] is null ? [] : Contract.A(preset, "sleeves"))
        {
            ValidateTarget(entry!);
            _ = Contract.S(entry!, "root");
            var value = Contract.D(entry!, "follow");
            Contract.Require(double.IsFinite(value) && value is >= 0 and <= 1, "预设中的肩部跟随值越界。");
        }
        foreach (var entry in preset["disabled_capsules"] is null ? [] : Contract.A(preset, "disabled_capsules"))
        {
            ValidateTarget(entry!);
            _ = Contract.S(entry!, "driver");
        }
        foreach (var entry in preset["gravity"] is null ? [] : Contract.A(preset, "gravity"))
        {
            ValidateTarget(entry!);
            var strength = Contract.D(entry!, "strength");
            Contract.Require(double.IsFinite(strength) && strength is >= 0 and <= GravityMaximum, "预设中的重力强度越界。");
        }
        _ = ReadPartGravity(preset);
        CapsulePresetEdits.Validate(preset);
        return CapsuleAuthoring.Read(preset);

        static void ValidateTarget(JsonNode entry)
        {
            _ = Contract.S(entry, "curve_name");
            foreach (var bone in Contract.A(entry, "root_bones")) _ = bone!.GetValue<string>();
        }
    }

    private static Dictionary<string, double> ReadPartGravity(JsonObject? preset)
    {
        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (preset?["part_gravity"] is null) return values;
        foreach (var row in Contract.A(preset, "part_gravity"))
        {
            var root = Contract.S(row!, "root");
            var strength = Contract.D(row!, "strength");
            Contract.Require(root.Length > 0 && double.IsFinite(strength) && strength is >= 0 and <= GravityMaximum &&
                values.TryAdd(root, strength), "预设中的部件重力无效或重复。");
        }
        return values;
    }

    public static double[] DraftPartGravity(JsonObject? preset, IEnumerable<string> roots)
    {
        var values = ReadPartGravity(preset);
        return roots.Select(root => values.GetValueOrDefault(root, -1)).ToArray();
    }

    public static void SetDraftPartGravity(JsonObject preset, IEnumerable<string> roots, double strength)
    {
        var values = ReadPartGravity(preset);
        foreach (var root in roots)
            if (strength == -1) values.Remove(root);
            else values[root] = strength;
        preset["part_gravity"] = new JsonArray(values.Select(entry => (JsonNode)new JsonObject
            { ["root"] = entry.Key, ["strength"] = entry.Value }).ToArray());
    }

    public static double[] DraftValues(JsonObject? preset, PhysicsGroup group, string? root) =>
        FindDraft(preset?["groups"] as JsonArray, group, root) is { } row ? ReadValues(Contract.A(row, "parameters")) : [-1, -1, -1];

    public static double DraftFollow(JsonObject? preset, PhysicsGroup group, string root) =>
        FindDraft(preset?["sleeves"] as JsonArray, group, root) is { } row ? Contract.D(row, "follow") : 1;

    public static double DraftGravity(JsonObject? preset, PhysicsGroup group) =>
        FindDraft(preset?["gravity"] as JsonArray, group, null) is { } row ? Contract.D(row, "strength") : -1;

    public static void SetDraftGravity(JsonObject preset, PhysicsGroup group, double strength)
    {
        var rows = preset["gravity"] as JsonArray;
        if (rows is null) { rows = new(); preset["gravity"] = rows; }
        if (FindDraft(rows, group, null) is { } previous) rows.Remove(previous);
        if (strength == -1) return;
        var target = DraftTarget(group, null);
        target["strength"] = strength;
        rows.Add(target);
    }

    public static void SetDraftValue(JsonObject preset, PhysicsGroup group, string? root, int parameter, double value)
    {
        var values = DraftValues(preset, group, root);
        values[parameter] = value;
        var rows = Contract.A(preset, "groups");
        if (FindDraft(rows, group, root) is { } previous) rows.Remove(previous);
        if (values.All(number => number == -1)) return;
        var entry = DraftTarget(group, root);
        entry["parameters"] = new JsonArray(values.Select(number => (JsonNode)JsonValue.Create(number)!).ToArray());
        rows.Add(entry);
    }

    public static void SetDraftFollow(JsonObject preset, PhysicsGroup group, string root, double follow)
    {
        var rows = preset["sleeves"] as JsonArray;
        if (rows is null) { rows = new(); preset["sleeves"] = rows; }
        if (FindDraft(rows, group, root) is { } previous) rows.Remove(previous);
        if (follow == 1) return;
        var entry = DraftTarget(group, root);
        entry["follow"] = follow;
        rows.Add(entry);
    }

    public static void SetDraftCapsuleDriver(JsonObject preset, JsonObject group, string driver, bool enabled)
    {
        var rows = preset["disabled_capsules"] as JsonArray;
        if (rows is null) { rows = new(); preset["disabled_capsules"] = rows; }
        var curve = Contract.S(group, "curve_name");
        var roots = Contract.A(group, "root_bones");
        var names = roots.Select(root => root!.GetValue<string>()).ToArray();
        var previous = rows.OfType<JsonObject>().SingleOrDefault(row => Matches(row, curve, names) &&
            string.Equals(Contract.S(row, "driver"), driver, StringComparison.OrdinalIgnoreCase));
        if (previous is not null) rows.Remove(previous);
        if (!enabled) rows.Add(new JsonObject { ["curve_name"] = curve, ["root_bones"] = roots.DeepClone(), ["driver"] = driver });
    }

    private static JsonObject? FindDraft(JsonArray? rows, PhysicsGroup group, string? root) => rows?.OfType<JsonObject>().FirstOrDefault(row =>
        Matches(row, group.Curve ?? "None", group.Chains.Select(chain => chain.Root)) &&
        string.Equals(row["root"]?.GetValue<string>(), root, StringComparison.OrdinalIgnoreCase));

    private static JsonObject DraftTarget(PhysicsGroup group, string? root)
    {
        var row = new JsonObject { ["curve_name"] = group.Curve ?? "None",
            ["root_bones"] = new JsonArray(group.Chains.Select(chain => (JsonNode)JsonValue.Create(chain.Root)!).ToArray()) };
        if (root is not null) row["root"] = root;
        return row;
    }

    public static IReadOnlyList<Control> Plan(JsonObject preset, OwnedCharacterSelection character, IEnumerable<JsonObject> liveGroups,
        IEnumerable<JsonObject>? liveParts = null)
    {
        var groups = liveGroups.ToArray();
        var controls = new List<Control>();
        foreach (var (group, chain, parameters, current) in Resolve(preset, character, groups))
            for (var i = 0; i < parameters.Length; i++)
                if (Math.Abs(parameters[i] - current[i]) > .000001) controls.Add(new(Commands[i], parameters[i], group, chain));
        foreach (var (group, chain, follow, current) in ResolveSleeves(preset, groups))
            if (Math.Abs(follow - current) > .000001) controls.Add(new("native_shoulder_follow", follow, group, chain));
        foreach (var (group, driver, enabled, current) in ResolveCapsules(preset, groups))
            if (enabled != current) controls.Add(new("native_capsule_collision", enabled ? 1 : 0, group, driver));
        var gravity = new Dictionary<uint, double>();
        foreach (var entry in preset["gravity"] is null ? [] : Contract.A(preset, "gravity"))
        {
            var roots = Contract.A(entry!, "root_bones").Select(root => root!.GetValue<string>());
            var matches = groups.Where(group => Matches(group, Contract.S(entry!, "curve_name"), roots)).ToArray();
            Contract.Require(matches.Length == 1 && matches[0]["gravity"] is JsonObject, "预设重力未匹配到可编辑的原生物理组。");
            Contract.Require(gravity.TryAdd(matches[0]["group_id"]!.GetValue<uint>(), Contract.D(entry!, "strength")), "预设中的重力组重复。");
        }
        foreach (var group in groups.Where(group => group["gravity"] is JsonObject))
        {
            var id = group["group_id"]!.GetValue<uint>();
            var strength = gravity.GetValueOrDefault(id, -1);
            if (Math.Abs(strength - Contract.D(group["gravity"]!, "requested")) > .000001) controls.Add(new(GravityCommand, strength, id, 0));
        }
        var parts = ReadPartGravity(preset);
        foreach (var part in liveParts ?? [])
        {
            var root = Contract.S(part, "root");
            var strength = parts.Remove(root, out var value) ? value : -1;
            if (Math.Abs(strength - Contract.D(part, "strength")) > .000001)
                controls.Add(new(PartGravityCommand, strength, 0, Contract.N(part, "id").GetValue<uint>()));
        }
        Contract.Require(parts.Count == 0, "预设未匹配到本次播放的物理部件。");
        return controls;
    }

    public static async Task ApplyAsync(string directory, uint slot, IEnumerable<Control> controls, CancellationToken cancellation)
    {
        foreach (var control in controls)
            await PlaybackControl.SendAsync(directory, control.Command, control.Value, cancellation, slot, control.Group, control.Member);
    }

    public static JsonObject Create(OwnedCharacterSelection character, IEnumerable<JsonObject> groups,
        IEnumerable<JsonObject>? parts = null)
    {
        var parameters = new JsonArray();
        var sleeves = new JsonArray();
        var capsules = new JsonArray();
        var gravity = new JsonArray();
        foreach (var group in groups)
        {
            AddSettings(group);
            if (group["gravity"] is JsonObject gravityState && Contract.D(gravityState, "requested") is var strength && strength >= 0)
                gravity.Add(new JsonObject { ["curve_name"] = group["curve_name"]!.DeepClone(),
                    ["root_bones"] = group["root_bones"]!.DeepClone(), ["strength"] = strength });
            if (group["chains"] is JsonArray settingsChains)
                foreach (var chain in settingsChains.OfType<JsonObject>()) AddSettings(chain);
            void AddSettings(JsonObject entry)
            {
                if (entry["settings"]?["requested"] is not JsonArray values || ReadValues(values).All(value => value == -1)) return;
                var row = new JsonObject { ["curve_name"] = group["curve_name"]!.DeepClone(),
                    ["root_bones"] = group["root_bones"]!.DeepClone(), ["parameters"] = values.DeepClone() };
                if (entry["root"] is { } root) row["root"] = root.DeepClone();
                parameters.Add(row);
            }
            if (group["capsules"] is JsonArray colliderRows)
                foreach (var collider in colliderRows.OfType<JsonObject>())
                    if (collider["enabled"]?.GetValue<bool>() == false)
                        capsules.Add(new JsonObject { ["curve_name"] = group["curve_name"]!.DeepClone(),
                            ["root_bones"] = group["root_bones"]!.DeepClone(), ["driver"] = collider["driver"]!.DeepClone() });
            if (group["sleeves"] is not JsonArray chains) continue;
            foreach (var sleeve in chains.OfType<JsonObject>())
                if (Contract.D(sleeve, "follow") != 1)
                    sleeves.Add(new JsonObject { ["curve_name"] = group["curve_name"]!.DeepClone(),
                        ["root_bones"] = group["root_bones"]!.DeepClone(), ["root"] = sleeve["root"]!.DeepClone(), ["follow"] = sleeve["follow"]!.DeepClone() });
        }
        var result = new JsonObject { ["schema"] = 1, ["kind"] = "native_physics_group_preset", ["character_id"] = character.CharacterId,
            ["profile_id"] = character.ProfileId, ["groups"] = parameters, ["sleeves"] = sleeves, ["disabled_capsules"] = capsules, ["gravity"] = gravity };
        if (parts is not null)
            foreach (var part in parts)
                if (Contract.D(part, "strength") is var strength && strength >= 0)
                    SetDraftPartGravity(result, [Contract.S(part, "root")], strength);
        return result;
    }

    public static IReadOnlyList<(uint Group, uint Chain, double Follow, double Current)> ResolveSleeves(JsonObject preset, IEnumerable<JsonObject> liveGroups)
    {
        var live = liveGroups.ToArray();
        var requested = new Dictionary<(uint Group, uint Chain), double>();
        var entries = preset["sleeves"] is null ? new JsonArray() : Contract.A(preset, "sleeves");
        foreach (var entry in entries.OfType<JsonObject>())
        {
            var roots = Contract.A(entry, "root_bones").Select(root => root!.GetValue<string>());
            var candidates = live.Where(group => Matches(group, Contract.S(entry, "curve_name"), roots)).ToArray();
            Contract.Require(candidates.Length == 1, "预设中的袖套物理组与当前演员不匹配。");
            var chain = Contract.A(candidates[0], "sleeves").OfType<JsonObject>().SingleOrDefault(row =>
                string.Equals(Contract.S(row, "root"), Contract.S(entry, "root"), StringComparison.OrdinalIgnoreCase));
            Contract.Require(chain is not null, "预设中的袖套骨链与当前演员不匹配。");
            var key = (Contract.N(candidates[0], "group_id").GetValue<uint>(), Contract.N(chain!, "chain_id").GetValue<uint>());
            var follow = Contract.D(entry, "follow");
            Contract.Require(double.IsFinite(follow) && follow >= 0 && follow <= 1 && requested.TryAdd(key, follow), "预设中的肩部跟随值越界或重复。");
        }
        return live.SelectMany(group => (group["sleeves"] as JsonArray ?? []).OfType<JsonObject>().Select(chain =>
        {
            var key = (Contract.N(group, "group_id").GetValue<uint>(), Contract.N(chain, "chain_id").GetValue<uint>());
            return (key.Item1, key.Item2, requested.GetValueOrDefault(key, 1), Contract.D(chain, "follow"));
        })).ToArray();
    }

    public static IReadOnlyList<(uint Group, uint Driver, bool Enabled, bool Current)> ResolveCapsules(JsonObject preset, IEnumerable<JsonObject> liveGroups)
    {
        var live = liveGroups.ToArray();
        var disabled = new HashSet<(uint Group, uint Driver)>();
        foreach (var entry in preset["disabled_capsules"] is null ? new JsonArray() : Contract.A(preset, "disabled_capsules"))
        {
            var roots = Contract.A(entry!, "root_bones").Select(root => root!.GetValue<string>());
            var group = live.SingleOrDefault(row => Matches(row, Contract.S(entry!, "curve_name"), roots));
            Contract.Require(group is not null, "预设中的碰撞组与当前演员不匹配。");
            var capsule = (group!["capsules"] as JsonArray)?.OfType<JsonObject>().SingleOrDefault(row =>
                string.Equals(Contract.S(row, "driver"), Contract.S(entry!, "driver"), StringComparison.OrdinalIgnoreCase));
            Contract.Require(capsule is not null, "预设中的碰撞驱动骨与当前演员不匹配。");
            var key = (group["group_id"]!.GetValue<uint>(), capsule!["driver_id"]!.GetValue<uint>());
            Contract.Require(disabled.Add(key), "预设中的碰撞体重复。");
        }
        return live.SelectMany(group => (group["capsules"] as JsonArray ?? []).OfType<JsonObject>().Select(capsule =>
        {
            var key = (group["group_id"]!.GetValue<uint>(), capsule["driver_id"]!.GetValue<uint>());
            return (key.Item1, key.Item2, !disabled.Contains(key), capsule["enabled"]!.GetValue<bool>());
        })).ToArray();
    }

    public static IReadOnlyList<(uint Group, uint Chain, double[] Parameters, double[] Current)> Resolve(JsonObject preset,
        OwnedCharacterSelection character, IEnumerable<JsonObject> liveGroups)
    {
        ValidateDraft(preset, character.CharacterId, character.ProfileId);
        var groups = Contract.A(preset, "groups");
        var current = liveGroups.ToArray();
        var targets = current.SelectMany(group => new[] { group }.Concat((group["chains"] as JsonArray ?? []).OfType<JsonObject>())
            .Select(entry => (Group: Contract.N(group, "group_id").GetValue<uint>(), Chain: entry["chain_id"]?.GetValue<uint>() ?? 0, Entry: entry))).ToArray();
        Contract.Require(groups.Count <= targets.Length, "物理预设超出可用的物理组和骨链数量。");
        var byGroup = new Dictionary<(uint Group, uint Chain), double[]>();
        foreach (var group in groups)
        {
            var roots = Contract.A(group!, "root_bones").Select(root => root!.GetValue<string>());
            var matches = current.Where(row => Matches(row, Contract.S(group!, "curve_name"), roots)).ToArray();
            Contract.Require(matches.Length == 1, "预设物理组与本次动画图的匹配不唯一。");
            var groupId = Contract.N(matches[0], "group_id").GetValue<uint>();
            uint chainId = 0;
            if (group!["root"] is JsonValue root)
            {
                var chain = (matches[0]["chains"] as JsonArray)?.OfType<JsonObject>().SingleOrDefault(entry =>
                    string.Equals(Contract.S(entry, "root"), root.GetValue<string>(), StringComparison.OrdinalIgnoreCase));
                Contract.Require(chain is not null, "预设中的物理骨链与当前演员不匹配。");
                chainId = Contract.N(chain!, "chain_id").GetValue<uint>();
            }
            Contract.Require(groupId > 0 && byGroup.TryAdd((groupId, chainId), ReadValues(Contract.A(group, "parameters"))), "物理预设的物理目标重复或无效。");
        }
        return targets.Select(target =>
        {
            var requested = ReadValues(Contract.A(Contract.O(target.Entry, "settings"), "requested"));
            return (target.Group, target.Chain, byGroup.GetValueOrDefault((target.Group, target.Chain)) ?? [-1, -1, -1], requested);
        }).ToArray();
    }

    public static bool Matches(JsonObject row, string curve, IEnumerable<string> roots) =>
        string.Equals(row["curve_name"]?.GetValue<string>(), curve, StringComparison.OrdinalIgnoreCase) &&
        row["root_bones"] is JsonArray bones && roots.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(bones.Select(bone => bone!.GetValue<string>()));

    private static double[] ReadValues(JsonArray values)
    {
        Contract.Require(values.Count == Commands.Length, "物理预设参数数量有误。");
        var result = values.Select(value => value?.GetValue<double>() ?? double.NaN).ToArray();
        for (var i = 0; i < result.Length; i++)
            Contract.Require(result[i] == -1 || double.IsFinite(result[i]) && result[i] >= 0 && result[i] <= Maxima[i], "物理预设参数值越界。");
        return result;
    }
}
