namespace SqlArtisan.TableClassGen;

internal sealed class CodeGenerationSettings(
    string outputNamespace,
    bool lowercaseNames,
    string outputDirectory,
    bool createSubFolders,
    string? specificTableName = null)
{
    private readonly string _outputDirectory = outputDirectory;
    private readonly bool _createSubFolders = createSubFolders;

    public string OutputNamespace => outputNamespace;

    public bool LowercaseNames => lowercaseNames;

    public string? SpecificTableName => specificTableName;

    public string CreateOutputFilePath(string tableName)
    {
        string directory = _outputDirectory;

        if (_createSubFolders && tableName.Length > 0)
        {
            char firstChar = char.ToUpperInvariant(tableName[0]);
            string subDirectory = Path.Combine(directory, firstChar.ToString());

            if (!Directory.Exists(subDirectory))
            {
                Directory.CreateDirectory(subDirectory);
            }

            directory = subDirectory;
        }

        return Path.Combine(directory, $"{tableName}.cs");
    }
}
