namespace SqlArtisan.Internal;

public sealed class AnalyticNtileFunction : AnalyticFunction
{
    private readonly int _buckets;

    internal AnalyticNtileFunction(int buckets)
    {
        _buckets = buckets;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Ntile)
        .OpenParenthesis()
        .Append(_buckets.ToInvariantString())
        .CloseParenthesis();
}
