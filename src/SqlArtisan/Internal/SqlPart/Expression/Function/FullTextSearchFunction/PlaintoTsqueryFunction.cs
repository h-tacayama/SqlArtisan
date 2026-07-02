namespace SqlArtisan.Internal;

/// <summary>
/// The PostgreSQL <c>PLAINTO_TSQUERY([config,] text)</c> function: parses plain,
/// unformatted text into a text-search query (terms ANDed). The optional
/// configuration is emitted as an inline string literal.
/// </summary>
public sealed class PlaintoTsqueryFunction : SqlExpression
{
    private readonly string? _config;
    private readonly SqlExpression _text;

    internal PlaintoTsqueryFunction(string? config, SqlExpression text)
    {
        _config = config;
        _text = text;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer
            .Append(Keywords.PlaintoTsquery)
            .OpenParenthesis();

        if (_config is not null)
        {
            buffer
                .AppendStringLiteral(_config)
                .Append(", ");
        }

        buffer
            .Append(_text)
            .CloseParenthesis();
    }
}
