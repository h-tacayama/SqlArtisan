using SqlArtisan.Internal;

namespace SqlArtisan;

public static partial class SqlWordbook
{
    public static SqlHints Hints(string hints) => new(hints);
}
