namespace SqlArtisan;

internal sealed class ExceptOperator(bool all) : SqlPart
{
    private readonly bool _all = all;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Except)
        .AppendIf(_all, $" {Keywords.All}");
}
