namespace SqlArtisan.Internal;

public sealed class AnalyticRowNumberFunction : AnalyticFunction
{
    internal AnalyticRowNumberFunction() { }

    private protected override string FunctionName => Keywords.RowNumber;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.RowNumber)
        .OpenParenthesis()
        .CloseParenthesis();
}
