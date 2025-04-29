namespace SqlArtisan;

internal sealed class ExceptOperator(bool all) : AbstractSqlPart
{
    private readonly bool _all = all;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Except)
        .AppendIf(_all, $" {Keywords.All}");
}
