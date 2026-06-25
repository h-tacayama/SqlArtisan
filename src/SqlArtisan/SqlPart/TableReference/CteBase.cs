using SqlArtisan.Internal;

namespace SqlArtisan;

public abstract class CteBase : TableReference
{
    public CteBase(string name) : base(name)
    {
        Alias = name;
    }

    protected string Alias { get; }

    /// <summary>
    /// Binds <paramref name="subquery"/> to this CTE's name, producing a
    /// <c>name AS (subquery)</c> definition to pass to
    /// <see cref="Sql.With(CommonTableExpression[])"/>.
    /// </summary>
    public CommonTableExpression As(ISubquery subquery) => new(Alias, subquery);
}
