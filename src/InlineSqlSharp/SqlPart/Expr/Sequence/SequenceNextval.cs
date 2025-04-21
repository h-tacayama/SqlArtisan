namespace InlineSqlSharp;

public sealed class SequenceNextval : AbstractExpr
{
    private readonly Sequence _sequence;

    internal SequenceNextval(Sequence sequence)
    {
        _sequence = sequence;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_sequence.Name)
        .Append(".")
        .Append(Keywords.NEXTVAL);
}
