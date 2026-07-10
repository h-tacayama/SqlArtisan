export const meta = {
  name: 'sa-multi-model-code-review',
  description: 'Fable orchestrates, Sonnet (via sa-reviewer) executes a deep multi-dimensional SqlArtisan review',
  phases: [
    { title: 'Scope', model: 'haiku', detail: 'Detect branch-point diff (cheap, mechanical)' },
    { title: 'Gates', model: 'haiku', detail: 'Run build/test/format gates once, up front' },
    { title: 'Orchestrate', model: 'fable', detail: 'Classify files and assign review dimensions' },
    { title: 'Execute', model: 'sonnet', detail: 'Deep review per file chunk via sa-reviewer' },
    { title: 'Synthesize', model: 'fable', detail: 'Integrate findings and produce final verdict' },
  ],
}

// ---------------------------------------------------------------------------
// PHASE 1: Scope — mechanical git work, cheapest model in the fleet.
// ---------------------------------------------------------------------------
phase('Scope')

const SCOPE_SCHEMA = {
  type: 'object',
  properties: {
    scope: { type: 'string', enum: ['diff', 'fullCodebase'] },
    branchPoint: { type: 'string' },
    changedFiles: { type: 'array', items: { type: 'string' } },
    diffStat: { type: 'string' },
  },
  required: ['scope', 'changedFiles'],
}

const scopePrompt = args?.reviewFullCodebase
  ? `Report scope="fullCodebase". Use Glob (not git) to list every file under:
- src/SqlArtisan/Sql/*.cs
- src/SqlArtisan/Internal/SqlPart/Expression/Function/**
- src/SqlArtisan/Internal/SqlBuilder/**
- tests/SqlArtisan.Tests/FunctionTests.*.cs
Return the full list as changedFiles.`
  : `Report scope="diff" for the current branch.

Per the sa-review-changes skill: local main is often stale, so a raw
"git diff main...HEAD" can pull in unrelated already-merged work. Find the
real branch point first:

1. git merge-base main HEAD   (fall back to origin/main HEAD if main is
   missing or stale)
2. git diff <merge-base>..HEAD --name-only
3. git diff <merge-base>..HEAD --stat

Execute these commands for real — do not guess. Return the merge-base commit
as branchPoint, the file list as changedFiles, and the stat text as diffStat.`

const scopeInfo = await agent(scopePrompt, {
  model: 'haiku',
  effort: 'low',
  label: 'scope-detection',
  phase: 'Scope',
  schema: SCOPE_SCHEMA,
})

log(`Scope: ${scopeInfo.scope}, ${scopeInfo.changedFiles.length} file(s)`)

// ---------------------------------------------------------------------------
// PHASE 2: Gates — run once, up front, so reviewers don't re-derive
// failures the toolchain already catches (sa-review-changes skill, step 2).
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

const gates = await agent(
  `Run the SqlArtisan review gates (sa-review-changes skill, step 2) and report
pass/fail for each. Do not fix anything — detection only.

dotnet build src/SqlArtisan/SqlArtisan.csproj -c Release
dotnet test tests/SqlArtisan.Tests
dotnet format SqlArtisan.sln --verify-no-changes

0 warnings is the bar for the build (AnalysisMode=Recommended, including
CS1574 cref resolution). Summarize any failure in one or two lines.`,
  { model: 'haiku', effort: 'low', label: 'gates', phase: 'Gates', schema: GATES_SCHEMA }
)

log(`Gates: build=${gates.buildPassed} test=${gates.testsPassed} format=${gates.formatClean}`)

// ---------------------------------------------------------------------------
// PHASE 3: Orchestrate — Fable classifies files and assigns dimensions.
// This is the one stage where the expensive model earns its keep: a
// misclassification here silently narrows every downstream review.
// ---------------------------------------------------------------------------
phase('Orchestrate')

