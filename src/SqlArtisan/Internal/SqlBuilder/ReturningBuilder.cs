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
            expressions, nameof(expressions), "RETURNING requires at least one expression.");

        // An alias is valid in a RETURNING list (PostgreSQL, SQLite), so it is
        // emitted faithfully here (ADR 0007); only the INTO form rejects it.
        SqlPart[] resolved =
            SelectItemResolver.Resolve(expressions, "ReturningItem", "returningItem");
        // Marked only once nothing here can throw, so a rejected list leaves the
        // inner builder exactly as it was.
        inner.MarkReturningPending();
        return new ReturningBuilder(inner, resolved);
    }

    // Single-use guard, mirroring SqlBuilderBase: Into() hands the chain back to
    // the inner builder, so a later call on this held stage would append a
    // second RETURNING clause (#245's silent-contamination class).
    private bool _completed;

    public SqlStatement Build()
    {
        ThrowIfCompleted();
        // Freeze only after the delegated build survives its guards, mirroring
        // BuildCore: a failed Build leaves the stage usable for a fix-up retry.
        SqlStatement statement = _inner.BuildWithPart(new ReturningClause(_expressions));
        _completed = true;
        return statement;
    }

    public SqlStatement Build(Dbms dbms)
    {
        ThrowIfCompleted();
        SqlStatement statement = _inner.BuildWithPart(new ReturningClause(_expressions), dbms);
        _completed = true;
        return statement;
    }

    public ISqlBuilder Into(params OutputParameter[] outputs)
    {
        ThrowIfCompleted();
        CollectionGuard.ThrowIfEmpty(
            outputs, nameof(outputs), "INTO requires at least one output parameter.");

        if (outputs.Length != _expressions.Length)
        {
            throw new ArgumentException(
                "INTO requires one output parameter per RETURNING expression " +
                $"({_expressions.Length} expected, {outputs.Length} provided).");
        }

        for (int i = 0; i < _expressions.Length; i++)
        {
            if (_expressions[i] is ExpressionAlias)
            {
                throw new ArgumentException(
                    "RETURNING ... INTO takes no column alias; the output parameter "
                    + "names the value, so drop the .As(...) alias.");
            }
        }

        // Both facts are fixed here, and Into freezes this stage, so a Build()-time
        // throw would leave no stage to retry from; the buffer's checks are the backstop.
        for (int i = 0; i < outputs.Length; i++)
        {
            if (string.IsNullOrEmpty(outputs[i].Variable))
            {
                throw new ArgumentException("An output variable name is required.");
            }

            for (int j = 0; j < i; j++)
            {
                if (outputs[j].Variable == outputs[i].Variable)
                {
                    throw new ArgumentException(
                        "A RETURNING INTO clause requires a distinct name for every variable; "
                            + $"'{outputs[i].Variable}' is duplicated.");
                }
            }
        }

        _inner.AddPart(new ReturningIntoClause(_expressions, outputs));
        _inner.DischargeReturning();
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
