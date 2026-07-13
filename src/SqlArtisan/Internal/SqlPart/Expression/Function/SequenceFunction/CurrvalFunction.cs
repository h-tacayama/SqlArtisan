namespace SqlArtisan.Internal;

public sealed class CurrvalFunction : SqlExpression
{
    private readonly string _sequenceName;

    internal CurrvalFunction(string sequenceName)
    {
        StringGuard.ThrowIfNullOrEmpty(sequenceName, "CURRVAL requires a sequence name.");

        _sequenceName = sequenceName;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.Currval)
        .OpenParenthesis()
        .AppendStringLiteral(_sequenceName)
        .CloseParenthesis();
}
