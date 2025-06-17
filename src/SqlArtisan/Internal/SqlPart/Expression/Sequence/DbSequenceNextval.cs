namespace SqlArtisan.Internal;

public sealed class DbSequenceNextval : SqlExpression
{
    private readonly DbSequence _sequence;

    internal DbSequenceNextval(DbSequence sequence)
    {
        _sequence = sequence;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{_sequence.Name}.{Keywords.Nextval}");
}
