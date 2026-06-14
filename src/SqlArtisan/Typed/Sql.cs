using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan.Typed;

// Single facade (one namespace). The DBMS is chosen per call via the type
// argument; capability constraints (where TDbms : ISupportsCeil) make a function
// that a DBMS lacks fail to COMPILE — so both *existence* and *mixing* are
// enforced by the type system, without splitting into per-DBMS namespaces.
public static class Sql
{
    // Universal function: no capability constraint, any DBMS marker.
    public static Expr<TDbms> Abs<TDbms>(object expr) =>
        new(new AbsFunction(Resolve(expr), DbmsTag.Of<TDbms>()));

    // Divergent functions: constrained to markers that declare support.
    public static Expr<TDbms> Ceil<TDbms>(object expr) where TDbms : ISupportsCeil =>
        new(new CeilFunction(Resolve(expr), DbmsTag.Of<TDbms>()));

    public static Expr<TDbms> Ceiling<TDbms>(object expr) where TDbms : ISupportsCeiling =>
        new(new CeilingFunction(Resolve(expr), DbmsTag.Of<TDbms>()));

    // Portability tax: a portable value (column/literal) must be *lifted* into a
    // specific TDbms before it can join that DBMS's query. The same column cannot
    // flow into two different TDbms without being re-lifted.
    public static Expr<TDbms> Of<TDbms>(SqlExpression portable) => new(portable);

    public static TypedQuery<TDbms> Select<TDbms>(params Expr<TDbms>[] items)
    {
        object[] nodes = new object[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            nodes[i] = items[i].Node;
        }

        return new TypedQuery<TDbms>((ISqlBuilder)global::SqlArtisan.Sql.Select(nodes));
    }
}
