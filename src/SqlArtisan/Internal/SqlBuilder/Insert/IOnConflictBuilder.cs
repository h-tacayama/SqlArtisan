namespace SqlArtisan.Internal;

// The state after ON CONFLICT (target): the conflict action must be chosen next.
public interface IOnConflictBuilder
{
    ISqlBuilder DoNothing();

    ISqlBuilder DoUpdateSet(params DbColumn[] updateColumns);
}
