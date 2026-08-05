export const meta = {
  name: 'sa-audit-sweep',
  description: 'Chunked audit of the codebase as it stands — globs the scope, fans the files out to sa-reviewer in parallel, adversarially verifies each chunk, then synthesizes. Whole-scope only; for a branch diff use the sa-review skills.',
  phases: [
    { title: 'Scope', model: 'haiku', detail: 'Glob the audit scope (cheap, mechanical)' },
    { title: 'Gates', model: 'haiku', detail: 'Run build/test/format gates once, up front' },
    { title: 'Orchestrate', model: 'fable', detail: 'Classify files and assign review dimensions' },
    { title: 'Execute', model: 'sonnet', detail: 'Deep review per file chunk via sa-reviewer' },
    { title: 'Verify', model: 'sonnet', detail: 'Adversarially verify each chunk\'s findings against primary sources' },
    { title: 'Synthesize', model: 'fable', detail: 'Integrate verified findings and produce final verdict' },
  ],
}

// ---------------------------------------------------------------------------
// PHASE 1: Scope — mechanical globbing, cheapest model in the fleet.
// ---------------------------------------------------------------------------
phase('Scope')

// Callers occasionally pass args as a JSON-encoded string rather than a
// live object; parse defensively instead of trusting the caller's encoding.
const runArgs = typeof args === 'string' ? JSON.parse(args) : (args ?? {})

log(`args received: ${JSON.stringify(runArgs)}`)

const SCOPE_SCHEMA = {
  type: 'object',
  properties: {
    scopeLabel: { type: 'string' },
    files: { type: 'array', items: { type: 'string' } },
  },
  required: ['files'],
}

// `SqlBuilder/**` and `SqlPart/**` appear twice, deliberately: the
// `Internal/`-prefixed path is implementation detail, the bare path is the
// public surface (ADR 0005) — don't collapse them into one glob. Each pattern
// below must be non-overlapping with the others: the Scope agent globs them
// independently and concatenates, so an overlap would hand the orchestrator
// a file list with duplicates baked in before it ever partitions
// anything.
const FULL_CODEBASE_GLOBS = [
  'src/SqlArtisan/Sql/*.cs',
  'src/SqlArtisan/Internal/SqlPart/**',
  'src/SqlArtisan/Internal/SqlBuilder/**',
  'src/SqlArtisan/Internal/Extensions/**',
  'src/SqlArtisan/SqlBuilder/**',
  'src/SqlArtisan/SqlPart/**',
  'src/SqlArtisan/Metadata/**',
  'src/SqlArtisan.ArrayBind/**',
  'src/SqlArtisan.Dapper/**',
  'src/SqlArtisan.TableClassGen/**',
  'src/SqlArtisan.Analyzers/**',
  'tests/SqlArtisan.Tests/**',
  'tests/SqlArtisan.Benchmark/**',
  'tests/SqlArtisan.Analyzers.Tests/**',
  'tests/SqlArtisan.IntegrationTests/**',
  'tests/SqlArtisan.TableClassGen.Tests/**',
]

// This workflow audits the tree as it stands; it never reads a diff. args.paths
// narrows the sweep to a slice (one Layout-table layer, say) — cheaper than the
// default and the recommended way to run it at all, given the whole-codebase
// scale warned about further down.
const scopeLabel = runArgs.paths ? 'paths' : 'fullCodebase'
const scopeGlobs = runArgs.paths ?? FULL_CODEBASE_GLOBS

const scopeInfo = await agent(
  `Use Glob (not git — this audit ignores what the branch changed) to list
every file matching these patterns:
${scopeGlobs.map((p) => `- ${p}`).join('\n')}
Exclude bin/ and obj/ build output. Report scopeLabel="${scopeLabel}" and
return the full list as files.`,
  {
    model: 'haiku',
    effort: 'low',
    label: 'scope-detection',
    phase: 'Scope',
    schema: SCOPE_SCHEMA,
  }
)

log(`Scope: ${scopeLabel}, ${scopeInfo.files.length} file(s)`)

// Nothing to audit — skip straight to a report instead of spending
// Gates/Orchestrate/Execute/Synthesize on an empty file list.
if (scopeInfo.files.length === 0) {
  log('No files in scope — nothing to audit, skipping the remaining phases')
  return {
    scope: scopeLabel,
    gates: null,
    chunksReviewed: '0/0 (skipped — no files in scope)',
    highRiskFiles: [],
    finalReport: `# SqlArtisan Audit: ${scopeLabel}

## Verdict
Clean

## Summary
No files matched the audit scope (empty glob match) — nothing to audit.

## Findings by Severity
None — no files in scope.

## Coverage
- Scope: ${scopeLabel}
- Files in scope: 0
- Gates/Orchestrate/Execute/Verify/Synthesize: skipped (empty scope)

## Recommendations (ranked)
1. No action needed.`,
  }
}

