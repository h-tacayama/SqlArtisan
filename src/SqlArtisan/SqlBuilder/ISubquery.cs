using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Marks a builder state whose query can be embedded as a subquery (a derived table, an <c>IN</c>/<c>EXISTS</c> operand, or a scalar subquery). Type a helper method or variable as this to hold a reusable subquery.
/// </summary>
public interface ISubquery
{
    internal void Format(SqlBuildingBuffer buffer);

    /// <summary>
    /// Aliases this subquery as a scalar expression for use in a <c>SELECT</c>
    /// list: <c>(SELECT ...) AS "alias"</c>.
    /// </summary>
    /// <param name="alias">The column alias.</param>
    /// <returns>An aliased scalar-subquery expression.</returns>
    public ExpressionAlias As(string alias) =>
        new(new ScalarSubquery(this), alias);

    /// <summary>
    /// Names this subquery as a derived-table source — <c>(SELECT ...) "alias"</c>
    /// — for a <c>FROM</c>/<c>JOIN</c> or MERGE <c>USING</c>. Read its projected
    /// columns by name with <c>Column(name)</c>.
    /// </summary>
    /// <param name="alias">The derived-table alias.</param>
    /// <returns>A <see cref="SubqueryDerivedTable"/> naming this subquery.</returns>
    public SubqueryDerivedTable AsTable(string alias) =>
        new(this, alias);
}
