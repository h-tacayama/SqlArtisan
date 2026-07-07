namespace SqlArtisan.Internal;

// The operator-joined counterpart to VariadicFunctionCore: renders a flat,
// parenthesized chain (a op b op c) instead of a comma-joined function call.
internal sealed class OperatorJoinedFunctionCore(string @operator, params SqlPart[] args)
{
    private readonly string _operator = @operator;
    private readonly SqlPart[] _args = args;

    internal void Format(SqlBuildingBuffer buffer)
    {
        buffer.OpenParenthesis(_args[0]);

        for (int i = 1; i < _args.Length; i++)
        {
            buffer.EncloseInSpaces(_operator);
            _args[i].Format(buffer);
        }

        buffer.CloseParenthesis();
    }
}