// ---------------------------------------------------------------------------
// PHASE 2: Gates — run once, up front, so reviewers don't re-derive
// failures the toolchain already catches (sa-review skill, step 2).
// ---------------------------------------------------------------------------
phase('Gates')

const GATES_SCHEMA = {
  type: 'object',
  properties: {
    buildPassed: { type: 'boolean' },
    testsPassed: { type: 'boolean' },
    formatClean: { type: 'boolean' },
    summary: { type: 'string' },
  },
  required: ['buildPassed', 'testsPassed', 'formatClean', 'summary'],
}

// The sweep can reach Analyzers/TableClassGen/Dapper/ArrayBind, so the
// core-only build+test the sa-review skill runs for a diff is not enough
// of a gate here — a red test in one of those projects would otherwise go
// undetected while its source still gets audited as if it passed CI.
const gates = await agent(
  `Run the SqlArtisan review gates (sa-review skill, step 2) and report
pass/fail for each. Do not fix anything — detection only.

dotnet build SqlArtisan.sln -c Release
dotnet test tests/SqlArtisan.Tests
dotnet test tests/SqlArtisan.Analyzers.Tests
dotnet test tests/SqlArtisan.TableClassGen.Tests
dotnet format SqlArtisan.sln --verify-no-changes

0 warnings is the bar for the build (AnalysisMode=Recommended, including
CS1574 cref resolution) — with one named exception: a SourceLink warning
reading "Source control information is not available" is a known artifact
of a sandboxed git remote that isn't a real github.com host, not a
code-quality issue. Disregard only that exact warning; anything else still
counts against the bar. Summarize any failure in one or two lines (if
multiple test suites ran, name which one failed).`,
  { model: 'haiku', effort: 'low', label: 'gates', phase: 'Gates', schema: GATES_SCHEMA }
)

log(`Gates: build=${gates.buildPassed} test=${gates.testsPassed} format=${gates.formatClean}`)

// A failing gate is itself a MUST FIX (sa-review skill: "a finding
// the tools already catch is wasted review budget"). Short-circuit before
// the expensive Orchestrate/Execute phases instead of spending them on code
// that may not even compile.
if (!gates.buildPassed || !gates.testsPassed || !gates.formatClean) {
  log('Gates failed — skipping Orchestrate/Execute/Synthesize and reporting the gate failure directly')
  return {
    scope: scopeLabel,
    gates,
    chunksReviewed: '0/0 (skipped — gates failed)',
    highRiskFiles: [],
    finalReport: `# SqlArtisan Audit: ${scopeLabel}

## Verdict
Not clean

## Summary
A gate failed before the audit began. Per the sa-review skill, a
tool-catchable failure is a MUST FIX on its own, and auditing further code
before it's fixed wastes review budget — the deep-audit phases were skipped.

## Findings by Severity

### MUST FIX
- Gate failure: ${gates.summary}

## Coverage
- Scope: ${scopeLabel}
- Files in scope: ${scopeInfo.files.length}
- Gates: build=${gates.buildPassed} test=${gates.testsPassed} format=${gates.formatClean}
- Orchestrate/Execute/Verify/Synthesize: skipped (fail-fast on gate failure)

## Recommendations (ranked)
1. Fix the failing gate(s) above, then re-run this workflow.`,
  }
}

// ---------------------------------------------------------------------------
// PHASE 3: Orchestrate — Fable classifies files and assigns dimensions.
// This is the one stage where the expensive model earns its keep: a
// misclassification here silently narrows every downstream review.
// ---------------------------------------------------------------------------
phase('Orchestrate')

// Shared by the schema (so a typo'd dimension fails validation) and the
// orchestrate prompt below — prevents drift within this file only.
// sa-review-orchestrator.md keeps its own hand-copied list; keep it in sync
// manually when this array changes.
const REVIEW_DIMENSIONS = [
  'adr-conformance',
  'api-design',
  'sql-style',
  'dbms-safety',
  'comment-quality',
  'guard-handling',
  'allocation-budget',
  'test-adequacy',
]

const FILE_GROUP_SCHEMA = {
  type: 'object',
  properties: {
    category: { type: 'string' },
    files: { type: 'array', items: { type: 'string' } },
    priority: { type: 'string', enum: ['high', 'medium', 'low'] },
    reviewDimensions: { type: 'array', items: { type: 'string', enum: REVIEW_DIMENSIONS } },
  },
  required: ['category', 'files', 'priority', 'reviewDimensions'],
}

