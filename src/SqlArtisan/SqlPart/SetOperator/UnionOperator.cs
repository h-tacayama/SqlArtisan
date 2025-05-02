namespace SqlArtisan;

internal sealed class UnionOperator(bool all) : SqlPart
{
    private readonly bool _all = all;

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Union)
        .AppendIf(_all, $" {Keywords.All}");
}
