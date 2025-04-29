namespace SqlArtisan.TableClassGen;

internal sealed class TableClassGenerator(ConsoleUI ui)
{
    private readonly ConsoleUI _ui = ui;

    public void GenerateAllTableClasses(
        ITableInfoRepository repository,
        CodeGenerationSettings settings)
    {
        _ui.ShowProgress("Retrieving all tables from database...");
        IReadOnlyList<DbTableInfo> tables = repository.GetAllTables();

        if (tables.Count == 0)
        {
            _ui.ShowError("No tables found in the specified schema.");
            return;
        }

        _ui.ShowProgress($"Found {tables.Count} tables. Generating class files...");
        int successCount = 0;

        foreach (DbTableInfo table in tables)
        {
            string code = table.GenerateCode(settings.OutputNamespace);
            string outputPath = settings.CreateOutputFilePath(table.PascalCaseName);

            try
            {
                File.WriteAllText(outputPath, code);
                successCount++;
            }
            catch (Exception ex)
            {
                _ui.ShowError($"Failed to write file for table {table.Name}: {ex.Message}");
            }
        }

        _ui.ShowSuccess($"Successfully generated {successCount} out of {tables.Count} table classes.");
    }
}
