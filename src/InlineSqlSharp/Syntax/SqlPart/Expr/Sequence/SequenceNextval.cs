namespace InlineSqlSharp;

public sealed class SequenceNextval(Sequence sequence) : AbstractExpr
{
    private readonly Sequence _sequence = sequence;

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_sequence.Name)
        .Append(".")
        .Append(Keywords.NEXTVAL);
}
