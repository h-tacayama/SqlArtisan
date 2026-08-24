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

    // Single-use guard, mirroring SqlBuilderBase: Into() hands the chain back to
    // the inner builder, so a later call on this held stage would append a
    // second RETURNING clause (#245's silent-contamination class).
    private bool _completed;

    public SqlStatement Build()
    {
        ThrowIfCompleted();
        _completed = true;
        return _inner.BuildWithPart(new ReturningClause(_expressions));
    }

    public SqlStatement Build(Dbms dbms)
    {
        ThrowIfCompleted();
        _completed = true;
        return _inner.BuildWithPart(new ReturningClause(_expressions), dbms);
    }

    public ISqlBuilder Into(params OutputParameter[] outputs)
    {
        ThrowIfCompleted();
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
        _completed = true;
        return (ISqlBuilder)_inner;
    }

    private void ThrowIfCompleted()
    {
        if (_completed)
        {
            throw new ArgumentException(
                "This RETURNING clause was already built; start a new chain.");
        }
    }
}
