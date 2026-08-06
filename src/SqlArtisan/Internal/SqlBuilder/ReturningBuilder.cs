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
        CollectionGuard.ThrowIfEmpty(
            expressions,
            "RETURNING requires at least one expression.");

        SqlPart[] resolved = SelectItemResolver.Resolve(expressions);

        for (int i = 0; i < resolved.Length; i++)
        {
            if (resolved[i] is ExpressionAlias)
            {
                throw new ArgumentException(
                    "RETURNING requires plain column expressions; " +
                    "name INTO variables with Into(new OutputParameter(...)).");
            }
        }

        return new ReturningBuilder(inner, resolved);
    }

    public ISqlBuilder Into(params OutputParameter[] outputs)
    {
        CollectionGuard.ThrowIfEmpty(
            outputs,
            "INTO requires at least one output parameter.");

        if (outputs.Length != _expressions.Length)
        {
            throw new ArgumentException(
                "INTO requires one output parameter per RETURNING expression " +
                $"({_expressions.Length} expected, {outputs.Length} provided).");
        }

        _inner.AddPart(new ReturningIntoClause(_expressions, outputs));
        return (ISqlBuilder)_inner;
    }

    public SqlStatement Build() =>
        _inner.BuildWithPart(new ReturningClause(_expressions));

    public SqlStatement Build(Dbms dbms) =>
        _inner.BuildWithPart(new ReturningClause(_expressions), dbms);
}
