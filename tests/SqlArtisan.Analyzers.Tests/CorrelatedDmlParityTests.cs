using System;
using SqlArtisan;
using static SqlArtisan.Sql;

namespace SqlArtisan.Analyzers.Tests;

/// <summary>
/// The empirical no-false-positive gate for SQLA0005: every shape the analyzer
/// reports must throw the same finding at Build(), and the exempt shapes must
/// build — parity proven by execution, not argument.
/// </summary>
public class CorrelatedDmlParityTests
{
    private const string GuardMessage =
        "The target of a correlated UPDATE or DELETE must be aliased.";

    private sealed class PTable : DbTableBase
    {
        public DbColumn Id;
        public DbColumn Dep;

        public PTable(string alias = "") : base("p", alias)
        {
            Id = new DbColumn(this, "id");
            Dep = new DbColumn(this, "dep");
        }
    }

    [Fact]
    public void Descriptor_MessageMirrorsRuntimeGuardVerbatim()
    {
        Assert.Equal(
            GuardMessage,
            $"{DiagnosticDescriptors.CorrelatedDmlTargetNotAliased.MessageFormat}.");
    }

    [Fact]
    public void Update_CorrelatedSetSubqueryUnaliasedTarget_ThrowsArgumentException()
    {
        PTable t = new();
        PTable r = new("r");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            Update(t)
            .Set(t.Id == Select(Max(r.Id)).From(r).Where(r.Dep == t.Dep))
            .Build());

        Assert.Equal(GuardMessage, ex.Message);
    }

    [Fact]
    public void DeleteFrom_CorrelatedExistsSubqueryUnaliasedTarget_ThrowsArgumentException()
    {
        PTable t = new();
        PTable r = new("r");

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
            .Where(Exists(Select(r.Id).From(r).Where(r.Id == t.Id)))
            .Build());

        Assert.Equal(GuardMessage, ex.Message);
    }

    [Fact]
    public void DeleteFrom_SameInstanceInInnerSelect_ThrowsArgumentException()
    {
        PTable t = new();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            DeleteFrom(t)
            .Where(t.Id.In(Select(t.Id).From(t)))
            .Build());

        Assert.Equal(GuardMessage, ex.Message);
    }

    [Fact]
    public void InsertInto_SelectReadingUnaliasedTarget_Builds()
    {
        PTable t = new();

        SqlStatement sql = InsertInto(t, t.Id).Select(t.Id).From(t).Build();

        Assert.Equal("INSERT INTO p (id) SELECT id FROM p", sql.Text);
    }

    [Fact]
    public void Update_UncorrelatedSubqueryUnaliasedTarget_Builds()
    {
        PTable t = new();
        PTable r = new("r");

        SqlStatement sql =
            Update(t)
            .Set(t.Id == 1)
            .Where(t.Id.In(Select(r.Id).From(r)))
            .Build();

        Assert.Equal(
            "UPDATE p SET id = :0 WHERE id IN (SELECT \"r\".id FROM p \"r\")",
            sql.Text);
    }
}
