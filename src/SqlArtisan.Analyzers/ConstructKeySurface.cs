using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SqlArtisan.Analyzers;

/// <summary>
/// Every <c>sqlartisan_construct_*</c> key an override can take effect through: the member and
/// arity keys of each SqlArtisan member a usage resolves an override for. Read from the
/// referenced assembly, not the matrix, so a key on an unentered member is not called stale.
/// </summary>
internal static class ConstructKeySurface
{
    public static HashSet<string> Collect(IAssemblySymbol sqlArtisan)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectNamespace(sqlArtisan.GlobalNamespace, keys);
        return keys;
    }

    private static void CollectNamespace(INamespaceSymbol ns, HashSet<string> keys)
    {
        foreach (INamespaceSymbol child in ns.GetNamespaceMembers())
        {
            CollectNamespace(child, keys);
        }

        foreach (INamedTypeSymbol type in ns.GetTypeMembers())
        {
            CollectType(type, keys);
        }
    }

    private static void CollectType(INamedTypeSymbol type, HashSet<string> keys)
    {
        if (!IsReachable(type.DeclaredAccessibility))
        {
            return;
        }

        foreach (INamedTypeSymbol nested in type.GetTypeMembers())
        {
            CollectType(nested, keys);
        }

        foreach (ISymbol member in type.GetMembers())
        {
            if (!IsReachable(member.DeclaredAccessibility))
            {
                continue;
            }

            switch (member)
            {
                // Operators reach the override through their own walkers, which skip the
                // construct-member filter, so they are taken whatever their containing type.
                case IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator } op:
                    AddMethod(op, keys);
                    break;
                case IMethodSymbol { MethodKind: MethodKind.Ordinary } method
                    when DialectUsageAnalyzer.IsConstructMember(method):
                    AddMethod(method, keys);
                    break;
                case IPropertySymbol { IsIndexer: false } or IFieldSymbol
                    when DialectUsageAnalyzer.IsConstructMember(member):
                    keys.Add(ConstructKeyNaming.MemberKey(member.Name));
                    break;
            }
        }
    }

    private static void AddMethod(IMethodSymbol method, HashSet<string> keys)
    {
        keys.Add(ConstructKeyNaming.MemberKey(method.Name));
        keys.Add(ConstructKeyNaming.ArityKey(method.Name, method.Parameters.Length));
    }

    // Protected members are reachable from a user's derived table class.
    private static bool IsReachable(Accessibility accessibility) =>
        accessibility is Accessibility.Public
            or Accessibility.Protected
            or Accessibility.ProtectedOrInternal;
}
