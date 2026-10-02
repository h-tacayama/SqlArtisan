namespace SqlArtisan.Internal;

internal sealed class OuterApplyClause : SqlPart, IJoinedRelation
{
    private readonly ISubquery _subquery;
    private readonly DerivedTableBase _alias;

    TableReference IJoinedRelation.Relation => _alias;

    internal OuterApplyClause(ISubquery subquery, DerivedTableBase alias)
    {
        _subquery = subquery;
        _alias = alias;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Outer} {Keywords.Apply} ")
        .EncloseInParentheses(_subquery)
        .AppendSpace()
        .Append(_alias);
}
