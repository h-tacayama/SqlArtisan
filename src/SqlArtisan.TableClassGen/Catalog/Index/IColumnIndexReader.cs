using System.Data;

namespace SqlArtisan.TableClassGen;

internal interface IColumnIndexReader
{
    ColumnIndexInfo Read(IDbConnection conn, string tableName);
}
