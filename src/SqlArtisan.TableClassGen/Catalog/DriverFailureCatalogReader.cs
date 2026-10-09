using System.Data.Common;

namespace SqlArtisan.TableClassGen;

// DbException is where the drivers report server and file failures; anything else, a
// guard that already names its fix or a bug, passes through as thrown.
internal sealed class DriverFailureCatalogReader(ICatalogReader inner, DbConnectionInfo connInfo)
    : ICatalogReader
{
    public IReadOnlyList<CatalogTable> GetAllTables()
    {
        try
        {
            return inner.GetAllTables();
        }
        catch (DbException ex)
        {
            throw Wrap(ex);
        }
    }

    public bool TryGetTable(string tableName, out CatalogTable? table)
    {
        try
        {
            return inner.TryGetTable(tableName, out table);
        }
        catch (DbException ex)
        {
            throw Wrap(ex);
        }
    }

    private CommandLineException Wrap(DbException ex) =>
        new($"{connInfo.CannotReadCatalogMessage} ({ex.Message})");
}
