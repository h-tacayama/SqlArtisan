using SqlArtisan.Internal;

namespace SqlArtisan;

/// <summary>
/// A <c>FOR UPDATE</c> lock behavior — <c>NOWAIT</c>, <c>SKIP LOCKED</c>, or
/// <c>WAIT n</c>, obtained from <c>Nowait</c> / <c>SkipLocked</c> /
/// <c>Wait(n)</c>. Type a variable or parameter as this to select the
/// behavior at runtime.
/// </summary>
public abstract class LockBehaviorBase : SqlPart
{
}
