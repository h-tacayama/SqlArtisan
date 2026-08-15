# ADR 0020 — Documentation precision boundary: what the docs assert, and what they delegate to the engine

**Status:** Accepted

## Context

One documentation change (#478's follow-up, four commits) went through four
independent review rounds. Each round found a real defect; each defect sat in
the same layer. The pattern is sharper than any individual finding:

| Claim | Rounds survived | In-repo source of truth |
|---|---|---|
| What SqlArtisan emits | all | unit tests, asserting the exact SQL string |
| Which dialects support a construct | all | `DialectMatrix.Entries`, live-verified by `MatrixSweepTests` |
| From which engine version | all | `DialectMatrix.Bounds`, gated by `EveryBound_HasDocsProvenance` |
| **Result semantics** (duplicates, `NULL` matching, multiplicity) | none | **none** |
| **Equivalence between constructs; hand-written SQL rewrites** | none | **none** |

Everything that held had a source of truth in the repository and a gate
keeping the prose tied to it. Everything that broke had neither. The four
defects were not carelessness on any one reviewer's part — the claims were
unfalsifiable from inside the repo, so review could only trade one plausible
assertion for another. Three of the four defects were introduced *by a fix for
the previous round's defect*.

There is a second, deeper reason those sentences kept failing. "Construct X is
unavailable here, so write Y instead" is a **portability claim** — precisely
what ADR 0001 refuses to do in code, and ADR 0010 names as a non-goal. The
prose was offering what the library deliberately withholds, and inherited every
correctness problem that comes with it: `EXCEPT` is not `EXCEPT ALL` minus its
duplicates, a `UNION` of two one-sided joins is not a `FULL JOIN`, and a
`NOT EXISTS` predicate is neither.

`.claude/rules/docs-style.md` actively drove this: its dialect-caveat clause
required "the working alternative in the same breath" and forbade "a bare 'not
supported on X' with no way out." Each review round, the fix obeyed that rule
by inventing another unverified equivalence.

Version floors in the reference pages are the same class of liability without
the same defence. `docs/` carries 44 of them; #477 measured every one as
currently correct, and closed as not planned after finding they cannot be gated:
a doc entry writes `` `Ltrim()` `` with no arity, so it cannot resolve the
matrix's arity-specific keys, and several floors (Oracle 12c `OFFSET/FETCH`,
SQLite 3.25 window functions, pgvector 0.7.0) lie outside the matrix's domain
entirely. Their accuracy today rests on review alone, and every future engine
release is a chance for them to drift silently.

## Decision

**A user-facing document asserts only what this repository can verify.**
Everything else is the engine's contract, and the docs link to the engine's
manual rather than restating it.

### The three tiers

| Tier | Stated in reference docs | Kept true by |
|---|---|---|
| What SqlArtisan emits | yes | unit tests (exact SQL strings) |
| Which dialects support a construct | yes | `DialectMatrix.Entries` + `MatrixSweepTests` |
| Minimum engine version | **no** — see below | — |
| Result semantics; construct equivalence; SQL rewrites | **no** | — |

### A version floor appears only where a gate keeps it synced

Floors are not banned from the repository — they are banned from surfaces where
nothing ties them to `DialectMatrix`:

- **`docs/analyzer.md`'s version-bound register** — every bound, with its
  primary source, gated by `DialectMatrixVersionBoundsTests.EveryBound_HasDocsProvenance`.
  This is the reference-side home for floors.
- **XML `<remarks>`** — gated in both directions by
  `XmlDocDialectParityTests.RemarksVersionFloor_MatchesMatrix` (#471), so a
  bound correction cannot ship without the remark moving with it.
- **The analyzer itself** — `SQLA0101` reports the floor at the call site once
  a target version is declared, which is the mechanism ADR 0003 designed for
  this.

Reference pages state *which dialects*, not *from which version*, and link to
the register for the latter.

### Substitution: point at an API, never at a rewrite

When a construct is unavailable on a dialect, name the **sibling SqlArtisan
API that emits that dialect's own construct** — `Minus` for Oracle's spelling
of `EXCEPT`, the `LIMIT` / `OFFSET-FETCH` family for SQL Server's `TOP`. That
is a fact about this library's surface, and the tests cover it.

Never supply a hand-written SQL recipe, and never claim two constructs are
interchangeable. Where no sibling API exists, **"not available there" is a
complete answer** — the reader is better served by a correct full stop than by
a rewrite whose limits the docs cannot state reliably.

### What this does not touch

Hazards about **SqlArtisan's own emitted SQL** stay, in full, under the
existing callout taxonomy: MySQL parsing `||` as logical OR, `NATURAL JOIN`
matching on shared column names as the schema drifts, `Log`'s dialect-defined
base. These describe what our output does on a target engine — ADR 0010's guard
rail, the reason this project exists. They are not claims about how to
substitute one construct for another.

## Rejected alternatives

- **Gate the reference pages' floors against the matrix (#477).** Measured
  infeasible: doc entries carry no arity, so a name-keyed lookup misfires on
  exactly the entries where the docs are most precise (`Trim` states both the
  member floor and the arity-2 floor on one line); 27 of the 44 floors sit in
  prose, headings, or table cells with no entry name to key on; and several
  floors have no matrix row at all, so the gate would pressure prose into
  driving `VersionBounds` — whose bar is live, primary-source provenance.
- **Keep the substitutes and write them more carefully.** Four rounds is the
  evidence against. Each round's author had the previous round's finding in
  hand and still shipped a fresh unverifiable claim; the layer's defect rate is
  a property of it having no source of truth, not of who wrote it.
- **Delegate the emitted-SQL hazards too.** Those are not DBMS-evolution
  tracking — they describe this library's own output, and dropping them would
  cut the guard rail ADR 0010 makes the mission.
- **Keep floors in reference pages, accept review as the gate.** This is the
  status quo that #477 documented and closed. It holds only as long as every
  future reviewer re-derives 44 facts against five engines' release histories.

## Consequences

- `.claude/rules/docs-style.md` changes in two clauses: the dialect-caveat
  note's "working alternative" requirement narrows to a sibling API, and the
  version-boundary note is removed from reference-page prose and redirected to
  the register.
- The 44 existing floor mentions leave `docs/functions.md`,
  `docs/query-statements.md`, `docs/expressions.md`, and `docs/cookbook.md`.
  The register in `docs/analyzer.md` becomes the single reference-side home.
- **Accepted cost:** a reader on a reference page no longer sees a construct's
  floor inline and follows a link for it. In exchange the floor exists once,
  where a test keeps it honest, and the analyzer surfaces it at the call site
  where the reader is actually writing the query.
- `sa-diff-review` and `sa-docs-audit` gain the classification question: does
  this sentence claim an equivalence, supply a rewrite, or state result
  semantics? If so it does not belong in the docs.
- This boundary is what makes documentation review converge: a claim either has
  a source of truth in the repo, or it is not made.
