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
    public CommonTableExpression As(ISubquery subquery) => new(_name, subquery);
}
