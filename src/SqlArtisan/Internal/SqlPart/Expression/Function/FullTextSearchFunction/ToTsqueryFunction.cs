namespace SqlArtisan.Internal;

/// <summary>
/// The PostgreSQL <c>TO_TSQUERY([config,] text)</c> function: parses text already
/// in tsquery syntax (operators <c>&amp;</c>, <c>|</c>, <c>!</c>) into a
/// text-search query. The optional configuration is emitted as an inline string
/// literal.
/// </summary>
public sealed class ToTsqueryFunction : SqlExpression
{
    private readonly string? _config;
    private readonly SqlExpression _text;

    internal ToTsqueryFunction(string? config, SqlExpression text)
    {
        _config = config;
        _text = text;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer
            .Append(Keywords.ToTsquery)
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
