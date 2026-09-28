namespace SqlArtisan.Internal;

public sealed class AnalyticDenseRankFunction : AnalyticFunction
{
    internal AnalyticDenseRankFunction() { }

    private protected override string FunctionName => Keywords.DenseRank;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.DenseRank)
        .OpenParenthesis()
        .CloseParenthesis();
}
