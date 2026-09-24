namespace SqlArtisan.Internal;

public sealed class OfClause : SqlPart
{
    // Oracle's OF names a column of the table to lock; PostgreSQL's and
    // MySQL's name the relations themselves, so exactly one field is set.
    private readonly DbColumn? _tableIdentifier;
    private readonly DbTableBase[]? _tables;

    internal OfClause(DbColumn tableIdentifier)
    {
        _tableIdentifier = tableIdentifier;
    }

    internal OfClause(DbTableBase[] tables)
    {
        _tables = tables;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(Keywords.Of);

        if (_tables is null)
        {
            buffer.PrependSpace(_tableIdentifier!);
            return;
        }

        for (int i = 0; i < _tables.Length; i++)
        {
            buffer.Append(i == 0 ? " " : ", ");
            _tables[i].FormatAsLockTarget(buffer);
        }
    }
}
