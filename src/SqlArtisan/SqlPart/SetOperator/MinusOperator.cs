namespace SqlArtisan;

internal sealed class MinusOperator(bool all) : AbstractSqlPart
{
    private readonly bool _all = all;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Minus)
        .AppendIf(_all, $" {Keywords.All}");
}
