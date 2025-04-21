namespace InlineSqlSharp;

public sealed class AllOrDistinct : AbstractSqlPart
{
    internal AllOrDistinct(bool isDistinct)
    {
        IsDistinct = isDistinct;
    }

    internal bool IsDistinct { get; }

    internal static AllOrDistinct All => new(false);

    internal static AllOrDistinct Distinct => new(true);

    internal override void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.AppendIf(IsDistinct, Keywords.DISTINCT);
}
