namespace SqlArtisan.TableClassGen;

internal sealed class CodeGenerationSettings(
    string outputNamespace,
    bool lowercaseNames,
    string outputDirectory,
    bool createSubFolders,
    IReadOnlyList<string>? tableNames = null,
    string accessibility = "internal",
    bool qualifySchema = false)
{
    private readonly bool _createSubFolders = createSubFolders;

    public string OutputNamespace => outputNamespace;

    public bool LowercaseNames => lowercaseNames;

    public string OutputDirectory => outputDirectory;

    // Empty means every table in the schema.
    public IReadOnlyList<string> TableNames => tableNames ?? [];

    public string Accessibility => accessibility;

    public bool QualifySchema => qualifySchema;

    // Path computation only: creating the directory here would make --dry-run and
    // --check, which both need the path without writing, touch the file system.
    public string CreateOutputFilePath(string className)
    {
        string directory = _createSubFolders && className.Length > 0
            ? Path.Combine(outputDirectory, char.ToUpperInvariant(className[0]).ToString())
            : outputDirectory;

        return Path.Combine(directory, $"{className}.cs");
    }
}
