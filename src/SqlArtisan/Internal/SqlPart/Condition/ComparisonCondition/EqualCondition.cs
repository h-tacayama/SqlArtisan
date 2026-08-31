namespace SqlArtisan.Internal;

internal sealed class EqualCondition(
    SqlExpression leftSide,
    SqlExpression rightSide) : EqualityCondition
{
    internal override SqlExpression LeftSide => leftSide;

    internal override SqlExpression RightSide => rightSide;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(LeftSide)
        .EncloseInSpaces(Operators.Equal)
        .Append(RightSide);

    // The target column stays unqualified (DbColumn.FormatUnqualified has the
    // why); the right side keeps its normal qualification.
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

        buffer.EncloseInSpaces(Operators.Equal).Append(RightSide);
    }
}
