namespace SqlArtisan.Internal;

public sealed class AnalyticPercentRankFunction : AnalyticFunction
{
    internal AnalyticPercentRankFunction() { }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.PercentRank)
        .OpenParenthesis()
        .CloseParenthesis();
}
