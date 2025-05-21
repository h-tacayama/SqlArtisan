using SqlArtisan.Internal;

namespace SqlArtisan;

public sealed class SqlParameters
{
    private readonly Dictionary<string, BindValue> _parameters;

    internal SqlParameters(Dictionary<string, BindValue> parameters)
    {
        _parameters = parameters;
    }

    public int Count => _parameters.Count;

    public IEnumerable<string> ParameterNames => _parameters.Select(p => p.Key);

    public void ForEach(Action<string, BindValue> action)
    {
        foreach (var parameter in _parameters)
        {
            action(parameter.Key, parameter.Value);
        }
    }

    public T? Get<T>(string name)
    {
        if (_parameters.TryGetValue(name, out BindValue? value))
        {
            return (T?)value.Value;
        }

        return default;
    }
}