const PLAN_SCHEMA = {
  type: 'object',
  properties: {
    fileGroups: { type: 'array', items: FILE_GROUP_SCHEMA },
    highRiskFiles: { type: 'array', items: { type: 'string' } },
    estimatedComplexity: { type: 'string', enum: ['high', 'medium', 'low'] },
  },
  required: ['fileGroups'],
}

const orchestratePrompt = `Classify these files and assign review dimensions.

SCOPE: ${scopeLabel}
FILES: ${JSON.stringify(scopeInfo.files)}
GATES: ${gates.summary}

Group by role:
1. Public API (Sql.*.cs) -> naming, API design, documentation alignment
2. Function implementations (Internal/SqlPart/Expression/Function/**) -> ADR
   conformance, DBMS grammar safety, allocation
3. Builders (Internal/SqlBuilder/**) -> SQL style, spacing, allocation budget
4. Tests (tests/**) -> coverage, exact-SQL assertions, guard tests, hazard
   shapes
5. Infrastructure (anything else touched) -> as appropriate

For each group's reviewDimensions, pick from: ${REVIEW_DIMENSIONS.join(', ')}.

Flag 2-3 highRiskFiles (recent core-logic changes, multiple ADR touchpoints,
or allocation-sensitive paths). Only include groups that actually have files
in FILES.`

const plan = await agent(orchestratePrompt, {
  agentType: 'sa-review-orchestrator',
  phase: 'Orchestrate',
  schema: PLAN_SCHEMA,
})

// Enforce the orchestrator spec's "highRiskFiles must be a subset of the
// input" constraint here, symmetric with the fileGroups partition check
// below — the spec states the contract but can't enforce it itself.
const scopeFilesSet = new Set(scopeInfo.files)
const highRiskFiles = (plan.highRiskFiles ?? []).filter((f) => scopeFilesSet.has(f))
const invalidHighRiskFiles = (plan.highRiskFiles ?? []).filter((f) => !scopeFilesSet.has(f))
if (invalidHighRiskFiles.length > 0) {
  log(`Note: orchestrator flagged ${invalidHighRiskFiles.length} highRiskFiles(s) not in scope — dropped: ${invalidHighRiskFiles.join(', ')}`)
}

log(`Plan: ${plan.fileGroups.length} group(s), complexity=${plan.estimatedComplexity ?? 'n/a'}`)

// ---------------------------------------------------------------------------
// Chunk each group so a single Sonnet call never has to hold more than a
// handful of files in deep-review context — matters most in fullCodebase
// mode, where "Public API" alone can span 20+ files.
// ---------------------------------------------------------------------------
const CHUNK_SIZE = 5
const reviewUnits = []
for (const group of plan.fileGroups) {
  const files = group.files ?? []
  if (files.length === 0) continue
  if (files.length <= CHUNK_SIZE) {
    reviewUnits.push({ ...group, chunkFiles: files, chunkLabel: group.category })
  } else {
    let part = 1
    for (let i = 0; i < files.length; i += CHUNK_SIZE) {
      reviewUnits.push({
        ...group,
        chunkFiles: files.slice(i, i + CHUNK_SIZE),
        chunkLabel: `${group.category} (part ${part++})`,
      })
    }
  }
}

// Synthesize concatenates every chunk's full verified review text into one
// prompt (see below) — at full-codebase scale that can exceed a single
// agent's context. There's no good in-workflow fix short of a map-reduce
// synthesis this task doesn't warrant; surface the risk instead of eating it
// silently, and point at the escape hatch (args.paths) that already exists.
const SYNTHESIS_CHUNK_WARNING_THRESHOLD = 40
if (reviewUnits.length > SYNTHESIS_CHUNK_WARNING_THRESHOLD) {
  log(`Warning: ${reviewUnits.length} chunks in a fullCodebase run — Synthesize concatenates every
chunk's full review text into one prompt and may exceed context at this scale. Consider re-running
with args.paths scoped to one layer of CLAUDE.md's Layout table at a time instead of the full sweep.`)
}

// A length subtraction alone only catches under-coverage and can go
// negative on duplicates; diff the actual file sets so both are caught.
const assignedFiles = reviewUnits.flatMap((u) => u.chunkFiles)
const assignedCounts = new Map()
for (const f of assignedFiles) assignedCounts.set(f, (assignedCounts.get(f) ?? 0) + 1)

