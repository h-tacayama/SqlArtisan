namespace SqlArtisan.TableClassGen;

internal sealed class AllTablesClassGenerator(
    ConsoleUI ui,
    ITableInfoRepository repository,
    CodeGenerationSettings settings) : ITableClassGenerator
{
    private readonly ConsoleUI _ui = ui;
    private readonly ITableInfoRepository _repository = repository;
    private readonly CodeGenerationSettings _settings = settings;

    public void Generate()
    {
        _ui.ShowProgress("Retrieving all tables from database...");
        IReadOnlyList<DbTableInfo> tables = _repository.GetAllTables();

        if (tables.Count == 0)
        {
            _ui.ShowError("No tables found in the specified schema.");
            return;
        }

        _ui.ShowProgress($"Found {tables.Count} tables. Generating class files...");
        int successCount = 0;

        foreach (DbTableInfo table in tables)
        {
            string code = table.GenerateCode(_settings.OutputNamespace);
            string outputPath = _settings.CreateOutputFilePath(table.ClassName);

            try
            {
                File.WriteAllText(outputPath, code);
                successCount++;
            }
            catch (Exception ex)
            {
                _ui.ShowError($"Failed to write file for table {table.TableName}: {ex.Message}");
            }
        }

        _ui.ShowSuccess($"Successfully generated {successCount} out of {tables.Count} table classes.");
    }
}
