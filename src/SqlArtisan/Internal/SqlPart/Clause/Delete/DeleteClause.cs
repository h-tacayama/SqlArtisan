﻿namespace SqlArtisan.Internal;

public sealed class DeleteClause : SqlPart
{
    private readonly DbTableBase _table;

    internal DeleteClause(DbTableBase table)
    {
        _table = table;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Delete} {Keywords.From} ")
        .Append(_table);
}
