namespace SqlArtisan;

public sealed class SequenceCurrVal : AbstractExpr
{
    private readonly SequenceObject _sequence;

    internal SequenceCurrVal(SequenceObject sequence)
    {
        _sequence = sequence;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append(_sequence.Name)
        .Append(".")
        .Append(Keywords.CurrVal);
}
