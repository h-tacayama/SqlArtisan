namespace SqlArtisan.TableClassGen;

internal static class GeneratedFileWriter
{
    public static void Write(string path, string code)
    {
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, code);
    }
}
