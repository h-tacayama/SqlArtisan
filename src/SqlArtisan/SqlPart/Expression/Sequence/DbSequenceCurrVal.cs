namespace SqlArtisan;

public sealed class DbSequenceCurrVal : SqlExpression
{
    private readonly DbSequence _sequence;

    internal DbSequenceCurrVal(DbSequence sequence)
    {
        _sequence = sequence;
    }

    internal override void FormatSql(SqlBuildingBuffer buffer) => buffer
        .Append($"{_sequence.Name}.{Keywords.CurrVal}");
}
