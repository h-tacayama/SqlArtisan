using SqlArtisan.TableClassGen;

namespace SqlArtisan.TableClassGen.Tests;

/// <summary>
/// The catalog type name reduces to one coarse category here, so the analyzer
/// never needs a five-dialect type table. A name no engine table claims stays
/// unknown, which reads as silence rather than as a guess.
/// </summary>
public class ColumnCategoryTests
{
    [Theory]
    [InlineData(Dbms.MySql, "varchar", "text")]
    [InlineData(Dbms.MySql, "longtext", "text")]
    [InlineData(Dbms.MySql, "mediumint", "numeric")]
    [InlineData(Dbms.MySql, "year", "temporal")]
    [InlineData(Dbms.MySql, "longblob", "binary")]
    [InlineData(Dbms.Oracle, "VARCHAR2", "text")]
    [InlineData(Dbms.Oracle, "NUMBER", "numeric")]
    [InlineData(Dbms.Oracle, "CLOB", "text")]
    [InlineData(Dbms.Oracle, "RAW", "binary")]
    [InlineData(Dbms.PostgreSql, "character varying", "text")]
    [InlineData(Dbms.PostgreSql, "double precision", "numeric")]
    [InlineData(Dbms.PostgreSql, "timestamp without time zone", "temporal")]
    [InlineData(Dbms.PostgreSql, "bytea", "binary")]
    [InlineData(Dbms.PostgreSql, "boolean", "boolean")]
    [InlineData(Dbms.SqlServer, "nvarchar", "text")]
    [InlineData(Dbms.SqlServer, "smallmoney", "numeric")]
    [InlineData(Dbms.SqlServer, "datetime2", "temporal")]
    [InlineData(Dbms.SqlServer, "image", "binary")]
    public void Of_RecognizedTypeName_ReturnsCategory(Dbms dbms, string dataType, string expected) =>
        Assert.Equal(expected, ColumnCategory.Of(dbms, dataType));

    // T-SQL's timestamp is a row version, not a time — the one name whose category
    // flips between engines rather than merely being absent from some.
    [Theory]
    [InlineData(Dbms.MySql, "temporal")]
    [InlineData(Dbms.Oracle, "temporal")]
    [InlineData(Dbms.PostgreSql, "temporal")]
    [InlineData(Dbms.SqlServer, "binary")]
    public void Of_Timestamp_ReturnsCategoryOfTheReadingEngine(Dbms dbms, string expected) =>
        Assert.Equal(expected, ColumnCategory.Of(dbms, "timestamp"));

    [Fact]
    public void Of_SqlServer_Bit_ReturnsBoolean() =>
        Assert.Equal("boolean", ColumnCategory.Of(Dbms.SqlServer, "bit"));

    // Elsewhere a bit is a bit vector, and comparing one is not this mistake.
    [Theory]
    [InlineData(Dbms.MySql)]
    [InlineData(Dbms.PostgreSql)]
    public void Of_Bit_OutsideSqlServer_ReturnsUnknown(Dbms dbms) =>
        Assert.Null(ColumnCategory.Of(dbms, "bit"));

    [Theory]
    [InlineData("varchar(50)", "text")]
    [InlineData("NUMBER(10,2)", "numeric")]
    [InlineData("TIMESTAMP(6) WITH TIME ZONE", "temporal")]
    public void Of_TypeNameCarryingPrecision_IgnoresIt(string dataType, string expected) =>
        Assert.Equal(expected, ColumnCategory.Of(Dbms.Oracle, dataType));

    [Theory]
    [InlineData("INTEGER", "numeric")]
    [InlineData("BIGINT", "numeric")]
    [InlineData("VARCHAR(50)", "text")]
    [InlineData("CLOB", "text")]
    [InlineData("BLOB", "binary")]
    [InlineData("", "binary")]
    [InlineData("REAL", "numeric")]
    [InlineData("DOUBLE", "numeric")]
    public void Of_Sqlite_DeclaredType_ReturnsAffinityCategory(string dataType, string expected) =>
        Assert.Equal(expected, ColumnCategory.Of(Dbms.Sqlite, dataType));

    // SQLite's last affinity rule sweeps every unmatched name into NUMERIC, which
    // would call a DATETIME column numeric while the value in it is conventionally
    // text. Left unknown instead, so the rule stays silent there.
    [Theory]
    [InlineData("DATETIME")]
    [InlineData("BOOLEAN")]
    [InlineData("DECIMAL(10,2)")]
    public void Of_Sqlite_TypeFallingToNumericAffinity_ReturnsUnknown(string dataType) =>
        Assert.Null(ColumnCategory.Of(Dbms.Sqlite, dataType));

    [Fact]
    public void Of_UnrecognizedTypeName_ReturnsUnknown() =>
        Assert.Null(ColumnCategory.Of(Dbms.PostgreSql, "geography"));

    // Which engine wrote the name decides what it means.
    [Fact]
    public void Of_UnknownDbms_ReturnsUnknown() =>
        Assert.Null(ColumnCategory.Of(Dbms.Unknown, "varchar"));
}
