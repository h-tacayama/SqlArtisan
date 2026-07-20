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

    [Fact]
    public void Merge_OnAllConditionsExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MergeInto(_t)
            .Using(_s)
            .On(ConditionIf(false, _t.Code == _s.Code))
            .WhenMatched().ThenUpdateSet(_t.Name == _s.Name)
            .Build(Dbms.Oracle));

        Assert.Equal("A MERGE ON clause requires a condition.", ex.Message);
    }

    [Fact]
    public void Merge_WhenMatchedConditionExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenMatched(ConditionIf(false, _s.Name == "x")).ThenUpdateSet(_t.Name == _s.Name)
            .Build(Dbms.Oracle));

        Assert.Equal("A MERGE WHEN MATCHED AND clause requires a condition.", ex.Message);
    }

    [Fact]
    public void Merge_WhenNotMatchedConditionExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenNotMatched(ConditionIf(false, _s.Name == "x"))
                .ThenInsert(_cols.Code).Values(_s.Code)
            .Build(Dbms.Oracle));

        Assert.Equal("A MERGE WHEN NOT MATCHED AND clause requires a condition.", ex.Message);
    }

    [Fact]
    public void Merge_WhenNotMatchedBySourceConditionExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenNotMatchedBySource(ConditionIf(false, _t.Name == "x")).ThenDelete()
            .Build(Dbms.SqlServer));

        Assert.Equal(
            "A MERGE WHEN NOT MATCHED BY SOURCE AND clause requires a condition.",
            ex.Message);
    }

    [Fact]
    public void Merge_DeleteWhereAllConditionsExcluded_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MergeInto(_t)
            .Using(_s)
            .On(_t.Code == _s.Code)
            .WhenMatched().ThenUpdateSet(_t.Name == _s.Name)
            .DeleteWhere(ConditionIf(false, _s.Name == "x"))
            .Build(Dbms.Oracle));

        Assert.Equal("A MERGE DELETE WHERE clause requires a condition.", ex.Message);
    }

    [Fact]
    public void MergeUsingSubquery_Oracle_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("MERGE INTO test_table \"t\" ");
        expected.Append("USING (SELECT \"s\".code, \"s\".name FROM test_table \"s\") \"q\" ");
        expected.Append("ON (\"t\".code = \"q\".code) ");
        expected.Append("WHEN MATCHED THEN UPDATE SET \"t\".name = \"q\".name");

        SubqueryDerivedTable q = Select(_s.Code, _s.Name).From(_s).AsTable("q");
        SqlStatement sql =
            MergeInto(_t)
            .Using(q)
            .On(_t.Code == q.Column("code"))
            .WhenMatched().ThenUpdateSet(_t.Name == q.Column("name"))
            .Build(Dbms.Oracle);

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void MergeUsingSubquery_SqlServer_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("MERGE INTO test_table \"t\" ");
        expected.Append("USING (SELECT \"s\".code, \"s\".name FROM test_table \"s\") \"q\" ");
        expected.Append("ON (\"t\".code = \"q\".code) ");
        expected.Append("WHEN MATCHED THEN UPDATE SET \"t\".name = \"q\".name;");

        SubqueryDerivedTable q = Select(_s.Code, _s.Name).From(_s).AsTable("q");
        SqlStatement sql =
            MergeInto(_t)
            .Using(q)
            .On(_t.Code == q.Column("code"))
            .WhenMatched().ThenUpdateSet(_t.Name == q.Column("name"))
            .Build(Dbms.SqlServer);

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void MergeUsingValues_SqlServer_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("MERGE INTO test_table \"t\" ");
        expected.Append("USING (VALUES (@0, @1), (@2, @3)) \"s\" (code, name) ");
        expected.Append("ON (\"t\".code = \"s\".code) ");
        expected.Append("WHEN MATCHED THEN UPDATE SET \"t\".name = \"s\".name ");
        expected.Append("WHEN NOT MATCHED THEN INSERT (code, name) ");
        expected.Append("VALUES (\"s\".code, \"s\".name);");

        ValuesDerivedTable s = Values("s", ["code", "name"], [[1, "Ann"], [2, "Bo"]]);
        SqlStatement sql =
            MergeInto(_t)
            .Using(s)
            .On(_t.Code == s.Column("code"))
            .WhenMatched().ThenUpdateSet(_t.Name == s.Column("name"))
            .WhenNotMatched().ThenInsert(_cols.Code, _cols.Name)
                .Values(s.Column("code"), s.Column("name"))
            .Build(Dbms.SqlServer);

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(4, sql.Parameters.Count);
    }

    [Fact]
    public void MergeUsingValues_PostgreSql_CorrectSql()
    {
        StringBuilder expected = new();
        expected.Append("MERGE INTO test_table \"t\" ");
        expected.Append("USING (VALUES (:0, :1), (:2, :3)) \"s\" (code, name) ");
        expected.Append("ON (\"t\".code = \"s\".code) ");
        expected.Append("WHEN MATCHED THEN UPDATE SET \"t\".name = \"s\".name ");
        expected.Append("WHEN NOT MATCHED THEN INSERT (code, name) ");
        expected.Append("VALUES (\"s\".code, \"s\".name)");

        ValuesDerivedTable s = Values("s", ["code", "name"], [[1, "Ann"], [2, "Bo"]]);
        SqlStatement sql =
            MergeInto(_t)
            .Using(s)
            .On(_t.Code == s.Column("code"))
            .WhenMatched().ThenUpdateSet(_t.Name == s.Column("name"))
            .WhenNotMatched().ThenInsert(_cols.Code, _cols.Name)
                .Values(s.Column("code"), s.Column("name"))
            .Build(Dbms.PostgreSql);

        Assert.Equal(expected.ToString(), sql.Text);
        Assert.Equal(4, sql.Parameters.Count);
    }

    [Fact]
    public void Values_NoRows_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Values("s", ["code"], []));

        Assert.Equal("A VALUES source requires at least one row.", ex.Message);
    }

    [Fact]
    public void Values_NoColumns_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Values("s", [], [[1]]));

        Assert.Equal("A VALUES source requires at least one column.", ex.Message);
    }

    [Fact]
    public void Values_RowWidthMismatch_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Values("s", ["code", "name"], [[1]]));

        Assert.Equal(
            "Every row of a VALUES source must supply one value per column; "
                + "the column list has 2, but a row has 1.",
            ex.Message);
    }
}
