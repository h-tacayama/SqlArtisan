namespace InlineSqlSharp;

public sealed class SequenceCurrval(Sequence sequence) : NumericExpr
{
    private readonly Sequence _sequence = sequence;

    public override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_sequence.Name)
        .Append(".")
        .Append(Keywords.CURRVAL);

    public override void FormatAsSelect(SqlBuildingBuffer buffer) =>
        FormatSql(buffer);
}
