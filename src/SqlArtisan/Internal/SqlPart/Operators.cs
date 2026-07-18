namespace SqlArtisan.Internal;

internal static class Operators
{
    // <@ / @> deliberately duplicate token values with the Jsonb constants —
    // separate names keep each call site's operand domain explicit.
    internal const string ArrayContainedBy = "<@";
    internal const string ArrayContains = "@>";
    internal const string ArrayOverlaps = "&&";
    internal const string Asterisk = "*";
    internal const string DoublePipe = "||";
    internal const string Equality = "=";
    internal const string GreaterThan = ">";
    internal const string GreaterThanOrEqual = ">=";
    internal const string Inequality = "<>";
    internal const string JsonArrow = "->";
    internal const string JsonArrowText = "->>";
    internal const string JsonbContains = "@>";
    internal const string JsonbExists = "?";
    internal const string JsonbExistsAll = "?&";
    internal const string JsonbExistsAny = "?|";
    internal const string JsonHashArrow = "#>";
    internal const string JsonHashArrowText = "#>>";
    internal const string LessThan = "<";
    internal const string LessThanOrEqual = "<=";
    internal const string Minus = "-";
    internal const string Percent = "%";
    internal const string Plus = "+";
    internal const string Slash = "/";
    internal const string TsMatch = "@@";
}
