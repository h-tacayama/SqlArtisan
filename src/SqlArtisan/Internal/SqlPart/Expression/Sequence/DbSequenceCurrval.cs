namespace SqlArtisan.Internal;

public sealed class DbSequenceCurrval : SqlExpression
{
    private readonly DbSequence _sequence;

    internal DbSequenceCurrval(DbSequence sequence)
    {
        _sequence = sequence;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{_sequence.Name}.{Keywords.Currval}");
}
