namespace SqlArtisan.Internal;

public sealed class AnalyticPercentRankFunction : AnalyticFunction
{
    internal AnalyticPercentRankFunction() { }

    private protected override string FunctionName => Keywords.PercentRank;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.PercentRank)
        .OpenParenthesis()
        .CloseParenthesis();
}
