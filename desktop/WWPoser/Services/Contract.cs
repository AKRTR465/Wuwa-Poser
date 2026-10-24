namespace WWPoser.Services;
internal static class Contract
{
    public const string Build = "c4956c30be531f3b992eabe42fa92fa9d10186000fe12e7546a11e1179bdac18";
    public static void Require(bool value, string reason)
    {
        if (!value)
            throw new InvalidDataException(reason);
    }

    public static JsonNode N(JsonNode node, string key) => node[key] ?? throw new InvalidDataException("缺少字段：" + key);
    public static JsonObject O(JsonNode node, string key) => N(node, key).AsObject();
    public static JsonArray A(JsonNode node, string key) => N(node, key).AsArray();
    public static string S(JsonNode node, string key) => N(node, key).GetValue<string>();
    public static long I(JsonNode node, string key) => N(node, key).GetValue<long>();
    public static double D(JsonNode node, string key) => N(node, key).GetValue<double>();
    public static bool True(JsonNode node, string key) => node[key] is JsonValue v && v.TryGetValue<bool>(out var b) && b;
    public static bool Equal(JsonNode? a, JsonNode? b) => JsonNode.DeepEquals(a, b);
    public static ulong Number(JsonNode n)
    {
        if (n is JsonValue v && v.TryGetValue<ulong>(out var x))
            return x;
        var text = n.GetValue<string>();
        return text.StartsWith("0x", StringComparison.Ordinal) ? Convert.ToUInt64(text[2..], 16) : Convert.ToUInt64(text);
    }

    public static JsonObject Select(JsonNode n, IEnumerable<string> keys)
    {
        var o = new JsonObject();
        foreach (var k in keys)
            o[k] = N(n, k).DeepClone();
        return o;
    }
}
