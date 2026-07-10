export const meta = {
  name: 'sa-multi-model-code-review',
  description: 'Fable orchestrator + Sonnet reviewers with git diff detection and allocation probes',
  phases: [
    { title: 'Scope (Bash)', detail: 'Detect git diff and file classification' },
    { title: 'Orchestrate (Fable)', model: 'fable', detail: 'Plan review dimensions' },
    { title: 'Execute (Sonnet)', model: 'sonnet', detail: 'Multi-dimensional review with probes' },
    { title: 'Synthesize (Fable)', model: 'fable', detail: 'Integrate findings and verdict' },
  ],
}

// SCOPE DETECTION PHASE: Determine review target via git diff
phase('Scope (Bash)')

log('Detecting review scope from git and args...')

const scopeInfo = await agent(
  `You have Bash and file-read tools.

**TASK: Detect review scope for SqlArtisan**

${args?.reviewFullCodebase ? `
User explicitly requested FULL CODEBASE review.
- Return scope: "fullCodebase"
- List all critical source files:
  * src/SqlArtisan/Sql/*.cs (ALL Public API files)
  * src/SqlArtisan/Internal/SqlPart/Expression/Function/** (ALL function implementations)
  * src/SqlArtisan/Internal/SqlBuilder/** (ALL builder implementations)
  * tests/SqlArtisan.Tests/FunctionTests.*.cs (ALL test files)
- Do not execute git commands; just report this list.
` : `
User did NOT request full codebase, so determine DIFF mode.

1. Run: git log --oneline main..HEAD (show commits on this branch)
2. Run: git diff main...HEAD --name-only (show files changed from branch point to HEAD)
3. Run: git diff main...HEAD --stat (show summary)

Return:
\`\`\`json
{
  "scope": "diff",
  "branchCommits": "output of git log main..HEAD",
  "changedFiles": "list from git diff --name-only",
  "diffStat": "output of git diff --stat"
}
\`\`\`

If main does not exist, try origin/main instead.
`}

**CRITICAL**: Do not guess—execute the commands and paste output verbatim.
`,
  {
    label: 'scope-detection',
    phase: 'Scope (Bash)',
  }
)

log(\`Scope detected: \${scopeInfo.scope}\`)

// ORCHESTRATION PHASE (Fable): Analyze and create review plan
phase('Orchestrate (Fable)')

log('Fable orchestrating file classification and review plan...')

const orchestratePrompt = \`
You are the orchestrator for a comprehensive SqlArtisan code review.

**SCOPE INFORMATION**
\${JSON.stringify(scopeInfo, null, 2)}

**TASK 1: Classify Files into Groups**
From the files listed above, group them by category:
1. **Public API** (Sql.*.cs) → Focus: naming, API design, documentation
2. **Function Implementations** (Internal/SqlPart/Expression/Function/**) → Focus: ADR, DBMS, perf
3. **Builders** (Internal/SqlBuilder/**) → Focus: style, spacing, allocation
4. **Tests** (tests/**) → Focus: coverage, assertions, guards
5. **Infrastructure** (other files) → Focus: as appropriate

**TASK 2: Determine Review Dimensions**
For each file group, specify which dimensions Sonnet should review:
- adr-conformance: cite specific ADRs (0001–0007, 0010)
- api-design: naming rules, overload split, collection types
- sql-style: Keywords.cs atoms, spacing, buffer helpers, const interpolation
- dbms-safety: DbmsDialect isolation, no Dbms branches in nodes, dialect-specific grammar
- comment-quality: WHY not WHAT, brevity, no restates
- guard-handling: eager throw vs Build-time throw, exception messages
- allocation-budget: ADR 0006—must be measured, not guessed
- test-adequacy: (for test files) coverage, negative cases, hazard shapes

**TASK 3: Identify High-Risk Files**
From the files in scope, pick 2–3 that are critical due to:
- Recent changes that touch core logic
- Multiple ADR touchpoints
- Allocation-sensitive paths

**OUTPUT FORMAT**
Return JSON:
\`\`\`json
{
  "scope": "\${scopeInfo.scope}",
  "fileGroups": [
    {
      "category": "Public API",
      "files": ["src/SqlArtisan/Sql/Sql.A.cs", ...],
      "priority": "high | medium | low",
      "reviewDimensions": ["adr-conformance", "api-design", "documentation"]
    },
    ...
  ],
  "highRiskFiles": ["file1", "file2", "file3"],
  "totalFilesInScope": N,
  "estimatedComplexity": "high | medium | low"
}
\`\`\`

Be concise. Your output drives Sonnet's execution.
\`

const plan = await agent(
  orchestratePrompt,
  {
    agentType: 'sa-review-orchestrator',
    phase: 'Orchestrate (Fable)',
    schema: {
      type: 'object',
      properties: {
        scope: { type: 'string' },
        fileGroups: { type: 'array' },
        highRiskFiles: { type: 'array' },
        totalFilesInScope: { type: 'number' },
        estimatedComplexity: { type: 'string' },
      },
      required: ['scope', 'fileGroups'],
    },
  }
)

log(\`Plan: \${plan.fileGroups.length} groups, \${plan.totalFilesInScope} files, complexity=\${plan.estimatedComplexity}\`)

// EXECUTION PHASE (Sonnet + Pipeline): Review each file group
phase('Execute (Sonnet)')

log('Sonnet reviewers executing multi-dimensional reviews...')

const reviewResults = await pipeline(
  plan.fileGroups,
  async (fileGroup) => {
    const dimensionsList = fileGroup.reviewDimensions
      .map((d, i) => \`\${i + 1}. \${d}\`)
      .join('\n')

    const fileList = fileGroup.files.slice(0, 3).join(', ') +
      (fileGroup.files.length > 3 ? \` (+ \${fileGroup.files.length - 3} more)\` : '')

    const reviewPrompt = \`
You are conducting a **deep, empirical code review** of SqlArtisan files.
This review includes allocation probes and grammar verification.

**FILE GROUP: \${fileGroup.category}**
Priority: \${fileGroup.priority}
Files: \${fileList}
Total in group: \${fileGroup.files.length}

**REVIEW DIMENSIONS** (apply all):
\${dimensionsList}

**YOUR APPROACH**

1. **Read the files** in this group (use your file-read tools).
2. **Apply each dimension**, citing specific code and relevant rules/ADRs.
3. **Verify empirically** where applicable:
   - For API files: confirm XML doc examples match actual SQL output
   - For Function implementations: verify DBMS grammar (build throwaway probes)
   - For Builders: check allocation efficiency (run GC.GetAllocatedBytesForCurrentThread probes)
   - For Tests: verify exact SQL strings, negative cases, hazard shapes

4. **Allocation Probe Template** (for ADR 0006 verification):
   If you need to check allocation, describe what probe to run. Example:
   \`\`\`csharp
   static long Alloc(Action a, int n) {
     a(); // warm up
     long b = GC.GetAllocatedBytesForCurrentThread();
     for (int i = 0; i < n; i++) a();
     return (GC.GetAllocatedBytesForCurrentThread() - b) / n;
   }
   // Compare: literal vs bound-parameter path
   Console.WriteLine(Alloc(() => GC.KeepAlive(
     Select(GroupConcat(u.Name, Separator(", "))).From(u).Build(Dbms.MySql)), 50_000) + " B/op");
   \`\`\`

5. **SQL Harness Template** (for DBMS grammar verification):
   Use this to verify emitted SQL across dialects:
   \`\`\`csharp
   foreach (Dbms d in new[]{ Dbms.PostgreSql, Dbms.MySql, Dbms.Oracle, Dbms.Sqlite, Dbms.SqlServer })
     Console.WriteLine(\`\${d}: \${Select(/* construct */).From(u).Build(d).Text}\`);
   \`\`\`

   Do NOT guess DBMS grammar—run this harness and paste output verbatim.

6. **Guard Verification** (for ADR 0007):
   For mandatory clauses (e.g., Oracle LISTAGG needs WITHIN GROUP):
   \`\`\`csharp
   try {
     Select(Listagg(u.Name, ", ")).From(u).Build(Dbms.Oracle);
     Console.WriteLine("LEAK: guard failed");
   } catch (Exception ex) { Console.WriteLine(\`GUARD: \${ex.GetType().Name}: \${ex.Message}\`); }
   \`\`\`

7. **Distinguish findings**:
   - MUST FIX: bugs, ADR violations, invalid/wrong SQL, missing guards
   - SHOULD DISCUSS: convention trade-offs, test gaps, doc drift, allocation opportunities
   - NITS: minor style, tone, micro-optimizations

8. **Output format**:
   \`\`\`
## \${fileGroup.category} Review

**Summary**
[1 paragraph: verdict, key findings, confidence]

### Dimension: [Name]
[Findings, or "No findings" if clean]

[Repeat for each dimension...]

**Empirical Verification**
- Harness probes: [what you tested]
- Dialects verified: [PostgreSQL, Oracle, MySQL, etc.]
- Allocation probes: [if any were run]
- Guard tests: [if tested]

---
   \`\`\`

Remember: You are one of \${plan.fileGroups.length} reviewers.
Be thorough within your group. Your findings will be synthesized with others.
\`

    return agent(
      reviewPrompt,
      {
        model: 'sonnet',
        label: \`review:\${fileGroup.category}\`,
        phase: 'Execute (Sonnet)',
        effort: 'high',
      }
    )
  }
)

log(\`Execution complete: \${reviewResults.filter(Boolean).length} groups reviewed\`)

// SYNTHESIS PHASE (Fable): Integrate and produce final report
phase('Synthesize (Fable)')

log('Fable synthesizing findings and final verdict...')

const synthesisPrompt = \`
You are the final synthesizer for a comprehensive SqlArtisan code review.
Sonnet has reviewed \${plan.fileGroups.length} file groups; integrate into one coherent report.

**REVIEW RESULTS BY FILE GROUP**
\${reviewResults.filter(Boolean).map((r, i) => \`
### Group \${i + 1}: \${plan.fileGroups[i]?.category || 'Unknown'}
\${r}
\`).join('\n')}

**YOUR TASK**

1. **Integrate findings**:
   - Identify cross-group patterns (e.g., "naming inconsistency spans Public API and Tests")
   - Cross-reference related issues (ADR violation in function X correlates with missing test case Y)
   - Prioritize: must-fix > should-discuss > nits

2. **Produce verdict**:
   - ✅ Mergeable: no must-fix issues
   - ⚠️ Mergeable after must-fix: clear path to resolution
   - ❌ Not mergeable: blocking issues

3. **Format final report**:

\`\`\`
# SqlArtisan Code Review: \${plan.scope === 'diff' ? 'Branch Diff' : 'Full Codebase'}

## Verdict
[Mergeable | Mergeable after must-fix | Not mergeable]

## Summary
[2–3 sentences: overall assessment, key risks, empirical coverage]

## Findings by Severity

### MUST FIX (Blocking)
- [file:line — defect — scenario — impact]
- ...

### SHOULD DISCUSS (Improvements)
- [file:line — issue — suggestion]
- ...

### NITS
- [minor findings]
- ...

## Coverage Summary
- File groups reviewed: \${plan.fileGroups.length}
- Total files examined: \${plan.totalFilesInScope}
- Scope: \${plan.scope}
- High-risk files flagged: \${plan.highRiskFiles?.length || 0}

**Empirical Probes Executed**
- SQL harness dialects: [which were actually tested]
- Allocation probes: [count and what they validated]
- Guard tests: [count]

## Recommendations (Ranked)
1. [What to fix first]
2. [What to discuss with team]
3. [What can defer]

---
\`\`\`

Synthesis matters more than individual reviews at this stage. Call out
contradictions or incomplete findings.
\`;

const finalReport = await agent(
  synthesisPrompt,
  {
    model: 'fable',
    label: 'synthesize',
    phase: 'Synthesize (Fable)',
  }
)

log('Review synthesis complete')

// RETURN results
return {
  scopeDetected: scopeInfo.scope,
  fileGroupsReviewed: plan.fileGroups.length,
  totalFilesInScope: plan.totalFilesInScope,
  highRiskFiles: plan.highRiskFiles,
  finalReport: finalReport,
  planSummary: plan,
}
