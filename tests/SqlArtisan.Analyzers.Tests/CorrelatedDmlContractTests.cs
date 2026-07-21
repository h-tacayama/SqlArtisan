using System;
using System.Linq;
using System.Reflection;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// Pins the core-API facts the SQLA0005 rule keys on (the ADR 0009 name
/// contract): if the API drifts, these fail loudly instead of the rule silently
/// dying or losing its no-false-positive soundness.
/// </summary>
public class CorrelatedDmlContractTests
{
    private static readonly Assembly Core = typeof(Sql).Assembly;

    // The rule reads the target from argument 0 of any public Update/DeleteFrom.
    [Fact]
    public void DmlHead_EveryOverloadTakesSingleDbTableBase()
    {
        MethodInfo[] heads = [.. Core.GetExportedTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(m => m.Name is "Update" or "DeleteFrom")];

        Assert.NotEmpty(heads);
        Assert.All(heads, head =>
        {
            ParameterInfo parameter = Assert.Single(head.GetParameters());
            Assert.Equal(typeof(DbTableBase), parameter.ParameterType);
        });
    }

    // The ctor-chain proof targets the second parameter of the single ctor.
    [Fact]
    public void DbTableBase_SingleCtorWithAliasAsSecondParameter()
    {
        ConstructorInfo ctor = Assert.Single(
            typeof(DbTableBase).GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        ParameterInfo[] parameters = ctor.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.All(parameters, p => Assert.Equal(typeof(string), p.ParameterType));
        Assert.Equal("tableAlias", parameters[1].Name);
    }

    // The proof is final only because the alias can never change after construction.
    [Fact]
    public void DbTableBase_AliasField_IsReadonly()
    {
        FieldInfo? field = typeof(DbTableBase).GetField(
            "_tableAlias", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(field);
        Assert.True(field.IsInitOnly);
    }

    // The column gate is the member's DbColumn type; the subquery boundary is a
    // Select-headed chain.
    [Theory]
    [InlineData("DbColumn")]
    [InlineData("DbTableBase")]
    public void GateType_ExistsInCoreApi(string typeName)
    {
        Assert.Contains(Core.GetExportedTypes(), t => t.Name == typeName);
    }

    [Fact]
    public void Select_ExistsOnSql()
    {
        Assert.Contains(
            typeof(Sql).GetMethods(BindingFlags.Public | BindingFlags.Static),
            m => m.Name == "Select");
    }
}
