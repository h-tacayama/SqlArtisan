namespace SqlArtisan.Internal;

// The operator overloads take a null left operand when a nullable variable is
// used unchecked; the parenthesized render then drops it silently (`( + :0)`),
// so every operator rejects it here — the same door for all twelve.
internal static class OperandGuard
{
    // Named after the operators' own parameter, so ParamName reads as the caller's.
    internal static SqlExpression ThrowIfNull(SqlExpression? @this, string op) =>
        @this ?? throw new ArgumentNullException(nameof(@this), NullLeftOperandMessage(op));

    // `null + col` binds the string-left `+` too, so it keeps this message.
    internal static string ThrowIfNull(string? leftSide, string op) =>
        leftSide ?? throw new ArgumentNullException(nameof(leftSide), NullLeftOperandMessage(op));

    internal static SqlExpression ThrowIfNullCompared(SqlExpression? @this, string op) =>
        @this ?? throw ExpressionResolver.NullCompared(nameof(@this), op);

    internal static string NullLeftOperandMessage(string op) =>
        $"The left operand of {op} cannot be null; write Sql.Null for a SQL NULL literal.";
}
