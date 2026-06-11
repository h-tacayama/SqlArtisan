namespace SqlArtisan.Internal;

public sealed class AnalyticCumeDistFunction : AnalyticFunction
{
    internal AnalyticCumeDistFunction() { }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.CumeDist)
        .OpenParenthesis()
        .CloseParenthesis();
}
