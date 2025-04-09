namespace InlineSqlSharp;

public sealed class AllOrDistinct : ISqlElement
{
    private bool _isDistinct;

    public bool IsDistinct => _isDistinct;

    public static AllOrDistinct All => new(false);

    public static AllOrDistinct Distinct => new(true);

    public void FormatSql(SqlBuildingBuffer buffer) =>
        buffer.AppendIf(_isDistinct, Keywords.DISTINCT);

    private AllOrDistinct(bool isDistinct) => _isDistinct = isDistinct;
}
