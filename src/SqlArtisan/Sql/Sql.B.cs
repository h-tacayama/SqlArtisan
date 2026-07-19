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

    /// <summary>
    /// Binds the whole array as one array-typed parameter (PostgreSQL):
    /// <c>col == Any(BindArray(values))</c> emits <c>col = ANY (:0)</c> with a
    /// single parameter. Unlike <c>In(...)</c>, the SQL text is identical for
    /// every list length, and an empty array is legal (matches no element).
    /// </summary>
    /// <param name="values">The array to bind as one parameter.</param>
    /// <returns>A <see cref="BindArrayValue"/> carrying the array.</returns>
    public static BindArrayValue BindArray<T>(T[] values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values), NullValueMessage);
        }

        if (!IsBindableType(typeof(T)))
        {
            throw new ArgumentException($"Invalid element type for BindArray: {typeof(T)}");
        }

        return new BindArrayValue(values);
    }

    /// <inheritdoc cref="BindArray{T}(T[])"/>
    public static BindArrayValue BindArray<T>(IReadOnlyCollection<T> values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values), NullValueMessage);
        }

        if (!IsBindableType(typeof(T)))
        {
            throw new ArgumentException($"Invalid element type for BindArray: {typeof(T)}");
        }

        // Normalized to T[] so the ADO-visible value is deterministic: Npgsql
        // maps arrays natively but not e.g. HashSet<T>.
        T[] copied = new T[values.Count];
        int i = 0;
        foreach (T value in values)
        {
            copied[i++] = value;
        }

        return new BindArrayValue(copied);
    }
}
