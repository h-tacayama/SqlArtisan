using SqlArtisan;
using static SqlArtisan.Sql;

// Compile-time guard for issue #99.
//
// The public Sql.Datepart(...) factory method once shared a simple name with the
// Datepart enum. Under the README-recommended `using static
// SqlArtisan.Sql;`, writing any enum member in expression position bound the name
// to the imported method instead of the type, failing to compile with CS0119.
// The enum has since been renamed to DateTimePart, removing the collision.
//
// This file deliberately lives OUTSIDE the SqlArtisan namespace: the in-namespace
// FunctionTests cannot reproduce the bug because they resolve the enum via the
// enclosing SqlArtisan namespace, which outranks `using static` imports. Here the
// enum is reachable only through the imports above — the exact real-world setup.
//
// Its value is that it COMPILES: using DateTimePart in expression position across
// every function that takes it proves the collision is gone. If the collision is
// ever reintroduced, this file stops compiling. The SQL each function emits is
// already covered exhaustively by FunctionTests.D/E, so no output is asserted here.
namespace SqlArtisanRegressionTests;

public class DateTimePartNameCollisionTests
{
    [Fact]
    public void DateTimePartEnum_InExpressionPosition_WithStaticUsing_Compiles()
    {
        SqlStatement[] statements =
        [
            Select(Extract(DateTimePart.Year, CurrentTimestamp)).Build(),
            Select(Datepart(DateTimePart.Year, CurrentTimestamp)).Build(),
            Select(Dateadd(DateTimePart.Month, 1, CurrentTimestamp)).Build(),
            Select(Datediff(DateTimePart.Day, CurrentTimestamp, CurrentTimestamp)).Build(),
            Select(DateTrunc(DateTimePart.Month, CurrentTimestamp)).Build(),
        ];

        Assert.All(statements, statement => Assert.False(string.IsNullOrEmpty(statement.Text)));
    }
}
