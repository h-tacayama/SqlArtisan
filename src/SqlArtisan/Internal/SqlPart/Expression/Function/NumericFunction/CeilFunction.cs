namespace SqlArtisan.Internal;

public sealed class CeilFunction : SqlExpression
{
    private readonly SqlExpression _expr;

    // Always authored for a specific DBMS: CEIL is the Oracle/PostgreSql/MySql/
    // Sqlite spelling. SQL Server uses CEILING (see CeilingFunction).
    internal CeilFunction(SqlExpression expr, Dbms dbms) : base(dbms)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append("CEIL")
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
