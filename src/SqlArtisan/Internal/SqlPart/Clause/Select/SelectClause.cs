﻿namespace SqlArtisan.Internal;

internal sealed class SelectClause : SqlPart
{
    private readonly SqlPart[] _selectItems;

    private SelectClause(SqlPart[] selectItems)
    {
        _selectItems = selectItems;
    }

    internal static SelectClause Parse(object[] selectItems) =>
        new(SelectItemResolver.Resolve(selectItems));

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Select} ")
        .AppendSelectItems(_selectItems);
}
