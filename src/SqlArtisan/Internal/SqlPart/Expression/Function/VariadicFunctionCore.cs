namespace SqlArtisan.Internal;

internal sealed class VariadicFunctionCore(string functionName, params SqlPart?[] args)
{
    private readonly string _functionName = functionName;
    private readonly SqlPart?[] _args = args;

    internal void Format(SqlBuildingBuffer buffer)
    {
        buffer.Append(_functionName)
            .OpenParenthesis();

        // The null break renders the omitted tail of a contiguous-prefix overload
        // family; a caller-supplied null never reaches here (ResolveVariadic throws).
        if (_args.Length > 0
            && _args[0] is not null)
        {
            _args[0]?.Format(buffer);

            for (int i = 1; i < _args.Length; i++)
            {
                if (_args[i] is null)
                {
                    break;
                }

                buffer.Append(", ");
                _args[i]?.Format(buffer);
            }
        }

        buffer.CloseParenthesis();
    }
}
