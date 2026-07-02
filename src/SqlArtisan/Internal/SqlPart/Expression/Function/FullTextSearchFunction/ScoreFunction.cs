namespace SqlArtisan.Internal;

/// <summary>
/// The Oracle Text <c>SCORE(label)</c> function: the relevance score computed by
/// the <c>CONTAINS</c> operator carrying the same label.
/// </summary>
public sealed class ScoreFunction : SqlExpression
{
    private readonly string _label;

    internal ScoreFunction(int label)
    {
        _label = label.ToInvariantString();
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Score)
        .OpenParenthesis()
        .Append(_label)
        .CloseParenthesis();
}
