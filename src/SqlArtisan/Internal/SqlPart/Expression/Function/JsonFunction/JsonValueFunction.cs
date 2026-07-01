namespace SqlArtisan.Internal;

/// <summary>
/// The <c>JSON_VALUE</c> function: extracts a scalar value from a JSON document
/// at the given path (Oracle, SQL Server). The path is emitted as an inline
/// string literal.
/// </summary>
public sealed class JsonValueFunction : SqlExpression
{
    private readonly SqlExpression _jsonDoc;
    private readonly string _path;

    internal JsonValueFunction(SqlExpression jsonDoc, string path)
    {
        _jsonDoc = jsonDoc;
        _path = path;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.JsonValue)
        .OpenParenthesis()
        .Append(_jsonDoc)
        .Append(", ")
        .AppendStringLiteral(_path)
        .CloseParenthesis();
}
