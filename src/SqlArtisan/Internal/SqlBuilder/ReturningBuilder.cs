namespace SqlArtisan.Internal;

internal sealed class ReturningBuilder : IReturningBuilder
{
    private readonly SqlBuilderBase _inner;
    private readonly SqlPart[] _expressions;

    private ReturningBuilder(SqlBuilderBase inner, SqlPart[] expressions)
    {
        _inner = inner;
        _expressions = expressions;
    }

    internal static ReturningBuilder Create(SqlBuilderBase inner, object[] expressions)
    {
        if (expressions.Length == 0)
        {
            throw new ArgumentException("At least one expression is required for Returning().");
        }

        SqlPart[] resolved = SelectItemResolver.Resolve(expressions);

        if (resolved.Any(p => p is ExpressionAlias))
        {
            throw new ArgumentException(
                "Use plain column expressions in Returning(). " +
                "To specify INTO variables, chain .Into(\"var1\", \"var2\").");
        }

        return new ReturningBuilder(inner, resolved);
    }

    public ISqlBuilder Into(params string[] variables)
    {
        if (variables.Length == 0)
        {
            throw new ArgumentException("At least one variable is required for Into().");
        }

        if (variables.Length != _expressions.Length)
        {
            throw new ArgumentException(
                $"Into() requires {_expressions.Length} variable(s) to match " +
                $"the {_expressions.Length} expression(s) in Returning(), but {variables.Length} were provided.");
        }

        _inner.AddPart(new ReturningIntoClause(_expressions, variables));
        return (ISqlBuilder)_inner;
    }

    public SqlStatement Build() =>
        _inner.BuildWithPart(new ReturningClause(_expressions));

    public SqlStatement Build(Dbms dbms) =>
        _inner.BuildWithPart(new ReturningClause(_expressions), dbms);
}
