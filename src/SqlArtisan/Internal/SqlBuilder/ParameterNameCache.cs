namespace SqlArtisan.Internal;

// Positional parameter names (":0", ":1", "@0", ...) are highly repetitive, so
// the common low indices are cached per marker and reused instead of formatting
// (and allocating) a fresh string on every parameter.
internal static class ParameterNameCache
{
    private const int CachedCount = 64;

    private static readonly string[] s_colon = Build(':');
    private static readonly string[] s_at = Build('@');
    private static readonly string[] s_question = Build('?');

    internal static string Get(char marker, int index)
    {
        if ((uint)index < CachedCount)
        {
            switch (marker)
            {
                case ':':
                    return s_colon[index];
                case '@':
                    return s_at[index];
                case '?':
                    return s_question[index];
                default:
                    break;
            }
        }

        return Format(marker, index);
    }

    private static string[] Build(char marker)
    {
        string[] names = new string[CachedCount];

        for (int i = 0; i < CachedCount; i++)
        {
            names[i] = Format(marker, i);
        }

        return names;
    }

    private static string Format(char marker, int index) =>
        marker + index.ToInvariantString();
}
