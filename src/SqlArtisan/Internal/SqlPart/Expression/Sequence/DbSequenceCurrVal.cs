namespace SqlArtisan.Internal;

public sealed class DbSequenceCurrVal : SqlExpression
{
    private readonly DbSequence _sequence;

    internal DbSequenceCurrVal(DbSequence sequence)
    {
        _sequence = sequence;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{_sequence.Name}.{Keywords.CurrVal}");
}
