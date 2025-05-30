﻿using System.Data;

namespace SqlArtisan.Internal;

public sealed class BindValue(
    object value,
    DbType? dbType = null,
    ParameterDirection? direction = null,
    int? size = null) : SqlExpression
{
    public object Value => value;

    public DbType? DbType => dbType;

    public ParameterDirection? Direction => direction;

    public int? Size => size;

    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.AddParameter(this);
}
