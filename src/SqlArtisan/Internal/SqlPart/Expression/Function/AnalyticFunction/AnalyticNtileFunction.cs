namespace SqlArtisan.Internal;

public sealed class AnalyticNtileFunction : AnalyticFunction
{
    private readonly string _buckets;

    internal AnalyticNtileFunction(int buckets)
    {
        _buckets = WindowFrameGuard.ValidateNtileBuckets(buckets).ToInvariantString();
    }

    private protected override string FunctionName => Keywords.Ntile;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Ntile)
        .OpenParenthesis()
        .Append(_buckets)
        .CloseParenthesis();
}
