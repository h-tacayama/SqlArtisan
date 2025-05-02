namespace SqlArtisan;

internal sealed class MinusOperator(bool all) : SqlPart
{
    private readonly bool _all = all;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Minus)
        .AppendIf(_all, $" {Keywords.All}");
}
