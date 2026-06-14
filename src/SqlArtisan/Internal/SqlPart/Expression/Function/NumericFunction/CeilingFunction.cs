namespace SqlArtisan.Internal;

public sealed class CeilingFunction : SqlExpression
{
    private readonly SqlExpression _expr;

    // The SQL Server (and PostgreSql/MySql/Sqlite) spelling. Oracle uses CEIL
    // (see CeilFunction).
    internal CeilingFunction(SqlExpression expr, Dbms dbms) : base(dbms)
    {
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append("CEILING")
        .OpenParenthesis()
        .Append(_expr)
        .CloseParenthesis();
}
