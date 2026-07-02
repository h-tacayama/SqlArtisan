namespace SqlArtisan.Internal;

/// <summary>
/// The SQL Server full-text <c>FREETEXT(column, freetext)</c> predicate, matching
/// rows whose column matches the meaning — not the exact wording — of the text.
/// </summary>
public sealed class FreetextCondition : SqlCondition
{
    private readonly SqlExpression _column;
    private readonly SqlExpression _freetext;

    internal FreetextCondition(SqlExpression column, SqlExpression freetext)
    {
        _column = column;
        _freetext = freetext;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Freetext)
        .OpenParenthesis()
        .Append(_column)
        .PrependComma(_freetext)
        .CloseParenthesis();
}
