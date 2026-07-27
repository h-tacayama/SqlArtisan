; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SQLA0001 | SqlArtisan.Dialect | Warning | A SqlArtisan analyzer .editorconfig value could not be recognized.
SQLA0002 | SqlArtisan.Dialect | Warning | A SqlArtisan construct is used against a configured target dialect it is not supported on.
SQLA0003 | SqlArtisan.Dialect | Warning | A construct's minimum engine version, per the matrix's version bounds, exceeds the declared sqlartisan_target_version.
SQLA0004 | SqlArtisan.Dialect | Warning | A SqlArtisan construct is used in a syntactic position the target dialect rejects it in.
SQLA0005 | SqlArtisan.Dialect | Warning | A correlated UPDATE or DELETE has an unaliased target — the same violation Build() rejects.
SQLA0006 | SqlArtisan.Dialect | Warning | A SQL identifier literal exceeds the target dialect's identifier-length limit.
SQLA0007 | SqlArtisan.Schema | Warning | IS NULL / IS NOT NULL on a column the generated table class declares NOT NULL, so the predicate is constant.
SQLA0008 | SqlArtisan.Schema | Warning | NOT IN over a subquery whose selected column is nullable, which matches no rows at all when the subquery yields a NULL.
SQLA0009 | SqlArtisan.Schema | Warning | An INSERT column list omits a column the generated table class declares NOT NULL with no default.
SQLA0010 | SqlArtisan.Schema | Disabled | COUNT of a column the generated table class declares nullable, which counts values rather than rows.
SQLA0011 | SqlArtisan.Schema | Warning | A filter wraps an indexed column in a function or matches it with a leading-wildcard pattern, so no index on it can be used.
