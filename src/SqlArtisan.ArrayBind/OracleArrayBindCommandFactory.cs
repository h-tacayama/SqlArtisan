using System.Data;
using System.Globalization;
using Oracle.ManagedDataAccess.Client;

namespace SqlArtisan.ArrayBind;

internal static class OracleArrayBindCommandFactory
{
    internal static OracleCommand Create(
        OracleConnection connection,
        IReadOnlyCollection<ISqlBuilder> statements,
        OracleTransaction? transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(statements);

        if (statements.Count == 0)
        {
            throw new ArgumentException("ExecuteArrayBind requires at least one statement.");
        }

        SqlStatement[] built = [.. statements.Select(s => s.Build(Dbms.Oracle))];

        for (int i = 1; i < built.Length; i++)
        {
            if (built[i].Text != built[0].Text)
            {
                throw new ArgumentException(
                    "ExecuteArrayBind requires every statement to build identical SQL text; "
                        + $"statement at index {i} differs from index 0.");
            }
        }

        int parameterCount = built[0].Parameters.Count;
        object[][] values = new object[parameterCount][];
        DbType?[] dbTypeHints = new DbType?[parameterCount];
        for (int p = 0; p < parameterCount; p++)
        {
            values[p] = new object[built.Length];
        }

        for (int r = 0; r < built.Length; r++)
        {
            int p = 0;
            built[r].Parameters.ForEach((_, bindValue) =>
            {
                values[p][r] = bindValue.Value;

                if (bindValue.DbType.HasValue)
                {
                    if (dbTypeHints[p].HasValue && dbTypeHints[p]!.Value != bindValue.DbType.Value)
                    {
                        throw new ArgumentException(
                            $"ExecuteArrayBind requires every row's Sql.BindNull(dbType) hint at parameter :{p} "
                                + $"to agree; found both DbType.{dbTypeHints[p]!.Value} and DbType.{bindValue.DbType.Value}.");
                    }

                    dbTypeHints[p] = bindValue.DbType;
                }

                p++;
            });
        }

        // Resolved before the command exists: every position's type is known-good — or the
        // guard has already thrown — before any OracleCommand/OracleParameter is allocated.
        OracleDbType[] resolvedTypes = new OracleDbType[parameterCount];
        for (int p = 0; p < parameterCount; p++)
        {
            resolvedTypes[p] = ResolveOracleDbType(p, dbTypeHints[p], values[p]);
        }

        OracleCommand command = connection.CreateCommand();
        command.CommandText = built[0].Text;
        command.ArrayBindCount = built.Length;

        if (transaction is not null)
        {
            command.Transaction = transaction;
        }

        for (int p = 0; p < parameterCount; p++)
        {
            command.Parameters.Add(new OracleParameter
            {
                ParameterName = p.ToString(CultureInfo.InvariantCulture),
                OracleDbType = resolvedTypes[p],
                Value = values[p],
            });
        }

        return command;
    }

    // Every non-null value at a position must map to one OracleDbType — the array binds as a
    // single typed parameter, so a mix (an int beside a decimal, an out-of-range short) would
    // bind values the type can't round-trip. An all-null position has no CLR type to infer
    // from, so an explicit Sql.BindNull(dbType) hint is required there; when both are present
    // the hint must agree with the values.
    private static OracleDbType ResolveOracleDbType(int position, DbType? dbTypeHint, object[] values)
    {
        OracleDbType? fromValues = null;
        Type? seenType = null;
        foreach (object value in values)
        {
            if (value is DBNull)
            {
                continue;
            }

            OracleDbType mapped = MapClrType(value.GetType());
            if (fromValues.HasValue && mapped != fromValues.Value)
            {
                throw new ArgumentException(
                    $"ExecuteArrayBind requires every bound value at parameter :{position} to map to the "
                        + $"same OracleDbType; a {seenType!.Name} value maps to OracleDbType.{fromValues.Value}, "
                        + $"but a {value.GetType().Name} value maps to OracleDbType.{mapped}.");
            }

            fromValues ??= mapped;
            seenType ??= value.GetType();
        }

        if (dbTypeHint.HasValue)
        {
            OracleDbType hinted = MapDbType(position, dbTypeHint.Value);
            if (fromValues.HasValue && fromValues.Value != hinted)
            {
                throw new ArgumentException(
                    $"ExecuteArrayBind cannot bind parameter :{position} as OracleDbType.{hinted} from "
                        + $"Sql.BindNull(DbType.{dbTypeHint.Value}); another row binds a {seenType!.Name} value "
                        + $"there, which maps to OracleDbType.{fromValues.Value} instead.");
            }

            return hinted;
        }

        if (fromValues.HasValue)
        {
            return fromValues.Value;
        }

        throw new ArgumentException(
            $"ExecuteArrayBind cannot infer an OracleDbType for parameter :{position}; every bound value is "
                + "null. Use Sql.BindNull(dbType) on at least one row to state the type explicitly.");
    }

    private static OracleDbType MapDbType(int position, DbType dbType) => dbType switch
    {
        DbType.Int32 => OracleDbType.Int32,
        DbType.Int64 => OracleDbType.Int64,
        DbType.Int16 => OracleDbType.Int16,
        DbType.Decimal => OracleDbType.Decimal,
        DbType.String => OracleDbType.Varchar2,
        DbType.DateTime => OracleDbType.TimeStamp,
        _ => throw new ArgumentException(
            $"ExecuteArrayBind cannot map DbType.{dbType} (parameter :{position}) to an OracleDbType; "
                + "supported types are Int32, Int64, Int16, Decimal, String, and DateTime."),
    };

    // DateTime → TimeStamp, not Date: OracleDbType.Date truncates sub-seconds in the driver —
    // a silent value change. A DATE column's engine-side conversion is its own contract.
    private static readonly IReadOnlyDictionary<Type, OracleDbType> ClrTypeMap = new Dictionary<Type, OracleDbType>
    {
        [typeof(int)] = OracleDbType.Int32,
        [typeof(long)] = OracleDbType.Int64,
        [typeof(short)] = OracleDbType.Int16,
        [typeof(decimal)] = OracleDbType.Decimal,
        [typeof(string)] = OracleDbType.Varchar2,
        [typeof(DateTime)] = OracleDbType.TimeStamp,
    };

    private static OracleDbType MapClrType(Type type)
    {
        if (ClrTypeMap.TryGetValue(type, out OracleDbType oracleDbType))
        {
            return oracleDbType;
        }

        throw new ArgumentException(
            $"ExecuteArrayBind cannot map bound value of type {type.Name} to an OracleDbType; "
                + "supported types are int, long, short, decimal, string, and DateTime.");
    }
}
