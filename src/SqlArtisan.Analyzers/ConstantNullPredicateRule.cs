using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0007 for <c>IS NULL</c> / <c>IS NOT NULL</c> on a column the
/// schema declares NOT NULL — a predicate whose answer is fixed before the
/// query runs (#266).
/// </summary>
internal static class ConstantNullPredicateRule
{
    public static void Check(OperationAnalysisContext context, IPropertyReferenceOperation predicate)
    {
        // Nullable is the only fact that decides this, and only when it is known
        // to be false: a nullable column makes both predicates meaningful.
        if (SchemaMetadata.Fact(predicate.Instance, SchemaMetadata.NullableArgument) is not false)
        {
            return;
        }

        bool isNull = predicate.Property.Name == "IsNull";

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ConstantNullPredicate,
            predicate.Syntax.GetLocation(),
            ((IPropertyReferenceOperation)predicate.Instance!).Property.Name,
            predicate.Property.Name,
            isNull ? "false" : "true"));
    }
}
