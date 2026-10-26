using System.Globalization;
using System.Text;

namespace WWPoser.Services;

/// <summary>Offline pronunciation metadata for the labels in the character catalog.</summary>
public sealed class CharacterSearchIndex
{
    // Phrase readings avoid guessing polyphonic names from single characters.
    // New labels still match Chinese; register their pronunciation here as well.
    private static readonly Dictionary<string, string[]> Readings = new()
    {
        ["散华"] = ["san hua"], ["白芷"] = ["bai zhi"], ["凌阳"] = ["ling yang"], ["折枝"] = ["zhe zhi"],
        ["釉瑚"] = ["you hu"], ["珂莱塔"] = ["ke lai ta"], ["绯雪"] = ["fei xue"], ["洛瑟菈"] = ["luo se la"],
        ["穗穗"] = ["sui sui"], ["炽霞"] = ["chi xia"], ["安可"] = ["an ke"], ["莫特斐"] = ["mo te fei"],
        ["长离"] = ["chang li"], ["布兰特"] = ["bu lan te"], ["露帕"] = ["lu pa"], ["嘉贝莉娜"] = ["jia bei li na"],
        ["莫宁"] = ["mo ning"], ["爱弥斯"] = ["ai mi si"], ["达妮娅"] = ["da ni ya"], ["景燃"] = ["jing ran"],
        ["卡卡罗"] = ["ka ka luo"], ["吟霖"] = ["yin lin"], ["渊武"] = ["yuan wu"], ["今汐"] = ["jin xi"],
        ["相里要"] = ["xiang li yao"], ["奥古斯塔"] = ["ao gu si ta"], ["卜灵"] = ["bu ling"], ["丽贝卡"] = ["li bei ka"],
        ["漂泊者"] = ["piao bo zhe"], ["心"] = ["xin"], ["锁暝"] = ["suo ming"], ["秧秧"] = ["yang yang"],
        ["秋水"] = ["qiu shui"], ["忌炎"] = ["ji yan"], ["鉴心"] = ["jian xin"], ["夏空"] = ["xia kong"],
        ["卡提希娅"] = ["ka ti xi ya"], ["尤诺"] = ["you nuo"], ["仇远"] = ["qiu yuan", "chou yuan"],
        ["西格莉卡"] = ["xi ge li ka"], ["清宵"] = ["qing xiao"], ["维里奈"] = ["wei li nai"], ["灯灯"] = ["deng deng"],
        ["守岸人"] = ["shou an ren"], ["菲比"] = ["fei bi"], ["赞妮"] = ["zan ni"], ["千咲"] = ["qian xiao"],
        ["琳奈"] = ["lin nai"], ["陆"] = ["lu"], ["赫斯"] = ["he si"], ["露西"] = ["lu xi"], ["桃祈"] = ["tao qi"],
        ["丹瑾"] = ["dan jin"], ["椿"] = ["chun"], ["洛可可"] = ["luo ke ke"], ["坎特蕾拉"] = ["kan te lei la"],
        ["弗洛洛"] = ["fu luo luo"], ["玄翎"] = ["xuan ling"],
        ["默认"] = ["mo ren"], ["男"] = ["nan"], ["女"] = ["nv"], ["导电"] = ["dao dian"], ["气动"] = ["qi dong"],
        ["衍射"] = ["yan she"], ["湮灭"] = ["yan mie", "yin mie"], ["叱妖诰"] = ["chi yao gao"],
        ["夏日旖旎"] = ["xia ri yi ni"], ["桂枝宁芙"] = ["gui zhi ning fu"], ["呢绒梦"] = ["ni rong meng"],
        ["桃夭灼灼"] = ["tao yao zhuo zhuo"], ["星火永明"] = ["xing huo yong ming"], ["城市午茶"] = ["cheng shi wu cha"],
        ["蜜桃冰"] = ["mi tao bing"], ["薄荷糖"] = ["bo he tang"], ["机甲"] = ["ji jia"],
        ["特殊形态"] = ["te shu xing tai"], ["芙露德莉斯"] = ["fu lu de li si"]
    };
    private static readonly string[] Phrases = Readings.Keys.OrderByDescending(word => word.Length).ToArray();
    private sealed record Entry(OwnedCharacterSelection Character, string Label, string[] Pinyin, string[] Initials);
    private readonly Entry[] entries;

    public CharacterSearchIndex(IEnumerable<OwnedCharacterSelection> characters)
    {
        entries = characters.Select(character =>
        {
            var aliases = Romanize(character.DisplayName);
            return new Entry(character, Normalize(character.DisplayName), aliases.Select(alias => Normalize(alias)).ToArray(),
                aliases.Select(alias => string.Concat(alias.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => word[0]))).ToArray());
        }).ToArray();
    }

    public IReadOnlyList<OwnedCharacterSelection> Search(string? query)
    {
        var tokens = (query ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(Normalize).Where(token => token.Length > 0).ToArray();
        return entries.Where(entry => tokens.All(token => entry.Label.Contains(token, StringComparison.Ordinal) ||
            entry.Pinyin.Any(alias => alias.Contains(token, StringComparison.Ordinal)) || entry.Initials.Any(alias => alias.Contains(token, StringComparison.Ordinal))))
            .Select(entry => entry.Character).ToArray();
    }

    internal static string[] Romanize(string label)
    {
        var readings = new List<string> { "" };
        for (int offset = 0; offset < label.Length;)
        {
            string? phrase = Phrases.FirstOrDefault(word => label.AsSpan(offset).StartsWith(word, StringComparison.Ordinal));
            if (phrase is not null)
            {
                readings = readings.SelectMany(prefix => Readings[phrase].Select(reading => prefix + " " + reading)).ToList();
                offset += phrase.Length;
            }
            else
            {
                char value = label[offset++];
                if (char.IsLetterOrDigit(value)) readings = readings.Select(prefix => prefix + " " + value).ToList();
            }
        }
        return readings.ToArray();
    }

    private static string Normalize(string text)
    {
        var normalized = new StringBuilder();
        foreach (char value in text.Normalize(NormalizationForm.FormKC).ToLowerInvariant().Normalize(NormalizationForm.FormD).Replace("u\u0308", "v"))
            if (char.IsLetterOrDigit(value) && CharUnicodeInfo.GetUnicodeCategory(value) != UnicodeCategory.NonSpacingMark) normalized.Append(value);
        return normalized.ToString();
    }
}
