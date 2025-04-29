namespace SqlArtisan;

public sealed class GreatestFunction : AbstractExpr
{
    private readonly VariadicFunctionCore _core;

    internal GreatestFunction(AbstractExpr[] expressions)
    {
        _core = new(Keywords.Greatest, expressions);
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        _core.FormatSql(buffer);
}
