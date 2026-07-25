namespace SqlArtisan.TableClassGen;

internal sealed class CodeGenerationSettings(
    string outputNamespace,
    bool lowercaseNames,
    string outputDirectory,
    bool createSubFolders,
    string? specificTableName = null,
    string accessibility = "internal",
    bool qualifySchema = false)
{
    private readonly string _outputDirectory = outputDirectory;
    private readonly bool _createSubFolders = createSubFolders;

    public string OutputNamespace => outputNamespace;

    public bool LowercaseNames => lowercaseNames;

    public string? SpecificTableName => specificTableName;

    public string Accessibility => accessibility;

    public bool QualifySchema => qualifySchema;

    // Path computation only: creating the directory here would make --dry-run and
    // --check, which both need the path without writing, touch the file system.
    public string CreateOutputFilePath(string tableName)
    {
        string directory = _createSubFolders && tableName.Length > 0
            ? Path.Combine(_outputDirectory, char.ToUpperInvariant(tableName[0]).ToString())
            : _outputDirectory;

        return Path.Combine(directory, $"{tableName}.cs");
    }
}
