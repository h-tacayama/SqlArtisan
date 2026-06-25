using SqlArtisan.Internal;

namespace SqlArtisan;

public abstract class CteBase(string name) : TableReference(name)
{
    /// <summary>
    /// Binds <paramref name="subquery"/> to this CTE's name, producing a
    /// <c>name AS (subquery)</c> definition to pass to
    /// <see cref="Sql.With(CommonTableExpression[])"/>.
    /// </summary>
    public CommonTableExpression As(ISubquery subquery) => new(_name, subquery);
}
