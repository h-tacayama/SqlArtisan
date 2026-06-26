namespace SqlArtisan.Internal;

public sealed class AnalyticRankFunction : AnalyticFunction
{
    internal AnalyticRankFunction() { }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Rank)
        .OpenParenthesis()
        .CloseParenthesis();
}
