using SqlArtisan.Internal;

namespace SqlArtisan;

public static partial class Sql
{
    /// <summary>
    /// A <c>n FOLLOWING</c> window-frame bound (offset rows/range after the
    /// current row).
    /// </summary>
    public static FrameBound Following(int offset) => FrameBound.Following(offset);
}
