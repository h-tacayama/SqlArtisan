using System.Text;

namespace SqlArtisan.Internal;

internal static class RegexpOptionsExtensions
{
    internal static RegexpOptionsValue ToValue(this RegexpOptions options) =>
        new(options);

    internal static string ToSql(this RegexpOptions options)
    {
        StringBuilder result = new();
        result.Append("'");

        if (options.HasFlag(RegexpOptions.CaseSensitive))
        {
            result.Append("c");
        }

        if (options.HasFlag(RegexpOptions.CaseInsensitive))
        {
            result.Append("i");
        }

        if (options.HasFlag(RegexpOptions.MultipleLines))
        {
            result.Append("m");
        }

        if (options.HasFlag(RegexpOptions.NewLine))
        {
            result.Append("n");
        }

        if (options.HasFlag(RegexpOptions.ExcludingWhiteSpace))
        {
            result.Append("x");
        }

        result.Append("'");
        return result.ToString();
    }
}