const missingFiles = scopeInfo.files.filter((f) => !assignedCounts.has(f))
const duplicateFiles = [...assignedCounts].filter(([, n]) => n > 1).map(([f]) => f)
const coverageClean = missingFiles.length === 0 && duplicateFiles.length === 0

if (!coverageClean) {
  log(`Coverage issue: ${missingFiles.length} file(s) missing, ${duplicateFiles.length} file(s) duplicated across groups — see synthesis`)
}

// ---------------------------------------------------------------------------
// PHASE 4+5: Execute, then adversarially Verify — one pipeline, no barrier:
// each chunk's verification starts as soon as its review lands. Execute runs
// via the sa-reviewer agent so every chunk inherits its read-only tool
// restriction and its pointer to the sa-review / sa-run-sql-harness
// skills, instead of re-deriving (and risking drift from) that procedure
// inline in this prompt. Verify re-enters sa-reviewer on its
// adversarial-verification mission (refute, don't confirm) so no finding
// reaches synthesis unchallenged.
// ---------------------------------------------------------------------------
phase('Execute')

const reviewResults = await pipeline(
  reviewUnits,
  (unit) =>
    agent(
      `Deep-review this SqlArtisan file group as part of a larger multi-group
pass. Gates already ran and passed/failed as follows — do not re-run them:
${gates.summary}

GROUP: ${unit.category}${unit.chunkLabel !== unit.category ? ` — ${unit.chunkLabel}` : ''}
PRIORITY: ${unit.priority}
FILES (review exactly these, nothing else):
${unit.chunkFiles.map((f) => `- ${f}`).join('\n')}

DIMENSIONS TO APPLY:
${unit.reviewDimensions.map((d) => `- ${d}`).join('\n')}

Follow the sa-review skill's checklist for whichever of these
dimensions apply, and use the sa-run-sql-harness skill for any empirical
verification (DBMS grammar, guard enforcement, allocation) — do not assume
emitted SQL or allocation behavior from memory. Skip the skill's own gate
step (already covered above) and skip re-scoping the diff (file list is
fixed above); otherwise follow it end to end. Skip the skill's adversarial
verification pass too — this workflow runs it as its own Verify stage on
your report.

Separate MUST FIX (bugs, ADR violations, invalid/wrong SQL, missing guards)
from SHOULD DISCUSS (convention trade-offs, coverage gaps, doc drift) and
NITS. Cite file:line and, for any DBMS-grammar or allocation claim, the
verbatim probe output that backs it.`,
      {
        agentType: 'sa-reviewer',
        model: 'sonnet',
        effort: 'high',
        label: `review:${unit.chunkLabel}`,
        phase: 'Execute',
      }
    ),
  // Verify stage. A null review (skipped/failed chunk) passes through so the
  // failed-chunk accounting below still sees it; a null *verifier* result
  // falls back to the unverified review — losing verification must not lose
  // the review itself.
  async (review, unit) => {
    if (!review) return null
    const verified = await agent(
      `Adversarial-verification mission (see your "Adversarial-verification
missions" section): try to REFUTE this chunk review, not confirm it.

FILES the review covered:
${unit.chunkFiles.map((f) => `- ${f}`).join('\n')}

CHUNK REVIEW UNDER TEST:
${review}

For each finding: attempt to refute it against primary sources — the code
itself, test catalogs, ADRs, or a live /tmp harness probe — never the
review's own text. Re-output the full review with every finding annotated:
- CONFIRMED — with the evidence that survived refutation (verbatim probe
  output or the primary source's file:line)
- REFUTED — with the disproving evidence
Then add, as extra findings, any factual claim in the reviewed files
themselves that the review missed and that falls to refutation, classified
DEFECT / OVERREACH / INCONSISTENCY with severity and evidence.`,
      {
        agentType: 'sa-reviewer',
        model: 'sonnet',
        effort: 'high',
        label: `verify:${unit.chunkLabel}`,
        phase: 'Verify',
      }
    )
    return verified
      ? verified
      : `(adversarial verification unavailable for this chunk — unverified review follows)\n${review}`
  }
)

const reviewedUnits = reviewResults.filter(Boolean)
const failedChunks = reviewUnits.filter((u, i) => !reviewResults[i]).map((u) => u.chunkLabel)
const unverifiedChunks = reviewUnits.filter((u, i) =>
  reviewResults[i]?.startsWith('(adversarial verification unavailable')
).map((u) => u.chunkLabel)
log(`Execution complete: ${reviewedUnits.length}/${reviewUnits.length} chunk(s) reviewed`
  + (unverifiedChunks.length > 0 ? `, ${unverifiedChunks.length} unverified` : ' and adversarially verified'))
