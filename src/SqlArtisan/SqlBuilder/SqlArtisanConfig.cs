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
    /// Registers how a value of <typeparamref name="T"/>, such as a value object, binds:
    /// wherever a value binds, it binds as what <paramref name="convert"/> returns.
    /// </summary>
    /// <typeparam name="T">The value's type: a concrete type no value position binds without a converter.</typeparam>
    /// <param name="convert">Returns the value to bind: a type a value position binds, a <see cref="byte"/> array, or a <see cref="BindValue"/> carrying one to set its data type or size.</param>
    /// <remarks>
    /// The converter runs when the value is passed, so it reaches values passed after this
    /// call; registering <typeparamref name="T"/> again replaces it for them.
    /// <see cref="Sql.BindArray{T}(T[])"/> and <c>new BindValue(value)</c> do not convert.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="convert"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> binds without a converter, or is an interface or abstract.</exception>
    public static void AddValueConverter<T>(Func<T, object> convert)
    {
        ArgumentNullException.ThrowIfNull(convert);

        Type type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (ExpressionResolver.IsBindableType(type))
        {
            throw new ArgumentException($"{type} binds without a value converter.");
        }

        // A converter is found by the value's exact type, which is never one of these.
        if (type.IsInterface || type.IsAbstract)
        {
            throw new ArgumentException(
                $"A value converter is found by the value's own type, so {type} "
                    + "must be a concrete type.");
        }

        ValueConverters.Add(type, value => convert((T)value));
    }

    /// <summary>
    /// Removes every converter <see cref="AddValueConverter{T}(Func{T, object})"/> registered.
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
