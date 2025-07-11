﻿using SqlArtisan.Internal;

namespace SqlArtisan;

public abstract class DbTableBase : TableReference
{
    private readonly string _tableAlias;

    public DbTableBase(string tableAlias)
    {
        _tableAlias = tableAlias;
    }

    public DbTableBase(string tableName, string tableAlias) : base(tableName)
    {
        _tableAlias = tableAlias;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        base.Format(buffer);

        if (!string.IsNullOrEmpty(_tableAlias))
        {
            buffer.AppendSpace();
            buffer.EncloseInAliasQuotes(_tableAlias);
        }
    }
}
