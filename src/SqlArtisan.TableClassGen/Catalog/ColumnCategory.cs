using System;
using System.Collections.Generic;

namespace SqlArtisan.TableClassGen;

// The engine-specific knowledge stays here: the analyzer only ever sees the
// category, so neither side needs a five-dialect type table.
internal static class ColumnCategory
{
    public const string Text = "text";

    public const string Numeric = "numeric";

    public const string Temporal = "temporal";

    public const string Binary = "binary";

    public const string Boolean = "boolean";

    // Names that mean the same thing on every engine that has them. The ones that
    // do not are resolved by Collides before this is consulted.
    private static readonly IReadOnlyDictionary<string, string> Shared =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["char"] = Text,
            ["char varying"] = Text,
            ["character"] = Text,
            ["character varying"] = Text,
            ["clob"] = Text,
            ["long"] = Text,
            ["longtext"] = Text,
            ["mediumtext"] = Text,
            ["nchar"] = Text,
            ["nclob"] = Text,
            ["ntext"] = Text,
            ["nvarchar"] = Text,
            ["nvarchar2"] = Text,
            ["text"] = Text,
            ["tinytext"] = Text,
            ["varchar"] = Text,
            ["varchar2"] = Text,

            ["bigint"] = Numeric,
            ["binary_double"] = Numeric,
            ["binary_float"] = Numeric,
            ["dec"] = Numeric,
            ["decimal"] = Numeric,
            ["double"] = Numeric,
            ["double precision"] = Numeric,
            ["fixed"] = Numeric,
            ["float"] = Numeric,
            ["float4"] = Numeric,
            ["float8"] = Numeric,
            ["int"] = Numeric,
            ["int2"] = Numeric,
            ["int4"] = Numeric,
            ["int8"] = Numeric,
            ["integer"] = Numeric,
            ["mediumint"] = Numeric,
            ["money"] = Numeric,
            ["number"] = Numeric,
            ["numeric"] = Numeric,
            ["real"] = Numeric,
            ["smallint"] = Numeric,
            ["smallmoney"] = Numeric,
            ["tinyint"] = Numeric,

            ["date"] = Temporal,
            ["datetime"] = Temporal,
            ["datetime2"] = Temporal,
            ["datetimeoffset"] = Temporal,
            ["interval"] = Temporal,
            ["smalldatetime"] = Temporal,
            ["time"] = Temporal,
            ["time with time zone"] = Temporal,
            ["time without time zone"] = Temporal,
            ["timestamp with local time zone"] = Temporal,
            ["timestamp with time zone"] = Temporal,
            ["timestamp without time zone"] = Temporal,
            ["timestamptz"] = Temporal,
            ["timetz"] = Temporal,
            ["year"] = Temporal,

            ["bfile"] = Binary,
            ["binary"] = Binary,
            ["blob"] = Binary,
            ["bytea"] = Binary,
            ["image"] = Binary,
            ["longblob"] = Binary,
            ["mediumblob"] = Binary,
            ["raw"] = Binary,
            ["rowversion"] = Binary,
            ["tinyblob"] = Binary,
            ["varbinary"] = Binary,

            ["bool"] = Boolean,
            ["boolean"] = Boolean,
        };

    public static string? Of(Dbms dbms, string dataType)
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
            : Shared.TryGetValue(name, out string? category) ? category : null;
    }

    private static bool Collides(string name) => name is "timestamp" or "bit";

    // T-SQL's timestamp is a row version rather than a time, and its bit is a truth
    // value where a MySQL BIT(n) or a PostgreSQL bit string is a bit vector —
    // comparing one of those is not the mistake this records, so it stays unknown.
    private static string? PerEngine(Dbms dbms, string name) => name switch
    {
        "timestamp" => dbms == Dbms.SqlServer ? Binary : Temporal,
        "bit" => dbms == Dbms.SqlServer ? Boolean : null,
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
    private static string? Affinity(string dataType)
    {
        string name = dataType.ToUpperInvariant();

        if (name.IndexOf("INT", StringComparison.Ordinal) >= 0)
        {
            return Numeric;
        }

        if (Contains(name, "CHAR") || Contains(name, "CLOB") || Contains(name, "TEXT"))
        {
            return Text;
        }

        if (Contains(name, "BLOB") || name.Length == 0)
        {
            return Binary;
        }

        return Contains(name, "REAL") || Contains(name, "FLOA") || Contains(name, "DOUB")
            ? Numeric
            : null;
    }

    private static bool Contains(string name, string part) =>
        name.IndexOf(part, StringComparison.Ordinal) >= 0;
}
