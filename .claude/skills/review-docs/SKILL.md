---
name: review-docs
description: Review SqlArtisan's user-facing documentation (README, docs/, llms.txt, CHANGELOG) for structure, completeness, accuracy, consistency, links, persuasiveness, and conciseness. Use when asked to review/audit the docs, check the README, validate a docs change before pushing, or confirm docs match the code. Complements review-changes (code) with doc-specific checks and bundles scripts that verify links, API coverage, terminology, and emitted SQL empirically.
---

# Review SqlArtisan documentation

This audits the **docs**, not the code (use `review-changes` / `code-review` for
code). The highest-value moves are **empirical and bidirectional**: verify the
emitted SQL by running the real builder (never from memory), and check coverage
both ways (every public API documented; every documented API real).

Surface map: **README** = landing + capability-map index (shipped in the NuGet
package, rendered on nuget.org); **`docs/`** = reference (`README.md` home +
`query-statements.md` / `expressions.md` / `functions.md`); **`llms.txt`** = AI
index. Conventions live in `.claude/rules/docs-style.md`.

## Run the bundled checks first

From the repo root (`dotnet` SDK required for the last one):

```bash
S=.claude/skills/review-docs/scripts
python3 $S/check_links.py          # anchor/link integrity across README + docs + llms.txt
python3 $S/check_api_coverage.py   # deficiency (undocumented API) + surplus (phantom API)
python3 $S/check_terms.py          # terminology + em-dash + whitespace lint vs docs-style.md
python3 $S/verify_sql_examples.py  # build + run every doc example; compare emitted SQL
```

Each exits non-zero on findings. They are heuristics — read each hit and judge;
do not blind-fix. `verify_sql_examples.py` embeds the table classes the examples
use; if a new example needs a new table/column, extend `SCAFFOLD` there.

## Review dimensions

Score the docs against these. The scripts cover 2–5 and parts of 6/10.

1. **Structure / information architecture.** README section order follows the
   evaluation funnel — *discover → use → deepen → meta* (Why / Key Features /
   Performance → Getting Started / Configuration → Design Philosophy /
   Documentation → Contributing / Changelog / License). README is a lean
   landing + capability map; `docs/` holds the reference. Index/TOC present and
   right-sized; file split neither too coarse nor too fine.

2. **Completeness — no deficiency (code → doc).** Every public `Sql.*` factory,
   builder step, and function is documented. (`check_api_coverage.py`; this is
   how the undocumented `GREATEST`/`LEAST` were caught.)

3. **Completeness — no surplus (doc → code).** No documented API is missing from
   source — no phantom names. Prose mentions and every code block must resolve
   to real APIs. (`check_api_coverage.py` + `verify_sql_examples.py` compiles
   every example.)

4. **Accuracy — output matches the code.** Documented emitted SQL equals the
   real `sql.Text`. Run it. (`verify_sql_examples.py`.) Remember `sql.Text` is a
   single line and a single condition has no wrapping parens; the `//` comments
   are line-wrapped for readability — don't claim "verbatim".

5. **Consistency — terminology & formatting.** Conform to
   `.claude/rules/docs-style.md`: *table class* (not "table schema class"),
   *type-safe* (adj) / *type safety* (noun), *bind parameter* (noun) /
   *bind-parameter* (modifier), *UPSERT* in caps, DBMS display names in prose vs
   `Dbms` enum identifiers in code, DBMS listed in enum order, spaced em dash.
   (`check_terms.py`.)

6. **Links & cross-references.** Anchors resolve; README→docs and docs↔docs use
   **absolute** `blob/main` GitHub URLs (nuget.org does not resolve relative
   links); `llms.txt` uses `raw.githubusercontent.com`; in-page anchors stay
   relative. (`check_links.py`.)

7. **Persuasiveness & honesty (landing copy).** The Why hook lands the arc
   *pain → solution → proof → payoff* and states the promise ("the SQL you write
   is the SQL that runs"). Claims are **scoped and backed** — no overclaim
   ("minimal overhead", not "no overhead"; "fastest builder", scoped to
   builders, not "fastest"); performance claims cite the benchmark suite with
   machine + versions (ADR 0006).

8. **Conciseness — no redundancy.** Section volume is proportionate (watch the
   longest, usually Quick Start). No near-duplicate sentences across sections
   (e.g. the Why close vs a Key Features bullet) — differentiate the angle.

9. **AI-consumption readiness.** `llms.txt` exists and its links track the docs;
   each `docs/` page leads with a design-constraints block; examples are
   deterministic C# → emitted-SQL pairs.

10. **Multi-surface rendering & mechanical hygiene.** Renders cleanly on **both**
    GitHub and **nuget.org** (relative links, no auto-TOC on nuget, `<details>`/
    `<sub>` may be stripped). No trailing whitespace, double blank lines, or
    mojibake. (`check_terms.py` covers whitespace/blank lines.)

## Process safeguards (hard-won)

- **Verify, don't recall.** Emitted SQL, dialect behaviour, and allocations come
  from running the builder, not from memory or the doc's own text.
- **Edit UTF-8-safely.** A `perl -i` substitution corrupted em-dashes here. Use
  Python (`encoding="utf-8"`) or the editing tools; re-scan for mojibake after
  any bulk text rewrite.
- **Lossless moves.** When relocating content, byte-compare the code-fence
  bodies before/after (`git show OLD:file` vs new) so no example is dropped or
  altered; only headings/links should change.
- **Report by severity; confirm editorial calls.** Group findings
  (broken/inaccurate > inconsistent > stylistic). Apply clear mechanical fixes;
  for wording/ordering/trims that are judgment calls, propose options and
  confirm before applying.

## Output

Summarise per dimension (pass / findings), list concrete fixes with file:line,
and state what was verified empirically (links resolve, N/N examples match,
coverage clean). Re-run the relevant script after fixes to confirm green.
