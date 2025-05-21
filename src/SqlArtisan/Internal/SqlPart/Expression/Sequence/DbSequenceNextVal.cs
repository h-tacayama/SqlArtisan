namespace SqlArtisan.Internal;

public sealed class DbSequenceNextVal : SqlExpression
{
    private readonly DbSequence _sequence;

    internal DbSequenceNextVal(DbSequence sequence)
    {
        _sequence = sequence;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{_sequence.Name}.{Keywords.NextVal}");
}
