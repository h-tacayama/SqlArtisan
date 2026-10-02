namespace SqlArtisan.Internal;

internal sealed class CrossJoinLateralClause : SqlPart, IJoinedRelation
{
    private readonly ISubquery _subquery;
    private readonly DerivedTableBase _alias;

    TableReference IJoinedRelation.Relation => _alias;

    internal CrossJoinLateralClause(ISubquery subquery, DerivedTableBase alias)
    {
        _subquery = subquery;
        _alias = alias;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append($"{Keywords.Cross} {Keywords.Join} {Keywords.Lateral} ")
        .EncloseInParentheses(_subquery)
        .AppendSpace()
        .Append(_alias);
}
