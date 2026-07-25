using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

internal static class TestSettings
{
    public static CodeGenerationSettings Create(
        string outputNamespace = "Generated.Tables",
        string accessibility = "internal",
        bool qualifySchema = false) =>
        new(
            outputNamespace,
            lowercaseNames: false,
            outputDirectory: ".",
            createSubFolders: false,
            specificTableName: null,
            accessibility: accessibility,
            qualifySchema: qualifySchema);
}
