using System.Reflection;
using SqlArtisan.Internal;

namespace SqlArtisan.Tests;

// A node is a value a caller can hold and compose into several statements, so no
// SqlPart may carry state a member could change after construction
// (.claude/rules/public-api-design.md, #668).
public class SqlPartImmutabilityTests
{
    [Fact]
    public void SqlPart_EveryDeclaredInstanceField_IsReadOnly()
    {
        Type[] parts = [.. typeof(SqlPart).Assembly.GetTypes()
            .Where(t => typeof(SqlPart).IsAssignableFrom(t))];

        Assert.NotEmpty(parts);

        // A captured primary-constructor parameter (<name>P) cannot be declared
        // readonly in C#; the rule, not this gate, keeps it unassigned.
        string[] mutable = [.. parts
            .SelectMany(t => t.GetFields(
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly))
            .Where(f => !f.IsInitOnly && !f.Name.StartsWith('<'))
            .Select(f => $"{f.DeclaringType!.Name}.{f.Name}")];

        Assert.True(mutable.Length == 0, string.Join(", ", mutable));
    }
}
