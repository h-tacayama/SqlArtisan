namespace SqlArtisan;

/// <summary>
/// The parameters bound by a built <see cref="SqlStatement"/>, each a marker name paired with its
/// value, in bind order.
/// </summary>
public sealed class SqlParameters
{
    private readonly List<KeyValuePair<string, BindValue>> _parameters;

    internal SqlParameters(List<KeyValuePair<string, BindValue>> parameters)
    {
        _parameters = parameters;
    }

    /// <summary>
    /// Gets the number of bound parameters.
    /// </summary>
    public int Count => _parameters.Count;

    /// <summary>
    /// Gets the parameter marker names, in bind order.
    /// </summary>
    public IEnumerable<string> ParameterNames => _parameters.Select(p => p.Key);

    /// <summary>
    /// Invokes <paramref name="action"/> once per parameter, in bind order, with its marker name
    /// and value.
    /// </summary>
    /// <param name="action">The callback receiving each parameter's name and value.</param>
    public void ForEach(Action<string, BindValue> action)
    {
        foreach (KeyValuePair<string, BindValue> parameter in _parameters)
        {
            action(parameter.Key, parameter.Value);
        }
    }

    /// <summary>
    /// Gets the value of the named parameter, cast to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="name">The parameter marker name to look up.</param>
    /// <returns>The value cast to <typeparamref name="T"/>, or <see langword="default"/> if no parameter has that name.</returns>
    /// <remarks>
    /// A missing name is not an error, so for a value type <typeparamref name="T"/> it reads
    /// the same as a bound <see langword="default"/>. Check <see cref="ParameterNames"/> to
    /// test whether a parameter exists.
    /// </remarks>
    /// <exception cref="InvalidCastException">The stored value cannot be cast to <typeparamref name="T"/>. A stored SQL <c>NULL</c> is held as <see cref="DBNull"/>, so it throws unless <typeparamref name="T"/> is a type <see cref="DBNull"/> converts to, such as <see cref="object"/>.</exception>
    public T? Get<T>(string name)
    {
        foreach (KeyValuePair<string, BindValue> parameter in _parameters)
        {
            if (parameter.Key == name)
            {
                return (T?)parameter.Value.Value;
            }
        }

        return default;
    }
}
