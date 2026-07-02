namespace SqlArtisan.Internal;

/// <summary>
/// The PostgreSQL <c>TO_TSVECTOR([config,] document)</c> function: reduces a
/// document to a text-search vector. The optional configuration is emitted as an
/// inline string literal.
/// </summary>
public sealed class ToTsvectorFunction : SqlExpression
{
    private readonly string? _config;
    private readonly SqlExpression _document;

    internal ToTsvectorFunction(string? config, SqlExpression document)
    {
        _config = config;
        _document = document;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer
            .Append(Keywords.ToTsvector)
            .OpenParenthesis();

        if (_config is not null)
        {
            buffer
                .AppendStringLiteral(_config)
                .Append(", ");
        }

        buffer
            .Append(_document)
            .CloseParenthesis();
    }
}
