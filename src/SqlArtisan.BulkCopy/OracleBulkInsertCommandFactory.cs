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

        T[] rowArray = [.. rows];

        OracleCommand command = connection.CreateCommand();
        command.CommandText = BuildCommandText(table, columns);
        command.ArrayBindCount = rowArray.Length;

        if (transaction is not null)
        {
            command.Transaction = transaction;
        }

        for (int i = 0; i < columns.Length; i++)
        {
            command.Parameters.Add(BuildParameter(i, oracleTypes[i], rowProperties[i], rowArray));
        }

        return command;
    }

    // OracleDbType.Date bound through a plain object[] silently inserts NULL under
    // ArrayBindCount, even for a non-null element (live-verified, #90) — a value-type
    // DateTime[] plus ArrayBindStatus is the array-bind-safe shape (and matches Oracle's
    // own array-bind samples, which use typed arrays rather than object[]).
    private static OracleParameter BuildParameter<T>(
        int index, OracleDbType oracleDbType, PropertyInfo rowProperty, T[] rowArray)
    {
        OracleParameter parameter = new()
        {
            ParameterName = index.ToString(CultureInfo.InvariantCulture),
            OracleDbType = oracleDbType,
        };

        if (oracleDbType == OracleDbType.Date)
        {
            DateTime[] values = new DateTime[rowArray.Length];
            OracleParameterStatus[] status = new OracleParameterStatus[rowArray.Length];

            for (int r = 0; r < rowArray.Length; r++)
            {
                if (rowProperty.GetValue(rowArray[r]) is DateTime value)
                {
                    values[r] = value;
                    status[r] = OracleParameterStatus.Success;
                }
                else
                {
                    status[r] = OracleParameterStatus.NullInsert;
                }
            }

            parameter.Value = values;
            parameter.ArrayBindStatus = status;
        }
        else
        {
            object[] values = new object[rowArray.Length];
            for (int r = 0; r < rowArray.Length; r++)
            {
                values[r] = rowProperty.GetValue(rowArray[r]) ?? DBNull.Value;
            }

            parameter.Value = values;
        }

        return parameter;
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
            return OracleDbType.Date;
        }

        throw new ArgumentException(
            $"BulkInsert cannot map property '{rowProperty.Name}' of type {type.Name} to an OracleDbType; "
                + "supported types are int, long, short, decimal, string, and DateTime, plus their nullable forms.");
    }
}
