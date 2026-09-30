namespace SqlArtisan.Internal;

/// <summary>
/// The state after <c>FOR UPDATE</c>: build, or embed the locked query as a subquery or CTE
/// body (MySQL, PostgreSQL; Oracle takes <c>FOR UPDATE</c> only in a top-level <c>SELECT</c>).
/// </summary>
public interface ISelectBuilderForUpdate : ISqlBuilder, ISubquery
{
}
