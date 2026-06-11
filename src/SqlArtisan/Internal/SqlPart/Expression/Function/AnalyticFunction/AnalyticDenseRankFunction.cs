namespace SqlArtisan.Internal;

public sealed class AnalyticDenseRankFunction : AnalyticFunction
{
    internal AnalyticDenseRankFunction() { }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.DenseRank)
        .OpenParenthesis()
        .CloseParenthesis();
}
