using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0200 for <c>IS NULL</c> / <c>IS NOT NULL</c> on a column the
/// schema declares NOT NULL — a predicate whose answer is fixed before the
/// query runs (#266).
/// </summary>
/// <remarks>
/// Only a statement that visibly builds its own query is judged, and only when
/// that query has no outer join: past one the column is legitimately NULL, and a
/// chain assembled elsewhere can carry a join this never sees.
/// </remarks>
internal static class ConstantNullPredicateRule
{
    public static void Check(OperationAnalysisContext context, IPropertyReferenceOperation predicate)
    {
        // Nullable is the only fact that decides this, and only when it is known
        // to be false: a nullable column makes both predicates meaningful.
        if (SchemaMetadata.Fact(predicate.Instance, SchemaMetadata.NullableArgument) is not false
            || !FluentChain.HasVisibleStatementHead(predicate)
            || FluentChain.HasOuterJoin(predicate))
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
