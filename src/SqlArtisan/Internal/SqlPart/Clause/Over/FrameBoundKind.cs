namespace SqlArtisan.Internal;

// Ordered so a later member is never a valid frame extent before an earlier
// one — WindowFrameGuard compares these ordinals directly (ADR 0012).
internal enum FrameBoundKind
{
    UnboundedPreceding,
    Preceding,
    CurrentRow,
    Following,
    UnboundedFollowing,
}
