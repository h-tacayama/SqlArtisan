namespace InlineSqlSharp;

public sealed class SequenceCurrval(Sequence sequence) : AbstractExpr
{
    private readonly Sequence _sequence = sequence;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_sequence.Name)
        .Append(".")
        .Append(Keywords.CURRVAL);
}
