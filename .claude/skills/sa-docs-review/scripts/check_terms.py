#!/usr/bin/env python3
"""Lint SqlArtisan docs for the terminology / formatting conventions in
.claude/rules/docs-style.md. Heuristic — review each hit, don't blindly fix.

Flags:
  - "table schema class"            -> use "table class"
  - "type-safety" (hyphen noun)     -> "type safety" (noun is open)
  - lowercase "upsert"              -> "UPSERT" (feature/concept name)
  - DBMS code identifiers in prose  -> use display spelling outside code
  - unspaced em-dash  word—word     -> spaced  "word — word"
  - "bind parameter <noun>" (space) -> "bind-parameter <noun>" (modifier)
  - trailing whitespace / double blank lines / trailing blank line at EOF

Term/style checks ignore inline code, link targets, and URLs to avoid false
positives (e.g. an `#upsert-...` anchor). Exit 0 if clean, 1 if any flag fires.
Run from repo root.
"""
import re
import sys
from pathlib import Path

FILES = [
    "README.md", "llms.txt", "docs/README.md",
    "docs/query-statements.md", "docs/expressions.md", "docs/functions.md",
    "docs/analyzer.md", "docs/cookbook.md", "docs/guides/dapper-quickstart.md",
    "docs/guides/ai-assistants.md",
    "docs/versioning.md", "SECURITY.md", "CHANGELOG.md",
    "src/SqlArtisan.TableClassGen/README.md",
]
CODE_IDENTS = ("PostgreSql", "SqlServer", "MySql", "Sqlite")  # display: PostgreSQL/SQL Server/MySQL/SQLite


def prose_lines(path):
    """Yield (lineno, text) for lines outside ``` code fences."""
    in_code = False
    for ln, line in enumerate(open(path, encoding="utf-8"), 1):
        if line.rstrip("\n").lstrip().startswith("```"):
            in_code = not in_code
            continue
        if not in_code:
            yield ln, line.rstrip("\n")


def strip_code_and_links(s: str) -> str:
    s = re.sub(r"`[^`]*`", "", s)        # inline code
    s = re.sub(r"\]\([^)]*\)", "]", s)   # markdown link target (keep the [text])
    s = re.sub(r"https?://\S+", "", s)   # bare URLs
    return s


def main() -> int:
    findings = []  # (file, line, rule, snippet)
    for f in FILES:
        if not Path(f).exists():
            continue
        lines = Path(f).read_text(encoding="utf-8").split("\n")
        if lines and lines[-1] == "":
            lines = lines[:-1]            # drop the element after the final newline
        for i in range(len(lines) - 1):   # interior double blanks
            if lines[i] == "" and lines[i + 1] == "":
                findings.append((f, i + 1, "double-blank-line", ""))
        if lines and lines[-1] == "":      # file ends with a blank line
            findings.append((f, len(lines), "trailing-blank-line-at-eof", ""))

        for ln, text in prose_lines(f):
            if text != text.rstrip():
                findings.append((f, ln, "trailing-whitespace", repr(text[-15:])))
            clean = strip_code_and_links(text)
            if re.search(r"table schema class", clean, re.I):
                findings.append((f, ln, "term:table-schema-class", text.strip()[:70]))
            if "type-safety" in clean:
                findings.append((f, ln, "term:type-safety", text.strip()[:70]))
            if re.search(r"\bupsert\b", clean):
                findings.append((f, ln, "term:lowercase-upsert", text.strip()[:70]))
            if re.search(r"[A-Za-z0-9)]—[A-Za-z0-9(]", clean):
                findings.append((f, ln, "style:unspaced-em-dash", text.strip()[:70]))
            if re.search(r"bind parameter (prefix|marker|type)", clean):
                findings.append((f, ln, "term:bind-parameter-modifier", text.strip()[:70]))
            bare = re.sub(r"Dbms\.[A-Za-z]+", "", clean)
            for ident in CODE_IDENTS:
                if re.search(rf"\b{ident}\b", bare):
                    findings.append((f, ln, f"dbms:code-ident-in-prose ({ident})", text.strip()[:70]))

    if findings:
        print(f"{len(findings)} potential issue(s):\n")
        for f, ln, rule, snip in findings:
            print(f"  {f}:{ln}  [{rule}]  {snip}")
        return 1
    print("OK: no terminology/formatting issues found.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
