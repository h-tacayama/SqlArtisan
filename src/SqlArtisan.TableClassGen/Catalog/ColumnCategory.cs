using System;
using System.Collections.Generic;

namespace SqlArtisan.TableClassGen;

// The engine-specific knowledge stays here: the analyzer only ever sees the
// category, so neither side needs a five-dialect type table.
internal static class ColumnCategory
{
    // Names that mean the same thing on every engine that has them. The ones that
    // do not are resolved by Collides before this is consulted.
    private static readonly IReadOnlyDictionary<string, DbTypeCategory> Shared =
        new Dictionary<string, DbTypeCategory>(StringComparer.Ordinal)
        {
            ["char"] = DbTypeCategory.Text,
            ["char varying"] = DbTypeCategory.Text,
            ["character"] = DbTypeCategory.Text,
            ["character varying"] = DbTypeCategory.Text,
            ["clob"] = DbTypeCategory.Text,
            ["long"] = DbTypeCategory.Text,
            ["longtext"] = DbTypeCategory.Text,
            ["mediumtext"] = DbTypeCategory.Text,
            ["nchar"] = DbTypeCategory.Text,
            ["nclob"] = DbTypeCategory.Text,
            ["ntext"] = DbTypeCategory.Text,
            ["nvarchar"] = DbTypeCategory.Text,
            ["nvarchar2"] = DbTypeCategory.Text,
            ["text"] = DbTypeCategory.Text,
            ["tinytext"] = DbTypeCategory.Text,
            ["varchar"] = DbTypeCategory.Text,
            ["varchar2"] = DbTypeCategory.Text,

            ["bigint"] = DbTypeCategory.Numeric,
            ["binary_double"] = DbTypeCategory.Numeric,
            ["binary_float"] = DbTypeCategory.Numeric,
            ["dec"] = DbTypeCategory.Numeric,
            ["decimal"] = DbTypeCategory.Numeric,
            ["double"] = DbTypeCategory.Numeric,
            ["double precision"] = DbTypeCategory.Numeric,
            ["fixed"] = DbTypeCategory.Numeric,
            ["float"] = DbTypeCategory.Numeric,
            ["float4"] = DbTypeCategory.Numeric,
            ["float8"] = DbTypeCategory.Numeric,
            ["int"] = DbTypeCategory.Numeric,
            ["int2"] = DbTypeCategory.Numeric,
            ["int4"] = DbTypeCategory.Numeric,
            ["int8"] = DbTypeCategory.Numeric,
            ["integer"] = DbTypeCategory.Numeric,
            ["mediumint"] = DbTypeCategory.Numeric,
            ["money"] = DbTypeCategory.Numeric,
            ["number"] = DbTypeCategory.Numeric,
            ["numeric"] = DbTypeCategory.Numeric,
            ["real"] = DbTypeCategory.Numeric,
            ["smallint"] = DbTypeCategory.Numeric,
            ["smallmoney"] = DbTypeCategory.Numeric,
            ["tinyint"] = DbTypeCategory.Numeric,

            ["date"] = DbTypeCategory.Temporal,
            ["datetime"] = DbTypeCategory.Temporal,
            ["datetime2"] = DbTypeCategory.Temporal,
            ["datetimeoffset"] = DbTypeCategory.Temporal,
            ["interval"] = DbTypeCategory.Temporal,
            ["smalldatetime"] = DbTypeCategory.Temporal,
            ["time"] = DbTypeCategory.Temporal,
            ["time with time zone"] = DbTypeCategory.Temporal,
            ["time without time zone"] = DbTypeCategory.Temporal,
            ["timestamp with local time zone"] = DbTypeCategory.Temporal,
            ["timestamp with time zone"] = DbTypeCategory.Temporal,
            ["timestamp without time zone"] = DbTypeCategory.Temporal,
            ["timestamptz"] = DbTypeCategory.Temporal,
            ["timetz"] = DbTypeCategory.Temporal,
            ["year"] = DbTypeCategory.Temporal,

            ["bfile"] = DbTypeCategory.Binary,
            ["binary"] = DbTypeCategory.Binary,
            ["blob"] = DbTypeCategory.Binary,
            ["bytea"] = DbTypeCategory.Binary,
            ["image"] = DbTypeCategory.Binary,
            ["longblob"] = DbTypeCategory.Binary,
            ["mediumblob"] = DbTypeCategory.Binary,
            ["raw"] = DbTypeCategory.Binary,
            ["rowversion"] = DbTypeCategory.Binary,
            ["tinyblob"] = DbTypeCategory.Binary,
            ["varbinary"] = DbTypeCategory.Binary,

            ["bool"] = DbTypeCategory.Boolean,
            ["boolean"] = DbTypeCategory.Boolean,
        };

    public static DbTypeCategory? Of(Dbms dbms, string dataType)
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
            : Shared.TryGetValue(name, out DbTypeCategory category) ? category : null;
    }

    private static bool Collides(string name) => name is "timestamp" or "bit";

    // T-SQL's timestamp is a row version rather than a time, and its bit is a truth
    // value where a MySQL BIT(n) or a PostgreSQL bit string is a bit vector —
    // comparing one of those is not the mistake this records, so it stays unknown.
    private static DbTypeCategory? PerEngine(Dbms dbms, string name) => name switch
    {
        "timestamp" => dbms == Dbms.SqlServer ? DbTypeCategory.Binary : DbTypeCategory.Temporal,
        "bit" => dbms == Dbms.SqlServer ? DbTypeCategory.Boolean : null,
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
    private static DbTypeCategory? Affinity(string dataType)
    {
        string name = dataType.ToUpperInvariant();

        if (name.IndexOf("INT", StringComparison.Ordinal) >= 0)
        {
            return DbTypeCategory.Numeric;
        }

        if (Contains(name, "CHAR") || Contains(name, "CLOB") || Contains(name, "TEXT"))
        {
            return DbTypeCategory.Text;
        }

        if (Contains(name, "BLOB") || name.Length == 0)
        {
            return DbTypeCategory.Binary;
        }

        return Contains(name, "REAL") || Contains(name, "FLOA") || Contains(name, "DOUB")
            ? DbTypeCategory.Numeric
            : null;
    }

    private static bool Contains(string name, string part) =>
        name.IndexOf(part, StringComparison.Ordinal) >= 0;
}
