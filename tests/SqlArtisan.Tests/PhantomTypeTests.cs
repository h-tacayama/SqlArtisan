// Markers are aliased, not imported: the phantom type `Oracle` collides with the
// Oracle.ManagedDataAccess driver namespace (same shadowing hazard the per-DBMS
// namespaces hit) — a finding in its own right.
using Mss = SqlArtisan.Typed.SqlServer;
using Ora = SqlArtisan.Typed.Oracle;
using TSql = SqlArtisan.Typed.Sql;

namespace SqlArtisan.Tests;

// Phantom-type experiment: one namespace, DBMS as a type argument. These positive
// tests show typed queries build correctly; the negative (must-not-compile) cases
// — cross-DBMS mixing and calling an unsupported function — are proven separately
// by scratch compiles recorded in the spike README.
public class PhantomTypeTests
{
    [Fact]
    public void Universal_Abs_BuildsForOracle()
    {
        SqlStatement sql = TSql.Select(TSql.Abs<Ora>(1)).Build();
        Assert.Equal("SELECT ABS(:0)", sql.Text);
    }

    [Fact]
    public void Ceil_BuildsForOracle()
    {
        SqlStatement sql = TSql.Select(TSql.Ceil<Ora>(1)).Build();
        Assert.Equal("SELECT CEIL(:0)", sql.Text);
    }

    [Fact]
    public void Ceiling_BuildsForSqlServer()
    {
        SqlStatement sql = TSql.Select(TSql.Ceiling<Mss>(1)).Build();
        Assert.Equal("SELECT CEILING(@0)", sql.Text);
    }

    [Fact]
    public void SameDbms_ItemsCombineFreely()
    {
        // Two Oracle-tagged expressions in one Oracle query: allowed.
        SqlStatement sql = TSql.Select(TSql.Abs<Ora>(1), TSql.Ceil<Ora>(2)).Build();
        Assert.Equal("SELECT ABS(:0), CEIL(:1)", sql.Text);
    }

    [Fact]
    public void PortableColumn_MustBeLiftedIntoTheTargetDbms()
    {
        TestTable t = new();
        // A portable column does not carry a TDbms; it must be lifted via Of<TDbms>.
        SqlStatement sql = TSql.Select(TSql.Of<Ora>(t.Code), TSql.Abs<Ora>(1)).Build();
        Assert.Equal("SELECT code, ABS(:0)", sql.Text);
    }
}
