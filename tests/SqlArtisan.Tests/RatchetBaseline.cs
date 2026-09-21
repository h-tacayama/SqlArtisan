namespace SqlArtisan.Tests;

// A per-file count baseline kept in a text file ("path count" per line): the
// tree must match it exactly, so a count may only shrink deliberately.
internal static class RatchetBaseline
{
    internal static void AssertMatches(
        string baselinePath, Dictionary<string, int> found, string what, string remedy)
    {
        Dictionary<string, int> allowed = new(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(baselinePath))
        {
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            int split = line.LastIndexOf(' ');
            allowed[line[..split]] = int.Parse(
                line[(split + 1)..],
                System.Globalization.CultureInfo.InvariantCulture);
        }

        List<string> drift = [];
        foreach (
            (string file, int count) in found.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (count != allowed.GetValueOrDefault(file))
            {
                drift.Add($"{file} {count} (baseline {allowed.GetValueOrDefault(file)})");
            }
        }

        foreach ((string file, int count) in allowed)
        {
            if (!found.ContainsKey(file))
            {
                drift.Add($"{file} 0 (baseline {count})");
            }
        }

        Assert.True(
            drift.Count == 0,
            $"{drift.Count} file(s) drifted from {Path.GetFileName(baselinePath)} — {what}; "
                + $"{remedy}:\n  "
                + string.Join("\n  ", drift));
    }
}
