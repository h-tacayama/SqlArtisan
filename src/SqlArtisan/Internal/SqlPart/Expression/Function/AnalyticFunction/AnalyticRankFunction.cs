namespace SqlArtisan.Internal;

public sealed class AnalyticRankFunction : AnalyticFunction
{
    internal AnalyticRankFunction() { }

    private protected override string FunctionName => Keywords.Rank;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Rank)
        .OpenParenthesis()
        .CloseParenthesis();
}
