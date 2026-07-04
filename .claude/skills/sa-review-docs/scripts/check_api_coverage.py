#!/usr/bin/env python3
"""Check doc <-> source API parity (the "no surplus / no deficiency" review).

Deficiency (code -> doc): every public `Sql.*` factory should be mentioned
somewhere in the docs. Reports factories that appear in no doc file.

Surplus (doc -> code): every C# name a doc claims via "`Name()` for `TOKEN`"
should exist as a public factory. Reports claimed names missing from source.

This is a fast heuristic, not a compiler. The authoritative surplus check is
that every code block compiles — see verify_sql_examples.py. Run from repo root.
Exit 0 if clean, 1 if any gap is found.
"""
import re
import subprocess
import sys
from pathlib import Path

SQL_DIR = Path("src/SqlArtisan/Sql")
DOC_FILES = [
    "README.md", "docs/README.md", "docs/query-statements.md",
    "docs/expressions.md", "docs/functions.md", "docs/analyzer.md",
]
# Known instance/extension members that legitimately appear as "`X()` for `Y`"
# without being a static Sql.* factory.
INSTANCE_OK = {"WithinGroup", "Over", "Currval", "Nextval", "PartitionBy", "OrderBy"}


def public_factories() -> set:
    names = set()
    pat = re.compile(r"public static [\w<>,.\[\] ]+?\b([A-Z][A-Za-z0-9]*)\s*(\(|=>|\{)")
    for cs in SQL_DIR.glob("*.cs"):
        for m in pat.finditer(cs.read_text(encoding="utf-8")):
            names.add(m.group(1))
    return names


def main() -> int:
    if not SQL_DIR.exists():
        print(f"ERROR: {SQL_DIR} not found (run from repo root).")
        return 1
    factories = public_factories()
    alldocs = "\n".join(Path(f).read_text(encoding="utf-8") for f in DOC_FILES if Path(f).exists())

    # Deficiency: factory mentioned nowhere in docs.
    undocumented = sorted(n for n in factories if not re.search(rf"\b{re.escape(n)}\b", alldocs))

    # Surplus: "`Name()` for `TOKEN`" whose Name is neither a factory nor a known member.
    claimed = set()
    for f in DOC_FILES:
        p = Path(f)
        if not p.exists():
            continue
        for m in re.finditer(r"`([A-Z][A-Za-z0-9]*)(?:\([^`]*\))?`\s+for\s+`", p.read_text(encoding="utf-8")):
            claimed.add(m.group(1))
    phantom = sorted(
        n for n in claimed
        if n not in factories and n not in INSTANCE_OK
        and subprocess.run(["grep", "-rwq", n, "src/SqlArtisan/"]).returncode != 0
    )

    print(f"public Sql factories: {len(factories)} | doc-claimed function names: {len(claimed)}")
    ok = True
    if undocumented:
        ok = False
        print("\nDEFICIENCY — public factories not mentioned in any doc:")
        for n in undocumented:
            print(f"  {n}")
    if phantom:
        ok = False
        print("\nSURPLUS — doc-claimed names not found in source:")
        for n in phantom:
            print(f"  {n}")
    if ok:
        print("OK: every public factory is documented; no phantom API claims.")
        return 0
    return 1


if __name__ == "__main__":
    sys.exit(main())
