#!/usr/bin/env python3
"""Verify every intra-doc anchor link resolves across README, docs/, and llms.txt.

Checks two link kinds:
  - same-file anchors:      [text](#anchor)
  - absolute GitHub links:  [text](https://github.com/OWNER/REPO/blob/main/<path>#anchor)

Anchors are derived from headings with GitHub's slug rules (lowercase, drop
punctuation except hyphens, spaces->hyphens, dedup with -1/-2 suffixes). Links
to other repos, raw URLs, plain files (no #anchor), and external URLs are skipped.

Exit code 0 = all resolve; 1 = broken links found. Run from the repo root.
"""
import os
import re
import sys

REPO = "h-tacayama/SqlArtisan"
FILES = [
    "README.md",
    "llms.txt",
    "docs/README.md",
    "docs/query-statements.md",
    "docs/expressions.md",
    "docs/functions.md",
    "docs/analyzer.md",
]


def slug(heading: str) -> str:
    h = heading.strip().lower()
    h = re.sub(r"[^\w\s-]", "", h)
    return h.replace(" ", "-")


def anchors_of(path: str) -> set:
    seen, out, in_code = {}, set(), False
    for line in open(path, encoding="utf-8"):
        if line.startswith("```"):
            in_code = not in_code
            continue
        if in_code:
            continue
        m = re.match(r"^(#{1,6})\s+(.*?)\s*$", line)
        if m:
            base = slug(m.group(2))
            a = base if base not in seen else f"{base}-{seen[base]}"
            seen[base] = seen.get(base, 0) + 1
            out.add(a)
    return out


def url_to_file(url: str):
    m = re.search(rf"github\.com/{REPO}/blob/main/(.+)$", url)
    return m.group(1) if m else None


def main() -> int:
    anchors = {f: anchors_of(f) for f in FILES if os.path.exists(f)}
    link_re = re.compile(r"\[[^\]]*\]\(([^)]+)\)")
    broken = []
    for f in anchors:
        in_code = False
        for ln, line in enumerate(open(f, encoding="utf-8"), 1):
            if line.startswith("```"):
                in_code = not in_code
                continue
            if in_code:
                continue
            for url in link_re.findall(line):
                if url.startswith("#"):
                    tgt, anc = f, url[1:]
                elif f"github.com/{REPO}/blob/main/" in url and "#" in url:
                    path, anc = url.split("#", 1)
                    tgt = url_to_file(path)
                else:
                    continue
                if tgt is None:
                    continue
                if tgt not in anchors:
                    broken.append((f, ln, url, f"untracked target {tgt}"))
                elif anc not in anchors[tgt]:
                    broken.append((f, ln, url, f"missing #{anc} in {tgt}"))

    if broken:
        print("BROKEN LINKS:")
        for f, ln, url, why in broken:
            print(f"  {f}:{ln}  {url}  -> {why}")
        return 1
    print(f"OK: all intra-doc anchor links resolve ({len(anchors)} files).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
