using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// An explicit bind-parameter handle for <paramref name="value"/>, emitted as
    /// a marker (<c>:0</c>, <c>:1</c>, …) assigned at <c>Build()</c>.
    /// </summary>
    /// <param name="value">The value to bind. Accepts the same types as an
    /// auto-bound literal (see <c>docs/functions.md</c>'s bind-parameter type
    /// list).</param>
    /// <returns>A bind-parameter expression.</returns>
    /// <remarks>
    /// Hold the result in a variable and reuse it across clauses (e.g. <c>SELECT</c>
    /// and <c>GROUP BY</c>) to make both positions bind the same marker; two
    /// separate <c>Bind(...)</c> calls with equal values still mint distinct
    /// markers.
    /// </remarks>
    public static SqlExpression Bind(object value)
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
