using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// Library-wide configuration.
/// </summary>
public static class SqlArtisanConfig
{
    /// <summary>
    /// Gets the dialect used when a statement is built without an explicit one (<see
    /// cref="ISqlBuilder.Build()"/>); defaults to <see cref="Dbms.PostgreSql"/>. Change it with
    /// <see cref="SetDefaultDbms(Dbms)"/>.
    /// </summary>
    public static Dbms DefaultDbms { get; private set; } = Dbms.PostgreSql;

    /// <summary>
    /// Registers how a value of <typeparamref name="T"/>, a .NET type of your own, binds:
    /// wherever a value binds, it binds as what <paramref name="convert"/> returns.
    /// </summary>
    /// <typeparam name="T">The value's type: a concrete type no value position binds without a converter.</typeparam>
    /// <param name="convert">Returns the value to bind: a type a value position binds without a converter, a <see cref="byte"/> array, <see langword="null"/> for SQL <c>NULL</c>, which a comparison rejects, or a <see cref="BindValue"/> carrying one to set its data type or size.</param>
    /// <remarks>
    /// It runs when the value is passed, so it reaches values passed after this call, and
    /// registering <typeparamref name="T"/> again replaces it. <c>new BindValue(value)</c> does
    /// not convert; <see cref="Sql.BindArray{T}(T[])"/> rejects an array of a registered type.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="convert"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> binds without a converter, is an interface or abstract, or is an <see cref="object"/> array.</exception>
    public static void AddValueConverter<T>(Func<T, object?> convert)
    {
        ArgumentNullException.ThrowIfNull(convert);

        Type type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (ExpressionResolver.IsDirectlyBindableType(type))
        {
            throw new ArgumentException($"{type} binds without a value converter.");
        }

        if (type.IsInterface || type.IsAbstract)
        {
            throw new ArgumentException(
                $"A value converter is found by the value's own type, so {type} "
                    + "must be a concrete type.");
        }

        if (type == typeof(object[]))
        {
            throw new ArgumentException(
                $"A value converter cannot take {type}: its value cannot be told from a "
                    + "list of values.");
        }

        ValueConverters.Add(type, value => convert((T)value));
    }

    /// <summary>
    /// Removes every converter <see cref="AddValueConverter{T}(Func{T, object?})"/> registered.
    /// </summary>
    public static void ResetValueConverters() => ValueConverters.Clear();

    /// <summary>
    /// Sets the dialect used by parameterless <see cref="ISqlBuilder.Build()"/> calls.
    /// </summary>
    /// <param name="dbms">The engine to use as the default; not <see cref="Dbms.Unknown"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dbms"/> is <see cref="Dbms.Unknown"/> or an undefined value.</exception>
    public static void SetDefaultDbms(Dbms dbms)
    {
        DbmsGuard.ThrowIfUnsupported(dbms);

        DefaultDbms = dbms;
    }
}
