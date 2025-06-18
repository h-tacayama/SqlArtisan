namespace SqlArtisan.TableClassGen;

internal sealed class SingleTableClassGenerator(
    ConsoleUI ui,
    ITableInfoRepository repository,
    CodeGenerationSettings settings) : ITableClassGenerator
{
    private readonly ConsoleUI _ui = ui;
    private readonly ITableInfoRepository _repository = repository;
    private readonly CodeGenerationSettings _settings = settings;

    public void Generate()
    {
        string tableName = _settings.SpecificTableName!;
        _ui.ShowProgress($"Retrieving table '{tableName}' from database...");

        if (!_repository.TryGetTableInfo(tableName, out DbTableInfo? table)
            || table is null)
        {
            _ui.ShowError($"Table '{tableName}' not found in the specified schema.");
            return;
        }

        string code = table.GenerateCode(_settings.OutputNamespace);
        string outputPath = _settings.CreateOutputFilePath(table.ClassName);

        try
        {
            File.WriteAllText(outputPath, code);
            _ui.ShowSuccess($"Successfully generated class for table '{tableName}' at {outputPath}");
        }
        catch (Exception ex)
        {
            _ui.ShowError($"Failed to write file for table {tableName}: {ex.Message}");
        }
    }
}
