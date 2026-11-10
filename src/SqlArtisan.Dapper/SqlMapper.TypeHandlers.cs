using SqlArtisan.Dapper.Internal;

namespace SqlArtisan.Dapper;

public static partial class SqlMapper
{
    /// <summary>
    /// Registers <paramref name="handler"/> with Dapper, and, for a type no value position
    /// binds by itself, such as a value object, with SqlArtisan too: wherever a value of
    /// <typeparamref name="T"/> binds, it binds as what the handler's <c>SetValue</c> sets.
    /// </summary>
    /// <typeparam name="T">The type the handler reads and writes.</typeparam>
    /// <param name="handler">The handler; its <c>SetValue</c> runs when the value is passed, on a parameter of SqlArtisan's own, so it must not cast to a provider's parameter type.</param>
    /// <remarks>
    /// Call it in place of Dapper's own <c>AddTypeHandler</c>, which SqlArtisan cannot see.
    /// A type a value position binds by itself registers with Dapper alone. The value then
    /// reaches every execution path converted, raw ADO.NET included.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
    public static void AddTypeHandler<T>(global::Dapper.SqlMapper.TypeHandler<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        global::Dapper.SqlMapper.AddTypeHandler(handler);
        if (BindsWithoutConverter<T>())
        {
            return;
        }

        SqlArtisanConfig.AddValueConverter<T>(value =>
        {
            CaptureParameter parameter = new();
            handler.SetValue(parameter, value);
            return parameter.Captured()!;
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
