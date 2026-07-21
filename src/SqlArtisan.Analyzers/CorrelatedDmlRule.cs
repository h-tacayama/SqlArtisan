using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Reports SQLA0005 when a correlated UPDATE/DELETE has a provably unaliased
/// target — the violation the core's Build()-time guard rejects, surfaced at
/// compile time (#256). Advisory: suppressing it does not disable the throw.
/// </summary>
/// <remarks>
/// Every proof fails toward silence (ADR 0003): a target that is not a local or
/// this-bound readonly field with a visible, provably-unaliased initializer —
/// or any unrecognized shape in the walks — yields a false negative, never a
/// false positive.
/// </remarks>
internal static class CorrelatedDmlRule
{
    private const int CtorChainDepthLimit = 8;

    public static void Check(OperationAnalysisContext context, IInvocationOperation dml)
    {
        if (dml.TargetMethod.Parameters.Length != 1
            || !IsSqlArtisanNamedType(dml.TargetMethod.Parameters[0].Type, "DbTableBase")
            || dml.Arguments.Length != 1)
        {
            return;
        }

        if (ResolveTargetSymbol(dml.Arguments[0].Value) is not { } target
            || !IsProvablyUnaliased(context.Compilation, target))
        {
            return;
        }

        // A joined UPDATE/DELETE with an unaliased target throws its own guard
        // (a different message) before the correlated guard arms, so reporting
        // "correlated" there would misdescribe it — scan the whole chain for a
        // joined step before deciding to report.
        IOperation current = dml;
        IOperation? correlated = null;
        while (FluentChain.Parent(current) is { } next)
        {
            if (IsJoinedStep(next.TargetMethod.Name))
            {
                return;
            }

            if (correlated is null)
            {
                foreach (IArgumentOperation argument in next.Arguments)
                {
                    if (FindCorrelatedColumn(argument.Value, target, argument.Value) is { } column)
                    {
                        correlated = column;
                        break;
                    }
                }
            }

            current = next;
        }

        if (correlated is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.CorrelatedDmlTargetNotAliased,
                correlated.Syntax.GetLocation()));
        }
    }

    private static bool IsJoinedStep(string name) =>
        name is "From" or "Using" or "InnerJoin" or "LeftJoin" or "RightJoin" or "FullJoin" or "On";

    // Only shapes whose runtime identity the symbol decides: a local, or a
    // readonly field read through this/statically (another instance's field is
    // a different table object).
    private static ISymbol? ResolveTargetSymbol(IOperation value) =>
        UnwrapConversion(value) switch
        {
            ILocalReferenceOperation local => local.Local,
            IFieldReferenceOperation field
                when field.Field.IsReadOnly && IsThisOrStatic(field) => field.Field,
            _ => null,
        };

    private static bool IsProvablyUnaliased(Compilation compilation, ISymbol target)
    {
        if (target.DeclaringSyntaxReferences.Length != 1
            || target.DeclaringSyntaxReferences[0].GetSyntax() is not VariableDeclaratorSyntax declarator
            || declarator.Initializer is not { } initializer)
        {
            return false;
        }

        SemanticModel model = compilation.GetSemanticModel(declarator.SyntaxTree);
        if (model.GetOperation(initializer.Value) is not { } initial
            || UnwrapConversion(initial) is not IObjectCreationOperation creation
            || !CreationProvablyUnaliased(compilation, creation))
        {
            return false;
        }

        return target is ILocalSymbol
            ? HasNoWrites(EnclosingMemberScope(declarator), target.Name)
            : ConstructorsDoNotWrite((IFieldSymbol)target);
    }

    // A zero-argument shortcut would false-positive on a constructor hardcoding
    // an alias (': base("t", "x")'), so the value reaching DbTableBase's
    // tableAlias slot is traced through the ctor-initializer chain and must
    // resolve to a null/empty constant.
    private static bool CreationProvablyUnaliased(Compilation compilation, IObjectCreationOperation creation)
    {
        if (creation.Constructor is not { } constructor || !DerivesFromDbTableBase(creation.Type))
        {
            return false;
        }

        Dictionary<IParameterSymbol, object?> values = EvaluateArguments(creation.Arguments, previous: null);

        for (int depth = 0; depth < CtorChainDepthLimit; depth++)
        {
            if (IsSqlArtisanNamedType(constructor.ContainingType, "DbTableBase"))
            {
                return constructor.Parameters.Length == 2
                    && values.TryGetValue(constructor.Parameters[1], out object? alias)
                    && alias is null or "";
            }

            if (constructor.DeclaringSyntaxReferences.Length != 1
                || constructor.DeclaringSyntaxReferences[0].GetSyntax() is not ConstructorDeclarationSyntax declaration
                || declaration.Initializer is not { } initializer)
            {
                return false;
            }

            SemanticModel model = compilation.GetSemanticModel(initializer.SyntaxTree);
            if (model.GetOperation(initializer) is not IInvocationOperation call
                || call.TargetMethod.MethodKind != MethodKind.Constructor)
            {
                return false;
            }

            values = EvaluateArguments(call.Arguments, values);
            constructor = call.TargetMethod;
        }

        return false;
    }

    // Roslyn materializes optional parameters as DefaultValue arguments carrying
    // their constant, so named/optional/reordered arguments need no extra logic.
    private static Dictionary<IParameterSymbol, object?> EvaluateArguments(
        ImmutableArray<IArgumentOperation> arguments,
        Dictionary<IParameterSymbol, object?>? previous)
    {
        Dictionary<IParameterSymbol, object?> values = new(SymbolEqualityComparer.Default);
        foreach (IArgumentOperation argument in arguments)
        {
            if (argument.Parameter is not { } parameter)
            {
                continue;
            }

            IOperation value = UnwrapConversion(argument.Value);
            if (value.ConstantValue.HasValue)
            {
                values[parameter] = value.ConstantValue.Value;
            }
            else if (previous is not null
                && value is IParameterReferenceOperation reference
                && previous.TryGetValue(reference.Parameter, out object? known))
            {
                values[parameter] = known;
            }
        }

        return values;
    }

    // Text-level scan; a shadowing name in a nested scope over-counts writes,
    // which fails toward silence.
    private static bool HasNoWrites(SyntaxNode scope, string name)
    {
        foreach (SyntaxNode node in scope.DescendantNodes())
        {
            bool writes = node switch
            {
                AssignmentExpressionSyntax assignment => AssignsTo(assignment.Left, name),
                PrefixUnaryExpressionSyntax prefix
                    when prefix.IsKind(SyntaxKind.PreIncrementExpression)
                        || prefix.IsKind(SyntaxKind.PreDecrementExpression)
                    => AssignsTo(prefix.Operand, name),
                PostfixUnaryExpressionSyntax postfix
                    when postfix.IsKind(SyntaxKind.PostIncrementExpression)
                        || postfix.IsKind(SyntaxKind.PostDecrementExpression)
                    => AssignsTo(postfix.Operand, name),
                ArgumentSyntax argument
                    when !argument.RefKindKeyword.IsKind(SyntaxKind.None)
                    => AssignsTo(argument.Expression, name),
                _ => false,
            };

            if (writes)
            {
                return false;
            }
        }

        return true;
    }

    // A tuple target reassigns each named element (`(t, x) = ...`), so the scan
    // must descend into it — an unhandled write shape would under-count and
    // false-positive rather than fail toward silence.
    private static bool AssignsTo(ExpressionSyntax target, string name)
    {
        while (target is ParenthesizedExpressionSyntax parenthesized)
        {
            target = parenthesized.Expression;
        }

        return target switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text == name,
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text == name,
            TupleExpressionSyntax tuple => tuple.Arguments.Any(a => AssignsTo(a.Expression, name)),
            _ => false,
        };
    }

    private static SyntaxNode EnclosingMemberScope(SyntaxNode node) =>
        node.FirstAncestorOrSelf<MemberDeclarationSyntax>() ?? node.SyntaxTree.GetRoot();

    // readonly bounds all writes to the initializer and constructors, so a
    // constructor scan is complete.
    private static bool ConstructorsDoNotWrite(IFieldSymbol field)
    {
        foreach (SyntaxReference reference in field.ContainingType.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is not TypeDeclarationSyntax type)
            {
                return false;
            }

            foreach (SyntaxNode node in type.DescendantNodes())
            {
                if (node is ConstructorDeclarationSyntax constructor && !HasNoWrites(constructor, field.Name))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static IOperation? FindCorrelatedColumn(IOperation node, ISymbol target, IOperation root)
    {
        if (node is IAnonymousFunctionOperation or ILocalFunctionOperation or IDelegateCreationOperation)
        {
            return null;
        }

        if (IsTargetColumn(node, target) && IsInsideSubquery(node, root))
        {
            return node;
        }

        foreach (IOperation child in node.ChildOperations)
        {
            if (FindCorrelatedColumn(child, target, root) is { } hit)
            {
                return hit;
            }
        }

        return null;
    }

    // The member belongs to the user's table class; its DbColumn TYPE is the
    // SqlArtisan gate (and disambiguates System.Data.Common.DbColumn).
    private static bool IsTargetColumn(IOperation node, ISymbol target)
    {
        if (node is not (IPropertyReferenceOperation or IFieldReferenceOperation))
        {
            return false;
        }

        var member = (IMemberReferenceOperation)node;
        if (!IsSqlArtisanNamedType(member.Type, "DbColumn"))
        {
            return false;
        }

        IOperation? instance = member.Instance is { } receiver ? UnwrapConversion(receiver) : null;
        return target switch
        {
            ILocalSymbol local => instance is ILocalReferenceOperation localReference
                && SymbolEqualityComparer.Default.Equals(localReference.Local, local),
            IFieldSymbol field => instance is IFieldReferenceOperation fieldReference
                && SymbolEqualityComparer.Default.Equals(fieldReference.Field, field)
                && IsThisOrStatic(fieldReference),
            _ => false,
        };
    }

    // The runtime boundary is EncloseInParentheses(ISubquery); its source-level
    // image is a Select-headed chain bound as an argument of a SqlArtisan call.
    private static bool IsInsideSubquery(IOperation node, IOperation root)
    {
        for (IOperation? current = node.Parent; current is not null; current = current.Parent)
        {
            if (current == root)
            {
                return false;
            }

            if (current is IArgumentOperation { Parent: IInvocationOperation host }
                && DialectUsageAnalyzer.IsFromSqlArtisan(host.TargetMethod.ContainingAssembly)
                && ChainHeadIsSelect(host))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ChainHeadIsSelect(IInvocationOperation invocation)
    {
        IInvocationOperation head = invocation;
        while (head.Instance is { } instance
            && UnwrapConversion(instance) is IInvocationOperation receiver
            && DialectUsageAnalyzer.IsFromSqlArtisan(receiver.TargetMethod.ContainingAssembly))
        {
            head = receiver;
        }

        return head.TargetMethod.Name == "Select";
    }

    private static bool IsThisOrStatic(IFieldReferenceOperation field) =>
        field.Instance is null or IInstanceReferenceOperation;

    private static bool DerivesFromDbTableBase(ITypeSymbol? type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (IsSqlArtisanNamedType(current, "DbTableBase"))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSqlArtisanNamedType(ITypeSymbol? type, string name) =>
        type is INamedTypeSymbol named
        && named.Name == name
        && DialectUsageAnalyzer.IsFromSqlArtisan(named.ContainingAssembly);

    private static IOperation UnwrapConversion(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }
}
