using System.Data;

namespace SqlArtisan.TableClassGen;

internal interface IColumnIndexRepository
{
    ColumnIndexInfo Read(IDbConnection conn, string tableName);
}
