using Microsoft.Data.Sqlite;
using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

// A throwaway on-disk SQLite database. On-disk (not :memory:) because the
// repository opens its own connection via DbConnectionInfo, so the schema must
// outlive the seeding connection.
internal sealed class TempSqliteDatabase : IDisposable
{
    private readonly string _path;

    private TempSqliteDatabase(string path)
    {
        _path = path;
        ConnectionInfo = new DbConnectionInfo(
            DbmsType.SQLite,
            string.Empty,
            0,
            path,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    public DbConnectionInfo ConnectionInfo { get; }

    public static TempSqliteDatabase Create(string schemaDdl)
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"sqlartisan_tcg_{Guid.NewGuid():N}.db");

        TempSqliteDatabase db = new(path);

        using (SqliteConnection connection = new($"Data Source={path}"))
        {
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = schemaDdl;
            command.ExecuteNonQuery();
        }

        return db;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
