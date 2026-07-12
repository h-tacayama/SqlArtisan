using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// A bind-parameter expression for <paramref name="value"/>, usable where a
    /// bare literal cannot appear — e.g. <c>Value(1).As("depth")</c> emits
    /// <c>:0 "depth"</c>.
    /// </summary>
    /// <param name="value">The value to bind.</param>
    /// <returns>A <see cref="BindValue"/> expression for <paramref name="value"/>.</returns>
    public static BindValue Value(object value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), NullValueMessage);
        }

        if (!IsBindable(value))
        {
            throw UnresolvableValue(nameof(Value), value);
        }

        return new BindValue(value);
    }
}