const FILE_GROUP_SCHEMA = {
  type: 'object',
  properties: {
    category: { type: 'string' },
    files: { type: 'array', items: { type: 'string' } },
    priority: { type: 'string', enum: ['high', 'medium', 'low'] },
    reviewDimensions: { type: 'array', items: { type: 'string' } },
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

SCOPE: ${scopeInfo.scope}
FILES: ${JSON.stringify(scopeInfo.changedFiles)}
GATES: ${gates.summary}

Group by role:
1. Public API (Sql.*.cs) -> naming, API design, documentation alignment
2. Function implementations (Internal/SqlPart/Expression/Function/**) -> ADR
   conformance, DBMS grammar safety, allocation
3. Builders (Internal/SqlBuilder/**) -> SQL style, spacing, allocation budget
4. Tests (tests/**) -> coverage, exact-SQL assertions, guard tests, hazard
   shapes
5. Infrastructure (anything else touched) -> as appropriate

For each group's reviewDimensions, pick from: adr-conformance, api-design,
sql-style, dbms-safety, comment-quality, guard-handling, allocation-budget,
test-adequacy.

Flag 2-3 highRiskFiles (recent core-logic changes, multiple ADR touchpoints,
or allocation-sensitive paths). Only include groups that actually have files
in FILES.`

const plan = await agent(orchestratePrompt, {
  agentType: 'sa-review-orchestrator',
  phase: 'Orchestrate',
  schema: PLAN_SCHEMA,
})

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

const droppedFiles = scopeInfo.changedFiles.length -
  reviewUnits.reduce((n, u) => n + u.chunkFiles.length, 0)
if (droppedFiles > 0) {
  log(`Note: orchestrator omitted ${droppedFiles} file(s) from the plan — see synthesis for coverage gaps`)
}

// ---------------------------------------------------------------------------
// PHASE 4: Execute — Sonnet, via the sa-reviewer agent so every chunk
// inherits its read-only tool restriction and its pointer to the
// sa-review-changes / sa-run-sql-harness skills, instead of re-deriving
// (and risking drift from) that procedure inline in this prompt.
// ---------------------------------------------------------------------------
phase('Execute')

const reviewResults = await pipeline(reviewUnits, (unit) =>
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

Follow the sa-review-changes skill's checklist for whichever of these
dimensions apply, and use the sa-run-sql-harness skill for any empirical
verification (DBMS grammar, guard enforcement, allocation) — do not assume
emitted SQL or allocation behavior from memory. Skip the skill's own gate
step (already covered above) and skip re-scoping the diff (file list is
fixed above); otherwise follow it end to end.

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
  )
)

const reviewedUnits = reviewResults.filter(Boolean)
log(`Execution complete: ${reviewedUnits.length}/${reviewUnits.length} chunk(s) reviewed`)

// ---------------------------------------------------------------------------
// PHASE 5: Synthesize — Fable integrates findings into one report.
// ---------------------------------------------------------------------------
phase('Synthesize')

const synthesisPrompt = `Synthesize ${reviewUnits.length} independent chunk reviews of a
SqlArtisan ${scopeInfo.scope === 'diff' ? 'branch diff' : 'full codebase pass'} into one report.

GATES: ${gates.summary}
${droppedFiles > 0 ? `\nCOVERAGE GAP: the orchestration plan omitted ${droppedFiles} file(s) that were in scope — call this out explicitly in the report.\n` : ''}
CHUNK REVIEWS:
${reviewUnits.map((u, i) => `--- ${u.chunkLabel} ---\n${reviewResults[i] ?? '(this chunk failed to return a result)'}`).join('\n\n')}

Tasks:
1. Merge findings across chunks; surface cross-chunk patterns (e.g. the same
   naming issue in both Public API and Tests) rather than listing duplicates.
2. Prioritize: MUST FIX > SHOULD DISCUSS > NITS.
3. Decide a verdict: Mergeable / Mergeable after must-fix / Not mergeable.
   A failing gate above is itself a MUST FIX and blocks "Mergeable".

Output as a headed report:

# SqlArtisan Code Review: ${scopeInfo.scope === 'diff' ? 'Branch Diff' : 'Full Codebase'}

## Verdict
...

## Summary
(2-3 sentences)

## Findings by Severity
### MUST FIX
### SHOULD DISCUSS
### NITS

## Coverage
- Chunks reviewed: ${reviewedUnits.length}/${reviewUnits.length}
- Files in scope: ${scopeInfo.changedFiles.length}
- Gates: build=${gates.buildPassed} test=${gates.testsPassed} format=${gates.formatClean}
- Empirical probes actually run (from chunk reviews): ...

## Recommendations (ranked)
1. ...`

const finalReport = await agent(synthesisPrompt, {
  model: 'fable',
  label: 'synthesize',
  phase: 'Synthesize',
})

log('Review synthesis complete')

return {
  scope: scopeInfo.scope,
  gates,
  chunksReviewed: `${reviewedUnits.length}/${reviewUnits.length}`,
  highRiskFiles: plan.highRiskFiles,
  finalReport,
}
