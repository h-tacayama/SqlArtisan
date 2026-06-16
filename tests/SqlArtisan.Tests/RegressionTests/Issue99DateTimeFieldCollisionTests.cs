using SqlArtisan;
using static SqlArtisan.Sql;

// Regression test for issue #99.
//
// The public Sql.Datepart(...) factory method once shared a simple name with the
// date/time-field enum. Under the README-recommended `using static
// SqlArtisan.Sql;`, writing any enum member in expression position bound the name
// to the imported method instead of the type, failing to compile with CS0119.
// The enum has since been renamed to DateTimeField, removing the collision.
//
// This file deliberately lives OUTSIDE the SqlArtisan namespace: the in-namespace
// FunctionTests cannot reproduce the bug because they can resolve the enum via the
// enclosing SqlArtisan namespace, which outranks `using static` imports. Here the
// enum is reachable only through the imports above — the exact real-world setup.
// If the collision is ever reintroduced, this file stops compiling.
namespace SqlArtisanRegressionTests;

public class Issue99DateTimeFieldCollisionTests
{
    [Fact]
    public void DateTimeFieldEnum_InExpressionPosition_WithStaticUsing_CompilesAndBuilds()
    {
        SqlStatement extract =
            Select(Extract(DateTimeField.Year, CurrentTimestamp)).Build();
        SqlStatement datepart =
            Select(Datepart(DateTimeField.Year, CurrentTimestamp)).Build();
        SqlStatement dateadd =
            Select(Dateadd(DateTimeField.Month, 1, CurrentTimestamp)).Build();

        Assert.Equal("SELECT EXTRACT(YEAR FROM CURRENT_TIMESTAMP)", extract.Text);
        Assert.Equal("SELECT DATEPART(YEAR, CURRENT_TIMESTAMP)", datepart.Text);
        Assert.Equal("SELECT DATEADD(MONTH, :0, CURRENT_TIMESTAMP)", dateadd.Text);
        Assert.Equal(1, dateadd.Parameters.Get<int>(":0"));
    }
}
