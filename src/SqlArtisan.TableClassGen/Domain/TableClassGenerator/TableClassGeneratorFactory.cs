namespace InlineSqlSharp.TableClassGen;

internal static class TableClassGeneratorFactory
{
    public static ITableClassGenerator Create(
        ConsoleUI ui,
        DbConnectionInfo connInfo,
        CodeGenerationSettings settings)
    {
        ITableInfoRepository repository = TableInfoRepositoryFactory.Create(
            connInfo,
            settings.LowercaseNames);

        return string.IsNullOrWhiteSpace(settings.SpecificTableName)
            ? new AllTablesClassGenerator(ui, repository, settings)
            : new SingleTableClassGenerator(ui, repository, settings);
    }
}
