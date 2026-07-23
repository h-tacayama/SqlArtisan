using System.Globalization;
using System.Reflection;
using System.Text;
using Oracle.ManagedDataAccess.Client;

namespace SqlArtisan.BulkCopy;

internal static class OracleBulkInsertCommandFactory
{
    internal static OracleCommand Create<T>(
        OracleConnection connection,
        DbTableBase table,
        IReadOnlyCollection<T> rows,
        OracleTransaction? transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(rows);

        if (rows.Count == 0)
        {
            throw new ArgumentException("BulkInsert requires at least one row.");
        }

        PropertyInfo[] columnProperties = [.. table.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(DbColumn))];

        if (columnProperties.Length == 0)
        {
            throw new ArgumentException(
                "BulkInsert requires the table class to expose at least one public DbColumn property.");
        }

        DbColumn[] columns = new DbColumn[columnProperties.Length];
        PropertyInfo[] rowProperties = new PropertyInfo[columnProperties.Length];
        OracleDbType[] oracleTypes = new OracleDbType[columnProperties.Length];

        for (int i = 0; i < columnProperties.Length; i++)
        {
            columns[i] = (DbColumn)columnProperties[i].GetValue(table)!;

            PropertyInfo? rowProperty = typeof(T).GetProperty(
                columnProperties[i].Name,
                BindingFlags.Public | BindingFlags.Instance);
            rowProperties[i] = rowProperty ?? throw new ArgumentException(
                $"BulkInsert requires the row type '{typeof(T).Name}' to have a public property "
                    + $"'{columnProperties[i].Name}' matching table class '{table.GetType().Name}'.");

            oracleTypes[i] = MapOracleDbType(rowProperty);
        }

        object[][] values = new object[columns.Length][];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = new object[rows.Count];
        }

        int rowIndex = 0;
        foreach (T row in rows)
        {
            for (int i = 0; i < rowProperties.Length; i++)
            {
                values[i][rowIndex] = rowProperties[i].GetValue(row) ?? DBNull.Value;
            }

            rowIndex++;
        }

        OracleCommand command = connection.CreateCommand();
        command.CommandText = BuildCommandText(table, columns);
        command.ArrayBindCount = rows.Count;

        if (transaction is not null)
        {
            command.Transaction = transaction;
        }

        for (int i = 0; i < columns.Length; i++)
        {
            command.Parameters.Add(new OracleParameter
            {
                ParameterName = i.ToString(CultureInfo.InvariantCulture),
                OracleDbType = oracleTypes[i],
                Value = values[i],
            });
        }

        return command;
    }

    // Bare names and :n positional markers, matching the core's Oracle INSERT emission.
    private static string BuildCommandText(DbTableBase table, DbColumn[] columns)
    {
        StringBuilder text = new();
        text.Append("INSERT INTO ").Append(table.TableName).Append(" (");

        for (int i = 0; i < columns.Length; i++)
        {
            if (i > 0)
            {
                text.Append(", ");
            }

            text.Append(columns[i].Name);
        }

        text.Append(") VALUES (");

        for (int i = 0; i < columns.Length; i++)
        {
            if (i > 0)
            {
                text.Append(", ");
            }

            text.Append(':').Append(i.ToString(CultureInfo.InvariantCulture));
        }

        text.Append(')');
        return text.ToString();
    }

    // Explicit types — ODP.NET inference reads the array's first element, which may be
    // DBNull. The map carries only types the Oracle integration lane live-proves.
    private static OracleDbType MapOracleDbType(PropertyInfo rowProperty)
    {
        Type type = Nullable.GetUnderlyingType(rowProperty.PropertyType) ?? rowProperty.PropertyType;

        if (type == typeof(int))
        {
            return OracleDbType.Int32;
        }

        if (type == typeof(long))
        {
            return OracleDbType.Int64;
        }

        if (type == typeof(short))
        {
            return OracleDbType.Int16;
        }

        if (type == typeof(decimal))
        {
            return OracleDbType.Decimal;
        }

        if (type == typeof(string))
        {
            return OracleDbType.Varchar2;
        }

        if (type == typeof(DateTime))
        {
            // TimeStamp, not Date: OracleDbType.Date truncates sub-seconds in the driver —
            // a silent value change. A DATE column's engine-side conversion is its own contract.
            return OracleDbType.TimeStamp;
        }

        throw new ArgumentException(
            $"BulkInsert cannot map property '{rowProperty.Name}' of type {type.Name} to an OracleDbType; "
                + "supported types are int, long, short, decimal, string, and DateTime, plus their nullable forms.");
    }
}
