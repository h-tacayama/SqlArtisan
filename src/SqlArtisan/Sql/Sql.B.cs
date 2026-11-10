using System.Data;
using SqlArtisan.Internal;
using static SqlArtisan.Internal.ExpressionResolver;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// An explicit bind-parameter handle for <paramref name="value"/>, emitted as
    /// a marker (<c>:0</c>, <c>:1</c>, …) assigned at <c>Build()</c>.
    /// </summary>
    /// <param name="value">The value to bind. Accepts the same types as an auto-bound literal, and also a <see cref="DateTimePart"/>, <see cref="RegexpOptions"/> or <see cref="SearchModifier"/>, which a value position rejects.</param>
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
            throw new ArgumentNullException(nameof(value), BindValue.NullValueMessage);
        }

        // ADO.NET's NULL has its own remedy, so it leaves the invalid-type message.
        if (value is DBNull)
        {
            throw new ArgumentException(
                "DBNull.Value cannot be bound; use Sql.BindNull to bind SQL NULL.");
        }

        if (!IsBindable(value))
        {
            throw UnresolvableValue(nameof(Bind), value);
        }

        return IsListed(value) ? new BindValue(value) : ValueConverters.Convert(value);
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
            throw new ArgumentNullException(nameof(values), BindValue.NullValueMessage);
        }

        ThrowIfUnbindableElement<T>();

        return new BindArrayValue(values);
    }

    /// <inheritdoc cref="BindArray{T}(T[])"/>
    public static BindArrayValue BindArray<T>(IReadOnlyCollection<T> values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values), BindValue.NullValueMessage);
        }

        ThrowIfUnbindableElement<T>();

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

    /// <summary>
    /// An explicit bind-parameter handle for a SQL <c>NULL</c>, emitted as a
    /// marker (<c>:0</c>, <c>:1</c>, …) assigned at <c>Build()</c>.
    /// </summary>
    /// <returns>A bind-parameter handle for a SQL <c>NULL</c>, its data type left
    /// to the driver to infer.</returns>
    /// <remarks>
    /// Unlike a bare <see langword="null"/> literal, which inlines the <c>NULL</c>
    /// keyword directly into the SQL text (see <c>Values(...)</c>), this reserves
    /// a real parameter — the statement's shape stays identical whether or not
    /// this value is null. <c>Bind(null)</c> still throws; reach for this
    /// factory when you deliberately want a bound <c>NULL</c>.
    /// </remarks>
    public static BindValue BindNull() => new(DBNull.Value, null);

    /// <inheritdoc cref="BindNull()"/>
    /// <param name="dbType">The data type the parameter is bound as.</param>
    /// <returns>A bind-parameter handle for a SQL <c>NULL</c> of
    /// <paramref name="dbType"/>.</returns>
    public static BindValue BindNull(DbType dbType) => new(DBNull.Value, dbType);

    // The array binds as it is, so a converter registered for the element type does not
    // reach it: an empty array has no element to learn the converted type from (#810).
    private static void ThrowIfUnbindableElement<T>()
    {
        if (IsBindableType(typeof(T)))
        {
            return;
        }

        Type type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        throw new ArgumentException(ValueConverters.Has(type)
            ? $"BindArray does not apply the value converter for {type}; "
                + "pass an array of the values it wraps."
            : $"Invalid element type for BindArray: {typeof(T)}");
    }
}
