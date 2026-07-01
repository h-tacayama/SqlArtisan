namespace SqlArtisan.Internal;

/// <summary>
/// The <c>JSON_QUERY</c> function: extracts a JSON object or array from a JSON
/// document at the given path (Oracle, SQL Server). The path is emitted as an
/// inline string literal.
/// </summary>
public sealed class JsonQueryFunction : SqlExpression
{
    private readonly SqlExpression _jsonDoc;
    private readonly string _path;

    internal JsonQueryFunction(SqlExpression jsonDoc, string path)
    {
        _jsonDoc = jsonDoc;
        _path = path;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.JsonQuery)
        .OpenParenthesis()
        .Append(_jsonDoc)
        .Append(", ")
        .AppendStringLiteral(_path)
        .CloseParenthesis();
}
