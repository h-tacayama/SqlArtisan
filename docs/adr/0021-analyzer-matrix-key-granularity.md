# ADR 0021 — Dialect-matrix key granularity: member-level by default, arity as a narrowing layer

**Status:** Accepted — refines [ADR 0008](0008-analyzer-override-configuration.md)
on the *matrix* side. ADR 0008 decided the override key has two levels; this
decides which level the shipped matrix's own entries use.

## Context

`DialectMatrix` keys 26 of its 309 `Entries` rows by `(member name, arity)`
and the rest by member name alone. (Its separate `Bounds` version-floor table,
keyed the same way, holds 63 more, 12 of them arity-keyed; #487's "38 of ~378"
counted the two tables as one.) #487 asked whether that split is an accident of
incremental filling — whether every entry should carry arity for
uniformity — and noted that the `MatrixKey` collision caveat makes it a
design question rather than a mechanical fill-in. #491 settled it; this ADR
is the record, so the next reader does not re-derive it.

The question is real because arity looks like free precision: an entry that
names the parameter count is strictly more specific than one that doesn't, and
"more specific" usually reads as "safer" for a guard rail (ADR 0010). It isn't,
for the reasons below.

## Decision

**Member name is the matrix's default key. An arity-qualified entry is a
narrowing layer over a member-level one, entered only where a member's own
overloads genuinely differ in dialect support.** Arity is not made universal.

- **The matrix's key set is a user-facing surface, not an internal index.**
  An entry's key shape decides both directions of the
  `sqlartisan_construct_*` contract: `DialectSupportResolver.MatchMatrixEntry`
  names the key each `SQLA0100`/`SQLA0101` message offers as the way to correct
  it, choosing by whether the match was arity-level, and `ToOverrideKey` renders
  the same shape into `AllOverrideKeys` — the recognized-key list `SQLA0001`
  validates override *values* against. Keying every entry by arity would make
  every message advertise `_arity<N>` and would drop every member-level key
  from the validated set — leaving the tier `docs/analyzer.md` documents as
  "applies to *every* overload, including ones added in a future version"
  advertised nowhere and value-checked nowhere. (A member-level override
  written today keeps resolving either way:
  `DialectSupportResolver.ResolveOverride` reads the user's configuration
  without consulting the matrix. The regression is to the documented surface,
  not to configurations in the wild.)
- **Arity entries override, they do not partition.**
  `DialectMatrix.TryGetEntryFrom` looks up `(name, arity)` first and falls back
  to `(name)`, and most arity rows sit *beside* a member-level row to narrow
  it: `Round` is broadly supported with an arity-1 exception for T-SQL, `Instr`
  is Oracle-only with an arity-2 exception for MySQL/SQLite; `ToNumber` runs
  the other way, Oracle + PostgreSQL at member level with an arity-1 row
  narrowing it to Oracle. Universal arity deletes that fallback, so **a future
  overload of an already-entered member would default to no coverage at all**
  — and
  `DialectMatrixCoverageTests` keys on member name, so the gate would pass in
  silence. That is a coverage regression wearing the costume of an improvement,
  and it runs against ADR 0003's degradable design, which spends silence only
  where nothing has been verified, never where a verdict already exists.
- **Arity is not a signature.** Using the declared parameter count as a
  shape gate is a category error: two overloads with the same arity and
  different parameter types collide into one key (ADR 0008's rejected
  type-based keys; the `Match` caveat in `DialectMatrix`'s class doc), and a
  construct whose dialect support depends on an argument's *value* has no
  arity to distinguish it at all (`Trunc`). Distinguishing call shapes belongs
  to a signature-aware mechanism (#489), not to the matrix's lookup key.

## Consequences

- Adding an arity entry for a member `Entries` already covers keeps it
  covered: the member-level row stays as the fallback. The `Bounds`
  version-floor table does **not** work this way — `TryGetMinVersion` looks the
  matched key up exactly — so a member carrying its bound on the member-level
  key needs a floor on the new arity key too, wherever the two rows' cells
  agree, or that overload silently loses the floor's verdict: a missing
  `SQLA0101` below the dialect's baseline, a false-positive `SQLA0100` above
  it. Record the overload's own version rather than copying the member's —
  `Trim`'s 2-arg form is SQL Server 2022 where the member is 2017, and the
  gate checks that a row is there, not what it says. Where the cells disagree
  the arity row carries a floor of its own or none — the member's value would
  contradict the bool at the baseline, which is the one story the two tables
  are required to tell. `RegexpSubstr`'s arity-6 row documents
  the trap in place, and `EveryArityEntry_ReKeysItsMemberLevelBound` gates it
  (#500) — reading `Entries` → `Bounds`, the direction
  `EveryBound_KeysARealMatrixEntry`'s orphan check does not cover.
- Adding the *first* entry for a member as an arity entry partitions that
  member — every overload not listed falls out of coverage. Six names are in
  that state today (`Concat`, `Date`, `Grouping`, `GroupingId`,
  `IntervalLiteral`, `Log`), each exhaustive against its current overloads.
  `DialectMatrixIntegrityTests.PartitionedMember_CoversEveryPublicOverloadArity`
  gates that exhaustiveness, so an overload with no entry of its own — a new
  one on those six, or one on a seventh partitioned name — fails the build
  rather than going quietly uncovered. The
  user-visible half of the same fact is a `docs/analyzer.md` known limitation.
- The `sqlartisan_construct_<name>` / `sqlartisan_construct_<name>_arity<N>`
  split stays a genuine two-tier contract on both sides: the user writes at
  either level (ADR 0008), and the shipped matrix asserts at either level.
- Uniformity is not a goal for this table. The mixed key shape is the design,
  not debt to be paid down later.
