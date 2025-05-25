using SqlArtisan.Internal;

namespace SqlArtisan;

public static partial class Sql
{
    public static SqlHints Hints(string hints) => new(hints);
}
