using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// The base class for a typed CTE class: derive from it and bind a body with
/// <see cref="As(ISubquery)"/> to reference the result from a
/// <see cref="Sql.With(CommonTableExpression[])"/>.
/// </summary>
/// <param name="name">The CTE name as it appears in SQL.</param>
public abstract class CteBase(string name) : TableReference(name)
{
    /// <summary>
    /// Binds <paramref name="subquery"/> to this CTE's name, producing a
    /// <c>name AS (subquery)</c> definition to pass to
    /// <see cref="Sql.With(CommonTableExpression[])"/>.
    /// </summary>
    /// <param name="subquery">The subquery to bind as this CTE's body.</param>
    /// <returns>A <see cref="CommonTableExpression"/> pairing this CTE's name with <paramref name="subquery"/>.</returns>
    public CommonTableExpression As(ISubquery subquery) => new(_name, subquery);

    // The CTE name is quoted wherever it is referenced (e.g. `FROM "cte"`) to
    // match the quoted `WITH "cte" AS ...` definition and the quoted column
    // references (`"cte".col`); a bare name would fold case on Oracle and break
    // those references (ORA-00904).
    internal override void Format(SqlBuildingBuffer buffer) =>
        buffer.EncloseInAliasQuotes(_name);
}
