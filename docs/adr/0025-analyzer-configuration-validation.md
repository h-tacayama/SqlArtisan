# ADR 0025 — Analyzer configuration validation: a line that configures nothing is reported

**Status:** Accepted

## Context

An unconfigured analyzer is silent, so a configuration line that quietly does
nothing reads as a clean build. ADR 0019 and ADR 0024 built `SQLA0001` on that
premise. The #655 audit found four shapes that still slipped past it, and one
record promising a surface that never existed:

- **A `sqlartisan_construct_*` key naming no member or arity** —
  `sqlartisan_construct_rolup`, `sqlartisan_construct_rollup_arity9`, or a key
  for a member a release renamed (#588's `update_where` / `insert_where`).
  ADR 0008 recorded this as a permanent Roslyn limitation. It is not: ADR 0019
  verified that `AnalyzerConfigOptions.Keys` enumerates the configured keys,
  and the analyzer already swept every construct-prefixed key for its *value*.
  A stale `unsupported` key silently loses the warning it asked for.
- **A blank `.editorconfig` `sqlartisan_syntax_*` value.** `.editorconfig`
  layering makes a blank in a narrower section *replace* the broader value, so
  `sqlartisan_syntax_sqlite =` under `[sub/**.cs]` unsets SQLite for those
  files with no report, while `docs/analyzer.md` said only `none` turns a
  dialect back off.
- **A version in another engine's spelling.** `sqlartisan_syntax_sqlserver =
  16` (SQL Server 2022's product version) parsed, compared as "year 16", and
  failed every SQL Server bound; `sqlartisan_syntax_postgresql = 2022` cleared
  every PostgreSQL bound.
- **The all-`none` reason fired on a deliberate carve-out.** A single-dialect
  project writing `none` for one path — the carve-out `docs/analyzer.md`
  recommends — reported `SQLA0001`. The report has no location, so its only
  levers (`<NoWarn>SQLA0001</NoWarn>`, a global severity) silenced typo
  detection with it.
- ADR 0008 promised "an MSBuild property as a secondary path" for the override
  keys. None was ever built: no `CompilerVisibleProperty`, and no
  `build_property` lookup when an override is resolved.

## Decision

**A construct key that names no SqlArtisan member, or no declared parameter
count of one, reports `SQLA0001`.** The valid set is read from the SqlArtisan
assembly the project references: the member key of every member a usage
resolves an override for (public or protected, operators included, enum members
and `SqlParameters` / `SqlStatement` excluded as the usage walk excludes them),
and an arity key for each declared parameter count of a method or operator. It
is not read from the dialect matrix, which leaves members out — a key on `Build`
or on `Concat`'s member level takes effect, so it is not stale. With no
SqlArtisan referenced, nothing is judged stale. `ConstructKeySurfaceTests` pins
the derived set (`Baselines/construct-keys.txt`), so a release that drops a key
users wrote does so deliberately.

**A blank `.editorconfig` `sqlartisan_syntax_*` value reports `SQLA0001`** as an
unrecognized value, and falls through to the MSBuild property like any other.
A blank MSBuild property stays unset: the SDK emits every declared property,
blank when the consumer set none. `none` is the spelling that turns a dialect
off.

**A version is spelled the way the matrix's bounds are: the release year on SQL
Server, the major version (with optional minor and patch) elsewhere.** A SQL
Server value whose first segment is below 2000, or another engine's value whose
first segment is 1000 or more, is unrecognized: it reports `SQLA0001` and leaves
the dialect unset, as any unrecognized value does. Each engine's message names
its own spelling. Mapping `16` to 2022 was rejected: it is an alias, which ADR
0019 keeps out of the value domain, and a table to maintain per release.

**The all-`none` reason reports only when no file in the compilation resolves a
dialect.** Wherever one does, a `none` elsewhere was written on purpose. The
case ADR 0019 named — one `none` and nothing else — still reports; a directory
left all-`none` by mistake beside a configured one does not, which is the cost
of not reporting a carve-out with no way to silence it alone. A dedicated ID was
rejected: still location-less, so silencing it would hide the mistaken case
everywhere too.

**The override keys have no MSBuild surface.** They are read from
`.editorconfig` and global configuration files only.

These reasons stay under `SQLA0001` (ADR 0018's Configuration band, ADR 0019's
splitting test): each is a line the user has to fix, and none is a state anyone
would want to keep while silencing it. Their messages are fixed text per key and
value, never advice derived from one file's configuration (ADR 0024).

## Consequences

- ADR 0008's "key-name typos remain permanently undetectable" and its MSBuild
  secondary path are superseded. ADR 0019's all-`none` reason is narrowed as
  above, and the typo-safety it cites against per-DBMS override keys (Rejected
  alternatives) no longer holds; deferring that axis rests on key-space size
  alone. ADR 0021's description of `AllOverrideKeys` as "the recognized-key list
  `SQLA0001` validates override values against" is out of date: values are
  checked on every configured construct key, names against the assembly, and
  `AllOverrideKeys` is only the fallback when a host cannot enumerate keys.
- A key for a member a newer SqlArtisan adds reports until the project
  upgrades; until then it overrides nothing anyway.
- A project left on `sqlartisan_syntax_sqlserver = 16`, or a year on another
  engine, loses that dialect's checks until it corrects the value — it was
  getting wrong ones before — and is told so.

Related: #655 (this change), ADR 0008 (the override keys), ADR 0019 (the family
and `SQLA0001`'s reasons), ADR 0021 (the matrix's key set), ADR 0024 (fixed
advice in a location-less report).
