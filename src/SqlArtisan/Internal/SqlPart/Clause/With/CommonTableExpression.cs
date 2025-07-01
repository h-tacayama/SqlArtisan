namespace SqlArtisan.Internal;

public sealed class CommonTableExpression(string name, ISubquery subquery)
{
    private readonly string _name = name;
    private readonly ISubquery _subquery = subquery;

    internal void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(_name);
        buffer.EncloseInSpaces(Keywords.As);
        buffer.OpenParenthesis();
        _subquery?.Format(buffer);
        buffer.CloseParenthesis();
    }
}
