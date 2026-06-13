using SqlArtisan.Internal;

namespace SqlArtisan;

public sealed class SqlParameters
{
    private readonly List<KeyValuePair<string, BindValue>> _parameters;

    internal SqlParameters(List<KeyValuePair<string, BindValue>> parameters) =>
        _parameters = parameters;

    public int Count => _parameters.Count;

    public IEnumerable<string> ParameterNames => _parameters.Select(p => p.Key);

    public void ForEach(Action<string, BindValue> action)
    {
        foreach (KeyValuePair<string, BindValue> parameter in _parameters)
        {
            action(parameter.Key, parameter.Value);
        }
    }

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
