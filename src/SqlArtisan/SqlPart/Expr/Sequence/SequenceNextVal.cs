namespace SqlArtisan;

public sealed class SequenceNextVal : AbstractExpr
{
    private readonly SequenceObject _sequence;

    internal SequenceNextVal(SequenceObject sequence)
    {
        _sequence = sequence;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_sequence.Name)
        .Append(".")
        .Append(Keywords.NextVal);
}
