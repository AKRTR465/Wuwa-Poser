using WwPoser.Compat;

// Writes the helper's session sidecars in the format the desktop prepares.
internal static class SessionFixture
{
    // Eight-column rows receive the default role/skin form columns.
    public static void Actors(string directory, IEnumerable<string> rows)
    {
        File.WriteAllLines(Path.Combine(directory, "actors.tsv"), rows.Where(row => row.Length != 0)
            .Select(row => row.Split('\t').Length == 8 ? row + "\tdefault\trole_skin\t0\t0\t\t\t\t0" : row));
        Sidecar(directory, "scene_visibility.tsv", "none\t" + GameCompat.SceneVisibilityRadiusCm + "\n");
        Sidecar(directory, "stage_floor.tsv", "capsule_bottom\t2.5\n");
    }
    public static void Actors(string directory, string text) => Actors(directory, text.Split('\n'));
    private static void Sidecar(string directory, string name, string text)
    {
        var path = Path.Combine(directory, name);
        if (!File.Exists(path)) File.WriteAllText(path, text);
    }
}
