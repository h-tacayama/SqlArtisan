namespace SqlArtisan.TableClassGen.Tests;

internal sealed class TempFile : IDisposable
{
    private TempFile(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public static TempFile Create(string content)
    {
        string path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"sqlartisan_tcg_{Guid.NewGuid():N}.json");

        File.WriteAllText(path, content);
        return new TempFile(path);
    }

    public void Dispose()
    {
        if (File.Exists(Path))
        {
            File.Delete(Path);
        }
    }
}
