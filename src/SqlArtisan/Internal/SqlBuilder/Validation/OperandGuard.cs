namespace SqlArtisan.Internal;

// The operator overloads take a null left operand when a nullable variable is
// used unchecked; the parenthesized render then drops it silently (`( + :0)`),
// so every operator rejects it here — the same door for all eleven.
internal static class OperandGuard
{
    internal const string NullLeftOperandMessage =
        "The left operand of an operator cannot be null; write Sql.Null for a SQL NULL literal.";

    // Named after the operators' own parameter, so ParamName reads as the caller's.
    internal static SqlExpression ThrowIfNull(SqlExpression? @this) =>
        @this ?? throw new ArgumentNullException(nameof(@this), NullLeftOperandMessage);
}
