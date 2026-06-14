// The real function node classes, given the ③ tag. Identical in shape to the
// real AbsFunction etc., plus the Dbms passed to the base ctor.

namespace SqlArtisan.Proof;

// Universal: exists on every DBMS, same spelling. Generated into every facade.
public sealed class AbsFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    internal AbsFunction(SqlExpression expr, Dbms dbms) : base(dbms) => _expr = expr;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append("ABS").OpenParenthesis().Append(_expr).CloseParenthesis();
}

// Diverging: CEIL exists on Oracle/PostgreSql/MySql/Sqlite — NOT SQL Server.
public sealed class CeilFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    internal CeilFunction(SqlExpression expr, Dbms dbms) : base(dbms) => _expr = expr;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append("CEIL").OpenParenthesis().Append(_expr).CloseParenthesis();
}

// Diverging: CEILING exists on SQL Server/PostgreSql/MySql/Sqlite — NOT Oracle.
public sealed class CeilingFunction : SqlExpression
{
    private readonly SqlExpression _expr;
    internal CeilingFunction(SqlExpression expr, Dbms dbms) : base(dbms) => _expr = expr;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append("CEILING").OpenParenthesis().Append(_expr).CloseParenthesis();
}

internal static class ExpressionResolver
{
    // Mirror of the real ExpressionResolver.Resolve(object): pass SqlExpressions
    // through; treat a bare string as a column reference for this proof.
    internal static SqlExpression Resolve(object value) => value switch
    {
        SqlExpression expr => expr,
        string name => new Column(name),
        _ => throw new NotSupportedException(value?.GetType().Name),
    };
}
