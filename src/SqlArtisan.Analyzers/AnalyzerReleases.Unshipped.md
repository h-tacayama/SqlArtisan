; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SQLA0001 | SqlArtisan.Dialect | Warning | A SqlArtisan analyzer .editorconfig value could not be recognized.
SQLA0002 | SqlArtisan.Dialect | Warning | A SqlArtisan construct is used against a configured target dialect it is not supported on.
SQLA0003 | SqlArtisan.Dialect | Warning | A SqlArtisan construct is used in a syntactic position the target dialect rejects it in.
SQLA0004 | SqlArtisan.Dialect | Warning | A SQL identifier literal exceeds the target dialect's identifier-length limit.
SQLA0005 | SqlArtisan.Dialect | Warning | A correlated UPDATE or DELETE has an unaliased target — the same violation Build() rejects.
SQLA0006 | SqlArtisan.Dialect | Warning | A construct's minimum engine version, per the matrix's version bounds, exceeds the declared sqlartisan_target_version.
