using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reads the schema facts a generated table class carries on its
/// <c>DbColumn</c> properties (#266). Every fact is tri-state: a named argument
/// the generator never wrote is <see langword="null"/> — unknown — and unknown
/// must read as silence, never as false.
/// </summary>
/// <remarks>
/// Matched by fully qualified name, never by a type reference: the analyzer
/// takes no build dependency on the core (ADR 0009).
/// </remarks>
internal static class SchemaMetadata
{
    public const string AttributeName = "SqlArtisan.DbColumnMetadataAttribute";

    public const string NullableArgument = "Nullable";

    public const string HasDefaultArgument = "HasDefault";

    public const string IndexedArgument = "Indexed";

    public static bool? Fact(IOperation? operation, string argument) =>
        operation is IPropertyReferenceOperation column
            ? Fact(column.Property, argument)
            : null;

    // The attribute is applied to nothing else, so its presence is the only test
    // needed — no check that the property is a DbColumn.
    public static bool? Fact(IPropertySymbol column, string argument)
    {
        foreach (AttributeData attribute in column.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != AttributeName)
            {
                continue;
            }

            foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments)
            {
                if (named.Key == argument && named.Value.Value is bool value)
                {
                    return value;
                }
            }
        }

        return null;
    }
}
