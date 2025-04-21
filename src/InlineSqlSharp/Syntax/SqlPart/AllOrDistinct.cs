namespace InlineSqlSharp;

public sealed class AllOrDistinct(bool isDistinct) : AbstractSqlPart
{
    internal bool IsDistinct => isDistinct;

    public static AllOrDistinct All => new(false);

    public static AllOrDistinct Distinct => new(true);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.AppendIf(IsDistinct, Keywords.DISTINCT);
}
