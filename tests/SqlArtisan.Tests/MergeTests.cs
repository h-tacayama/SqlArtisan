using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class MergeTests
{
    private readonly TestTable _t = new("t");
    private readonly TestTable _s = new("s");

    // Unaliased columns for the INSERT column list, which names target columns
    // and must not be alias-qualified.
    private readonly TestTable _cols = new();

    [Fact]
    public void Merge_Oracle_MatchedUpdate_NotMatchedInsert()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("MERGE INTO test_table \"t\" ");
        expected.Append("USING test_table \"s\" ");
        expected.Append("ON (\"t\".code = \"s\".code) ");
        expected.Append("WHEN MATCHED THEN UPDATE SET \"t\".name = \"s\".name ");
        expected.Append("WHEN NOT MATCHED THEN INSERT (code, name) ");
        expected.Append("VALUES (\"s\".code, \"s\".name)");

        // Act
        SqlStatement sql =
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenMatched().ThenUpdateSet(_t.Name == _s.Name)
            .WhenNotMatched().ThenInsert(_cols.Code, _cols.Name).Values(_s.Code, _s.Name)
            .Build(Dbms.Oracle);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Merge_Oracle_WhenMatchedWithExtraCondition()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("MERGE INTO test_table \"t\" ");
        expected.Append("USING test_table \"s\" ");
        expected.Append("ON (\"t\".code = \"s\".code) ");
        expected.Append("WHEN MATCHED AND \"s\".name = :0 THEN UPDATE SET \"t\".name = \"s\".name");

        // Act
        SqlStatement sql =
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenMatched(_s.Name == "active").ThenUpdateSet(_t.Name == _s.Name)
            .Build(Dbms.Oracle);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal("active", sql.Parameters.Get<string>(":0"));
    }

    [Fact]
    public void Merge_Oracle_MatchedUpdate_WithInClauseDeleteWhere()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("MERGE INTO test_table \"t\" ");
        expected.Append("USING test_table \"s\" ");
        expected.Append("ON (\"t\".code = \"s\".code) ");
        expected.Append("WHEN MATCHED THEN UPDATE SET \"t\".name = \"s\".name ");
        expected.Append("DELETE WHERE \"t\".name IS NULL");

        // Act
        SqlStatement sql =
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenMatched().ThenUpdateSet(_t.Name == _s.Name).DeleteWhere(_t.Name.IsNull)
            .Build(Dbms.Oracle);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Merge_SqlServer_AppendsTerminatingSemicolon()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("MERGE INTO test_table \"t\" ");
        expected.Append("USING test_table \"s\" ");
        expected.Append("ON (\"t\".code = \"s\".code) ");
        expected.Append("WHEN MATCHED THEN UPDATE SET \"t\".name = \"s\".name ");
        expected.Append("WHEN NOT MATCHED THEN INSERT (code, name) ");
        expected.Append("VALUES (\"s\".code, \"s\".name);");

        // Act
        SqlStatement sql =
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenMatched().ThenUpdateSet(_t.Name == _s.Name)
            .WhenNotMatched().ThenInsert(_cols.Code, _cols.Name).Values(_s.Code, _s.Name)
            .Build(Dbms.SqlServer);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Merge_SqlServer_WhenMatchedThenDelete()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("MERGE INTO test_table \"t\" ");
        expected.Append("USING test_table \"s\" ");
        expected.Append("ON (\"t\".code = \"s\".code) ");
        expected.Append("WHEN MATCHED THEN DELETE;");

        // Act
        SqlStatement sql =
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenMatched().ThenDelete()
            .Build(Dbms.SqlServer);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Merge_SqlServer_WhenNotMatchedBySource_ThenUpdate()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("MERGE INTO test_table \"t\" ");
        expected.Append("USING test_table \"s\" ");
        expected.Append("ON (\"t\".code = \"s\".code) ");
        expected.Append("WHEN NOT MATCHED BY SOURCE THEN UPDATE SET \"t\".name = @0;");

        // Act
        SqlStatement sql =
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenNotMatchedBySource().ThenUpdateSet(_t.Name == "archived")
            .Build(Dbms.SqlServer);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(1, sql.Parameters.Count);
        Assert.Equal("archived", sql.Parameters.Get<string>("@0"));
    }

    [Fact]
    public void Merge_SqlServer_WhenNotMatchedBySource_ThenDelete_WithCondition()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("MERGE INTO test_table \"t\" ");
        expected.Append("USING test_table \"s\" ");
        expected.Append("ON (\"t\".code = \"s\".code) ");
        expected.Append("WHEN NOT MATCHED BY SOURCE AND \"t\".name IS NULL THEN DELETE;");

        // Act
        SqlStatement sql =
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenNotMatchedBySource(_t.Name.IsNull).ThenDelete()
            .Build(Dbms.SqlServer);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Merge_PostgreSql_OmitsTerminatingSemicolon()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("MERGE INTO test_table \"t\" ");
        expected.Append("USING test_table \"s\" ");
        expected.Append("ON (\"t\".code = \"s\".code) ");
        expected.Append("WHEN MATCHED THEN UPDATE SET \"t\".name = \"s\".name ");
        expected.Append("WHEN NOT MATCHED THEN INSERT (code, name) ");
        expected.Append("VALUES (\"s\".code, \"s\".name)");

        // Act
        SqlStatement sql =
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenMatched().ThenUpdateSet(_t.Name == _s.Name)
            .WhenNotMatched().ThenInsert(_cols.Code, _cols.Name).Values(_s.Code, _s.Name)
            .Build(Dbms.PostgreSql);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Merge_NotMatchedInsert_LiteralValuesAreParameterized()
    {
        // Arrange
        StringBuilder expected = new();
        expected.Append("MERGE INTO test_table \"t\" ");
        expected.Append("USING test_table \"s\" ");
        expected.Append("ON (\"t\".code = \"s\".code) ");
        expected.Append("WHEN NOT MATCHED THEN INSERT (code, name) ");
        expected.Append("VALUES (:0, :1)");

        // Act
        SqlStatement sql =
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenNotMatched().ThenInsert(_cols.Code, _cols.Name).Values(7, "new-row")
            .Build(Dbms.Oracle);

        // Assert
        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(2, sql.Parameters.Count);
    }
}
