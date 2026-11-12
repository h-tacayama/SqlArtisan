using SqlArtisan.Dapper.Internal;

namespace SqlArtisan.Dapper;

public static partial class SqlMapper
{
    /// <summary>
    /// Registers <paramref name="handler"/> with Dapper and, for a type SqlArtisan takes a value
    /// converter for, with SqlArtisan: a value of <typeparamref name="T"/> then binds as the
    /// value, data type and size the handler's <c>SetValue</c> sets.
    /// </summary>
    /// <typeparam name="T">The type the handler reads and writes.</typeparam>
    /// <param name="handler">The handler; its <c>SetValue</c> runs when the value is passed, on a parameter of SqlArtisan's own, so it must not cast to a provider's parameter type.</param>
    /// <remarks>
    /// Call it in place of Dapper's own <c>AddTypeHandler</c>, which SqlArtisan cannot see. A
    /// type registered with SqlArtisan reaches every execution path converted; a type
    /// <c>SqlArtisanConfig.AddValueConverter</c> rejects registers with Dapper alone.
    /// <para>The value <c>SetValue</c> sets must be one a value position binds, a <c>byte[]</c>
    /// or <c>DBNull</c>, and one Dapper takes; bind a provider's own type, such as pgvector's
    /// <c>Vector</c>, or one Dapper rejects through <c>new BindValue(value)</c>.</para>
    /// <para>A <c>Precision</c> or <c>Scale</c> the handler sets is not carried.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
    public static void AddTypeHandler<T>(global::Dapper.SqlMapper.TypeHandler<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        global::Dapper.SqlMapper.AddTypeHandler(handler);

        // The core's guard decides, never a copy or probe of what binds directly: a type a
        // later SqlArtisan binds by itself stays accepted there, so this keeps converting.
        try
        {
            SqlArtisanConfig.AddValueConverter<T>(value =>
            {
                CaptureParameter parameter = new();
                handler.SetValue(parameter, value);
                return parameter.Captured(typeof(T));
            });
        }
        catch (ArgumentException)
        {
            // Dapper still reads a member declared as a type the core takes no converter for.
        }
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
}
