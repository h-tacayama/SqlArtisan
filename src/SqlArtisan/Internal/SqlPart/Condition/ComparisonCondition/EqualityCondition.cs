namespace SqlArtisan.Internal;

public sealed class EqualityCondition(
    SqlExpression leftSide,
    SqlExpression rightSide) : EqualityBasedCondition
{
    internal override SqlExpression LeftSide => leftSide;

    internal override SqlExpression RightSide => rightSide;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(LeftSide)
        .Append($" {Operators.Equality} ")
        .Append(RightSide);

    // Renders `column = value` for a DML assignment, forcing the target column
    // (left side) unqualified. PostgreSQL rejects an alias-qualified SET / DO
    // UPDATE SET target (e.g. `SET x.col = ...`); the right side keeps its
    // normal qualification so it can reference the alias or EXCLUDED.
    internal void FormatAsAssignment(SqlBuildingBuffer buffer)
    {
        if (LeftSide is DbColumn column)
        {
            column.FormatUnqualified(buffer);
        }
        else
        {
            LeftSide.Format(buffer);
        }

        buffer.Append($" {Operators.Equality} ").Append(RightSide);
    }
}
