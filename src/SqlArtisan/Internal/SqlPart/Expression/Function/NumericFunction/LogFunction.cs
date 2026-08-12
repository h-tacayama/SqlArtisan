namespace SqlArtisan.Internal;

public sealed class LogFunction : SqlExpression
{
    private readonly SqlExpression? _base;
    private readonly SqlExpression _expr;

    internal LogFunction(SqlExpression expr)
    {
        _expr = expr;
    }

    internal LogFunction(SqlExpression @base, SqlExpression expr)
    {
        _base = @base;
        _expr = expr;
    }

    internal override void Format(SqlBuildingBuffer buffer)
    {
        buffer
            .Append(Keywords.Log)
            .OpenParenthesis();

        // The base leads the argument list, so it cannot ride a trailing
        // PrependCommaIfNotNull the way an optional last argument does.
        if (_base is null)
        {
            buffer.Append(_expr);
        }
        else
        {
            buffer
                .Append(_base)
                .PrependComma(_expr);
        }

        buffer.CloseParenthesis();
    }
}
