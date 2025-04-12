namespace InlineSqlSharp;

public sealed class AllOrDistinct : ISqlElement
{
    private AllOrDistinct(bool isDistinct)
    {
        IsDistinct = isDistinct;
    }

    public bool IsDistinct { get; }

    public static AllOrDistinct All => new(false);

    public static AllOrDistinct Distinct => new(true);

    public void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.AppendIf(IsDistinct, Keywords.DISTINCT);
}