if (failedChunks.length > 0) {
  log(`Chunk failure: ${failedChunks.length} chunk(s) returned no result — files in them were never reviewed: ${failedChunks.join(', ')}`)
}
if (unverifiedChunks.length > 0) {
  log(`Verification gap: ${unverifiedChunks.length} chunk(s) fell back to an unverified review — see Coverage: ${unverifiedChunks.join(', ')}`)
}

// ---------------------------------------------------------------------------
// PHASE 6: Synthesize — Fable integrates findings into one report.
// ---------------------------------------------------------------------------
phase('Synthesize')

const synthesisPrompt = `Synthesize ${reviewUnits.length} chunk reviews of a
SqlArtisan audit (scope: ${scopeLabel}) into one report.
Each chunk went through adversarial verification, except any listed below as
unverified (its review stands as drafted, unchallenged).

GATES: ${gates.summary}
${!coverageClean ? `
COVERAGE GAP — call this out explicitly in the report:
${missingFiles.length > 0 ? `- Missing (in scope, never assigned to a group): ${missingFiles.join(', ')}\n` : ''}${duplicateFiles.length > 0 ? `- Duplicated (assigned to more than one group, reviewed redundantly): ${duplicateFiles.join(', ')}\n` : ''}` : ''}${failedChunks.length > 0 ? `
CHUNK FAILURE — call this out explicitly in the report:
- ${failedChunks.length} chunk(s) returned no result and were never reviewed: ${failedChunks.join(', ')}
` : ''}${unverifiedChunks.length > 0 ? `
UNVERIFIED CHUNKS — call this out explicitly in Coverage; treat their findings as unconfirmed:
- ${unverifiedChunks.join(', ')}
` : ''}
CHUNK REVIEWS (verified chunks have findings annotated CONFIRMED/REFUTED,
plus any extra DEFECT/OVERREACH/INCONSISTENCY findings the verifier added;
unverified chunks carry the "(adversarial verification unavailable...)" marker):
${reviewUnits.map((u, i) => `--- ${u.chunkLabel} ---\n${reviewResults[i] ?? '(this chunk failed to return a result — its files were never reviewed)'}`).join('\n\n')}

Tasks:
1. Merge findings across chunks; surface cross-chunk patterns (e.g. the same
   naming issue in both Public API and Tests) rather than listing duplicates.
2. Drop REFUTED findings from the verdict — list them briefly in a
   "Refuted in verification" note so the exclusion is visible, never
   silent. Route the verifiers' extra DEFECT / OVERREACH / INCONSISTENCY
   findings by severity: DEFECT to MUST FIX; OVERREACH and INCONSISTENCY to
   MUST FIX or SHOULD DISCUSS. A chunk marked "(adversarial verification
   unavailable...)" was never verified — say so in Coverage and treat its
   findings as unverified.
3. Prioritize: MUST FIX > SHOULD DISCUSS > NITS.
4. Decide a verdict: Clean / Clean after must-fix / Not clean. A failing gate
   above is itself a MUST FIX and blocks "Clean" — and so is a coverage gap
   (a missing or duplicated file above) and a chunk failure (a chunk above
   that never returned a result): both mean files in scope were silently
   never audited, which is exactly the kind of silent failure this workflow
   exists to catch, so treat either the same as a failing gate.

Output as a headed report:

# SqlArtisan Audit: ${scopeLabel}

## Verdict
...

## Summary
(2-3 sentences)

## Findings by Severity
### MUST FIX
### SHOULD DISCUSS
### NITS

## Coverage
- Scope: ${scopeLabel}
- Chunks reviewed: ${reviewedUnits.length}/${reviewUnits.length}
- Chunks adversarially verified: ${reviewedUnits.length - unverifiedChunks.length}/${reviewedUnits.length}${unverifiedChunks.length > 0 ? ` (unverified: ${unverifiedChunks.join(', ')})` : ''}
- Files in scope: ${scopeInfo.files.length}
- Gates: build=${gates.buildPassed} test=${gates.testsPassed} format=${gates.formatClean}
- Empirical probes actually run (from chunk reviews and verification): ...

## Recommendations (ranked)
1. ...`

const finalReport = await agent(synthesisPrompt, {
  model: 'fable',
  label: 'synthesize',
  phase: 'Synthesize',
})

log('Review synthesis complete')

return {
  scope: scopeLabel,
  gates,
  chunksReviewed: `${reviewedUnits.length}/${reviewUnits.length}`,
  highRiskFiles,
  finalReport,
}
