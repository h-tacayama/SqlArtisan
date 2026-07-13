namespace SqlArtisan.Internal;

/// <summary>
/// The <c>JSON_EXTRACT</c> function: extracts a value from a JSON document at
/// the given path (MySQL, SQLite). The path is emitted as an inline string
/// literal.
/// </summary>
public sealed class JsonExtractFunction : SqlExpression
{
    private readonly SqlExpression _jsonDoc;
    private readonly string _path;

    internal JsonExtractFunction(SqlExpression jsonDoc, string path)
    {
        StringGuard.ThrowIfNullOrEmpty(path, "JSON_EXTRACT requires a path.");

        _jsonDoc = jsonDoc;
        _path = path;
    }

    internal override void Format(SqlBuildingBuffer buffer) => buffer
        .Append(Keywords.JsonExtract)
        .OpenParenthesis()
        .Append(_jsonDoc)
        .Append(", ")
        .AppendStringLiteral(_path)
        .CloseParenthesis();
}
