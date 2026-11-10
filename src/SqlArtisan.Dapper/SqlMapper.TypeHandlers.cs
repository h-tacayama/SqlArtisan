using SqlArtisan.Dapper.Internal;

namespace SqlArtisan.Dapper;

public static partial class SqlMapper
{
    /// <summary>
    /// Registers <paramref name="handler"/> with Dapper and, for a type no value position binds
    /// by itself, such as a value object, with SqlArtisan: a value of <typeparamref name="T"/>
    /// then binds as the value, data type and size the handler's <c>SetValue</c> sets.
    /// </summary>
    /// <typeparam name="T">The type the handler reads and writes.</typeparam>
    /// <param name="handler">The handler; its <c>SetValue</c> runs when the value is passed, on a parameter of SqlArtisan's own, so it must not cast to a provider's parameter type.</param>
    /// <remarks>
    /// Call it in place of Dapper's own <c>AddTypeHandler</c>, which SqlArtisan cannot see. A
    /// type it registers with SqlArtisan reaches every execution path converted; one a value
    /// position binds by itself, an interface and an abstract type register with Dapper alone.
    /// <para>A <c>Precision</c> or <c>Scale</c> the handler sets is not carried.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
    public static void AddTypeHandler<T>(global::Dapper.SqlMapper.TypeHandler<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        global::Dapper.SqlMapper.AddTypeHandler(handler);

        // A value is matched by its own type, so a converter for an interface or an
        // abstract type would never run; Dapper still reads a member declared as one.
        if (BindsWithoutConverter<T>() || typeof(T).IsInterface || typeof(T).IsAbstract)
        {
            return;
        }

        SqlArtisanConfig.AddValueConverter<T>(value =>
        {
            CaptureParameter parameter = new();
            handler.SetValue(parameter, value);
            return parameter.Captured(typeof(T));
        });
    }

    /// <summary>
    /// Removes every type handler from Dapper and every value converter from SqlArtisan.
    /// </summary>
    /// <remarks>
    /// Dapper's own <c>ResetTypeHandlers</c> leaves SqlArtisan's converters in place.
    /// </remarks>
    public static void ResetTypeHandlers()
    {
        global::Dapper.SqlMapper.ResetTypeHandlers();
        SqlArtisanConfig.ResetValueConverters();
    }

    // BindArray takes exactly the types a value position binds by itself, an empty array
    // included, so it answers the question without a copy of the core's list.
    private static bool BindsWithoutConverter<T>()
    {
        try
        {
            Sql.BindArray(Array.Empty<T>());
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
