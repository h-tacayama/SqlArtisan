namespace SqlArtisan.Internal;

/// <summary>
/// PostgreSQL's <c>DISTINCT ON (expr, ...)</c> select prefix: keeps one row per
/// distinct combination of the given expressions (the first per the
/// <c>ORDER BY</c>). Pass it to <c>Select(DistinctOn(...), ...)</c>.
/// </summary>
/// <remarks>
/// PostgreSQL-only syntax; emitted faithfully on every dialect (ADR 0001/0003).
/// Unlike <see cref="DistinctKeyword"/> it is valid only as a select prefix, never
/// inside an aggregate, so it is a separate type.
/// </remarks>
public sealed class DistinctOnKeyword : SqlPart
{
    private readonly SqlExpression[] _expressions;

    internal DistinctOnKeyword(SqlExpression[] expressions)
    {
        if (expressions.Length == 0)
        {
            throw new ArgumentException(
                "DISTINCT ON requires at least one expression.");
        }

        _expressions = expressions;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Distinct} {Keywords.On} ")
        .OpenParenthesis()
        .AppendCsv(_expressions)
        .CloseParenthesis();
}
