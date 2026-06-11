namespace SqlArtisan.Internal;

public sealed class AnalyticRowNumberFunction : AnalyticFunction
{
    internal AnalyticRowNumberFunction() { }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.RowNumber)
        .OpenParenthesis()
        .CloseParenthesis();
}
