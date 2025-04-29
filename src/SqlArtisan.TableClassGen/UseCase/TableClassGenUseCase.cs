namespace SqlArtisan.TableClassGen;

internal sealed class TableClassGenUseCase
{
    public void Execute()
    {
        ConsoleUI ui = new();

        try
        {
            ui.ShowProgress("Starting table class generation process...");

            DbConnectionInfo connInfo = ui.ReadDatabaseConnectionInfo();
            CodeGenerationSettings settings = ui.ReadCodeGenerationSettings();

            ITableClassGenerator generator = TableClassGeneratorFactory.Create(
                ui,
                connInfo,
                settings);

            ui.ShowProgress("");

            generator.Generate();

            ui.ShowSuccess("Table class generation process completed successfully.");
        }
        catch (Exception ex)
        {
            ui.ShowError(ex.Message);
        }

        ui.ShowProgress("");
    }
}
