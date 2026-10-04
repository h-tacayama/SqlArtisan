using System.Text;

namespace SqlArtisan.Internal;

internal static class RegexpOptionsExtensions
{
    // Read from the enum so an appended flag is covered without an edit here.
    private static readonly RegexpOptions s_defined = DefinedFlags();

    internal static RegexpOptionsValue ToValue(this RegexpOptions options) =>
        new(options);

    internal static string ToSql(this RegexpOptions options)
    {
        // An undefined bit emits no letter today and would start emitting one
        // once a flag is appended at that bit, so it is rejected rather than dropped.
        if ((options & ~s_defined) != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options), options, "The value is not a defined RegexpOptions combination.");
        }

        StringBuilder result = new();
        result.Append('\'');

        if (options.HasFlag(RegexpOptions.CaseSensitive))
        {
            result.Append('c');
        }

        if (options.HasFlag(RegexpOptions.CaseInsensitive))
        {
            result.Append('i');
        }

        if (options.HasFlag(RegexpOptions.MultipleLines))
        {
            result.Append('m');
        }

        if (options.HasFlag(RegexpOptions.NewLine))
        {
            result.Append('n');
        }

        if (options.HasFlag(RegexpOptions.ExcludingWhiteSpace))
        {
            result.Append('x');
        }

        result.Append('\'');
        return result.ToString();
    }

    private static RegexpOptions DefinedFlags()
    {
        RegexpOptions defined = RegexpOptions.None;
        foreach (RegexpOptions flag in Enum.GetValues<RegexpOptions>())
        {
            defined |= flag;
        }

        return defined;
    }
}
