using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// An explicit bind-parameter handle for <paramref name="value"/>, emitted as
    /// a marker (<c>:0</c>, <c>:1</c>, …) assigned at <c>Build()</c>.
    /// </summary>
    /// <param name="value">The value to bind. Accepts the same types as an auto-bound literal.</param>
    /// <returns>A bind-parameter handle for <paramref name="value"/>.</returns>
    /// <remarks>
    /// Hold the result in a variable and reuse it across clauses to make each bind
    /// the same marker; two separate <c>Bind(...)</c> calls with equal values still
    /// mint distinct markers.
    /// </remarks>
    public static BindValue Bind(object value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), NullValueMessage);
        }

        if (!IsBindable(value))
        {
            throw UnresolvableValue(nameof(Bind), value);
        }

        return new BindValue(value);
    }
}
