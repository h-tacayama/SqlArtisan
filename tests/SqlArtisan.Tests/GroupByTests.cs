using System.Text;
using static SqlArtisan.Sql;

namespace SqlArtisan.Tests;

public class GroupByTests
{
    private readonly TestTable _t = new("t");

    [Fact]
    public void GroupBy_SingleColumn_CorrectSql()
    {
        SqlStatement sql =
            Select(_t.Name)
            .From(_t)
            .GroupBy(_t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("\"t\".name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void GroupBy_MultipleColumns_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(
                _t.Code,
                _t.Name)
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Rollup_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(Rollup(_t.Code, _t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("ROLLUP(\"t\".code, \"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Rollup_MySql_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(Rollup(_t.Code, _t.Name))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.code, ");
        expected.Append("`t`.name ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("GROUP BY ");
        expected.Append("`t`.code, `t`.name WITH ROLLUP");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Rollup_MySql_SingleColumnGroup_CorrectSql()
    {
        // A single-column Group renders bare, so the MySQL suffix form accepts it.
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(Rollup(Group(_t.Code), _t.Name))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.code, ");
        expected.Append("`t`.name ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("GROUP BY ");
        expected.Append("`t`.code, `t`.name WITH ROLLUP");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Rollup_MySql_CompositeGroup_CorrectSql()
    {
        // The builder emits faithfully (ADR 0001); whether MySQL accepts a
        // composite grouping element in its WITH ROLLUP suffix form is the
        // author's responsibility and the analyzer's concern (ADR 0003), not a
        // Build-time check.
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(Rollup(Group(_t.Code, _t.Name), _t.CreatedAt))
            .Build(Dbms.MySql);

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("`t`.code, ");
        expected.Append("`t`.name ");
        expected.Append("FROM ");
        expected.Append("test_table `t` ");
        expected.Append("GROUP BY ");
        expected.Append("(`t`.code, `t`.name), `t`.created_at WITH ROLLUP");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Rollup_Sqlite_ThrowsNotSupportedException()
    {
        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            Select(_t.Code)
            .From(_t)
            .GroupBy(Rollup(_t.Code))
            .Build(Dbms.Sqlite));
    }

    [Fact]
    public void Cube_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(Cube(_t.Code, _t.Name))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("CUBE(\"t\".code, \"t\".name)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Cube_MySql_ThrowsNotSupportedException()
    {
        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            Select(_t.Code)
            .From(_t)
            .GroupBy(Cube(_t.Code, _t.Name))
            .Build(Dbms.MySql));
    }

    [Fact]
    public void Cube_Sqlite_ThrowsNotSupportedException()
    {
        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            Select(_t.Code)
            .From(_t)
            .GroupBy(Cube(_t.Code, _t.Name))
            .Build(Dbms.Sqlite));
    }

    [Fact]
    public void GroupingSets_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(GroupingSets(
                Group(_t.Code),
                Group(_t.Name),
                Group()))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("GROUPING SETS(\"t\".code, \"t\".name, ())");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void GroupingSets_CompositeSet_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(GroupingSets(
                Group(_t.Code, _t.Name),
                Group(_t.CreatedAt),
                Group()))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("GROUPING SETS((\"t\".code, \"t\".name), \"t\".created_at, ())");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Rollup_CompositeGroup_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(Rollup(Group(_t.Code, _t.Name), _t.CreatedAt))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("ROLLUP((\"t\".code, \"t\".name), \"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void Cube_CompositeGroup_CorrectSql()
    {
        SqlStatement sql =
            Select(
                _t.Code,
                _t.Name)
            .From(_t)
            .GroupBy(Cube(Group(_t.Code, _t.Name), _t.CreatedAt))
            .Build();

        StringBuilder expected = new();
        expected.Append("SELECT ");
        expected.Append("\"t\".code, ");
        expected.Append("\"t\".name ");
        expected.Append("FROM ");
        expected.Append("test_table \"t\" ");
        expected.Append("GROUP BY ");
        expected.Append("CUBE((\"t\".code, \"t\".name), \"t\".created_at)");

        Assert.Equal(expected.ToString(), sql.Text);
    }

    [Fact]
    public void GroupingSets_MySql_ThrowsNotSupportedException()
    {
        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            Select(_t.Code)
            .From(_t)
            .GroupBy(GroupingSets(Group(_t.Code), Group(_t.Name)))
            .Build(Dbms.MySql));
    }

    [Fact]
    public void GroupingSets_Sqlite_ThrowsNotSupportedException()
    {
        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            Select(_t.Code)
            .From(_t)
            .GroupBy(GroupingSets(Group(_t.Code), Group(_t.Name)))
            .Build(Dbms.Sqlite));
    }

    [Fact]
    public void GroupBy_WithNoItems_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Select(_t.Code).From(_t).GroupBy());
    }

    [Fact]
    public void GroupBy_WithNullItems_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            Select(_t.Code).From(_t).GroupBy(null!));
    }
}
