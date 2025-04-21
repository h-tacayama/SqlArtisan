namespace InlineSqlSharp;

public sealed class SequenceCurrval : AbstractExpr
{
    private readonly Sequence _sequence;

    internal SequenceCurrval(Sequence sequence)
    {
        _sequence = sequence;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_sequence.Name)
        .Append(".")
        .Append(Keywords.CURRVAL);
}
