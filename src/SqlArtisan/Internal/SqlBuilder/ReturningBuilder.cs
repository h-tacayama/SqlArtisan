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

        for (int i = 0; i < resolved.Length; i++)
        {
            if (resolved[i] is ExpressionAlias)
            {
                throw new ArgumentException(
                    "Use plain column expressions in Returning(). " +
                    "To specify INTO variables, chain .Into(\"var1\", \"var2\").");
            }
        }

        return new ReturningBuilder(inner, resolved);
    }

    public ISqlBuilder Into(params OutputParameter[] outputs)
    {
        if (outputs.Length == 0)
        {
            throw new ArgumentException("At least one output parameter is required for Into().");
        }

        if (outputs.Length != _expressions.Length)
        {
            throw new ArgumentException(
                $"Into() requires {_expressions.Length} output parameter(s) to match " +
                $"the {_expressions.Length} expression(s) in Returning(), but {outputs.Length} were provided.");
        }

        _inner.AddPart(new ReturningIntoClause(_expressions, outputs));
        return (ISqlBuilder)_inner;
    }

    public SqlStatement Build() =>
        _inner.BuildWithPart(new ReturningClause(_expressions));

    public SqlStatement Build(Dbms dbms) =>
        _inner.BuildWithPart(new ReturningClause(_expressions), dbms);
}
