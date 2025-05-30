﻿namespace SqlArtisan.Internal;

public sealed class LowerFunction : SqlExpression
{
    private readonly SqlExpression _source;

    internal LowerFunction(SqlExpression source)
    {
        _source = source;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Lower)
        .OpenParenthesis()
        .Append(_source)
        .CloseParenthesis();
}
