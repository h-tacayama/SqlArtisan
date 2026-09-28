namespace SqlArtisan.Internal;

public sealed class AnalyticCumeDistFunction : AnalyticFunction
{
    internal AnalyticCumeDistFunction() { }

    private protected override string FunctionName => Keywords.CumeDist;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.CumeDist)
        .OpenParenthesis()
        .CloseParenthesis();
}
