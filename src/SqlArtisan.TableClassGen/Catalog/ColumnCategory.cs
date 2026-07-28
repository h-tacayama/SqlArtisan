using System;
using System.Collections.Generic;

namespace SqlArtisan.TableClassGen;

// The engine-specific knowledge stays here: the analyzer only ever sees the
// category, so neither side needs a five-dialect type table.
internal static class ColumnCategory
{
    // Names that mean the same thing on every engine that has them. The ones that
    // do not are resolved by Collides before this is consulted.
    private static readonly IReadOnlyDictionary<string, DbColumnType> Shared =
        new Dictionary<string, DbColumnType>(StringComparer.Ordinal)
        {
            ["char"] = DbColumnType.Text,
            ["char varying"] = DbColumnType.Text,
            ["character"] = DbColumnType.Text,
            ["character varying"] = DbColumnType.Text,
            ["clob"] = DbColumnType.Text,
            ["long"] = DbColumnType.Text,
            ["longtext"] = DbColumnType.Text,
            ["mediumtext"] = DbColumnType.Text,
            ["nchar"] = DbColumnType.Text,
            ["nclob"] = DbColumnType.Text,
            ["ntext"] = DbColumnType.Text,
            ["nvarchar"] = DbColumnType.Text,
            ["nvarchar2"] = DbColumnType.Text,
            ["text"] = DbColumnType.Text,
            ["tinytext"] = DbColumnType.Text,
            ["varchar"] = DbColumnType.Text,
            ["varchar2"] = DbColumnType.Text,

            ["bigint"] = DbColumnType.Numeric,
            ["binary_double"] = DbColumnType.Numeric,
            ["binary_float"] = DbColumnType.Numeric,
            ["dec"] = DbColumnType.Numeric,
            ["decimal"] = DbColumnType.Numeric,
            ["double"] = DbColumnType.Numeric,
            ["double precision"] = DbColumnType.Numeric,
            ["fixed"] = DbColumnType.Numeric,
            ["float"] = DbColumnType.Numeric,
            ["float4"] = DbColumnType.Numeric,
            ["float8"] = DbColumnType.Numeric,
            ["int"] = DbColumnType.Numeric,
            ["int2"] = DbColumnType.Numeric,
            ["int4"] = DbColumnType.Numeric,
            ["int8"] = DbColumnType.Numeric,
            ["integer"] = DbColumnType.Numeric,
            ["mediumint"] = DbColumnType.Numeric,
            ["money"] = DbColumnType.Numeric,
            ["number"] = DbColumnType.Numeric,
            ["numeric"] = DbColumnType.Numeric,
            ["real"] = DbColumnType.Numeric,
            ["smallint"] = DbColumnType.Numeric,
            ["smallmoney"] = DbColumnType.Numeric,
            ["tinyint"] = DbColumnType.Numeric,

            ["date"] = DbColumnType.Temporal,
            ["datetime"] = DbColumnType.Temporal,
            ["datetime2"] = DbColumnType.Temporal,
            ["datetimeoffset"] = DbColumnType.Temporal,
            ["interval"] = DbColumnType.Temporal,
            ["smalldatetime"] = DbColumnType.Temporal,
            ["time"] = DbColumnType.Temporal,
            ["time with time zone"] = DbColumnType.Temporal,
            ["time without time zone"] = DbColumnType.Temporal,
            ["timestamp with local time zone"] = DbColumnType.Temporal,
            ["timestamp with time zone"] = DbColumnType.Temporal,
            ["timestamp without time zone"] = DbColumnType.Temporal,
            ["timestamptz"] = DbColumnType.Temporal,
            ["timetz"] = DbColumnType.Temporal,
            ["year"] = DbColumnType.Temporal,

            ["bfile"] = DbColumnType.Binary,
            ["binary"] = DbColumnType.Binary,
            ["blob"] = DbColumnType.Binary,
            ["bytea"] = DbColumnType.Binary,
            ["image"] = DbColumnType.Binary,
            ["longblob"] = DbColumnType.Binary,
            ["mediumblob"] = DbColumnType.Binary,
            ["raw"] = DbColumnType.Binary,
            ["rowversion"] = DbColumnType.Binary,
            ["tinyblob"] = DbColumnType.Binary,
            ["varbinary"] = DbColumnType.Binary,

            ["bool"] = DbColumnType.Boolean,
            ["boolean"] = DbColumnType.Boolean,
        };

    public static DbColumnType? Of(Dbms dbms, string dataType)
    {
        // Which engine wrote the name decides what it means, so without one there
        // is nothing to decide.
        if (dbms == Dbms.Unknown)
        {
            return null;
        }

        if (dbms == Dbms.Sqlite)
        {
            return Affinity(dataType);
        }

        string name = Bare(dataType);

        return Collides(name)
            ? PerEngine(dbms, name)
            : Shared.TryGetValue(name, out DbColumnType category) ? category : null;
    }

    private static bool Collides(string name) => name is "timestamp" or "bit";

    // T-SQL's timestamp is a row version rather than a time, and its bit is a truth
    // value where a MySQL BIT(n) or a PostgreSQL bit string is a bit vector —
    // comparing one of those is not the mistake this records, so it stays unknown.
    private static DbColumnType? PerEngine(Dbms dbms, string name) => name switch
    {
        "timestamp" => dbms == Dbms.SqlServer ? DbColumnType.Binary : DbColumnType.Temporal,
        "bit" => dbms == Dbms.SqlServer ? DbColumnType.Boolean : null,
        _ => null,
    };

    // Length, precision and scale are deliberately dropped: the category is the
    // whole fact, so varchar(50) and varchar(4000) are one thing.
    private static string Bare(string dataType)
    {
        string name = dataType.Trim();
        int paren = name.IndexOf('(');

        if (paren >= 0)
        {
            name = name.Substring(0, paren).TrimEnd();
        }

        return name.ToLowerInvariant();
    }

    // SQLite resolves a declared type to an affinity by substring, in this order.
    // The final "everything else is NUMERIC" rule is deliberately not applied: it
    // would call a DATETIME column numeric, and dates are conventionally stored as
    // text there, so the fact stays unknown instead.
    private static DbColumnType? Affinity(string dataType)
    {
        string name = dataType.ToUpperInvariant();

        if (name.IndexOf("INT", StringComparison.Ordinal) >= 0)
        {
            return DbColumnType.Numeric;
        }

        if (Contains(name, "CHAR") || Contains(name, "CLOB") || Contains(name, "TEXT"))
        {
            return DbColumnType.Text;
        }

        if (Contains(name, "BLOB") || name.Length == 0)
        {
            return DbColumnType.Binary;
        }

        return Contains(name, "REAL") || Contains(name, "FLOA") || Contains(name, "DOUB")
            ? DbColumnType.Numeric
            : null;
    }

    private static bool Contains(string name, string part) =>
        name.IndexOf(part, StringComparison.Ordinal) >= 0;
}
