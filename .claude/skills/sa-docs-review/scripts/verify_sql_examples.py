#!/usr/bin/env python3
"""Empirically verify the emitted SQL in the docs against the real builder.

Extracts every runnable code block from docs/query-statements.md,
docs/expressions.md, docs/functions.md (a `.Build(...)` statement followed by an
expected `// SELECT/INSERT/...` comment), generates a throwaway .NET console that
references the repo's SqlArtisan, runs it, and compares the emitted `sql.Text`
to the documented SQL (whitespace-normalised, spaces around parens ignored).

Why: the documented SQL must match what the code emits today — not what we
remember. The `//` comments are line-wrapped for readability while `sql.Text`
is a single line, so the comparison normalises whitespace.

Usage:  python3 .claude/skills/sa-docs-review/scripts/verify_sql_examples.py [workdir]
Run from the repo root. Requires the dotnet SDK. Exit 0 if all match.

Table classes used by the doc examples are embedded below. If the docs add an
example using a new table/column, compilation fails loudly — extend SCAFFOLD.
"""
import json
import os
import re
import subprocess
import sys
import tempfile

DOCS = ["docs/query-statements.md", "docs/expressions.md", "docs/functions.md"]
SQLKW = ("SELECT", "INSERT", "UPDATE", "DELETE", "MERGE", "WITH")
SQLPROJ = "src/SqlArtisan/SqlArtisan.csproj"

# Locals a snippet may reference without declaring (the section's prose declares
# them once); injected when referenced and not assigned inside the block. The
# aliased form is picked when the documented SQL qualifies through the alias.
INJECTABLE_LOCALS = [
    ("u", "UsersTable u = new();", 'UsersTable u = new("u");'),
    ("s", "SalesTable s = new();", 'UsersTable s = new("s");'),
    ("post", "PostsTable post = new();", 'PostsTable post = new("post");'),
]

SCAFFOLD = r"""using System.Data;
using SqlArtisan;
using static SqlArtisan.Sql;

internal sealed class UsersTable : DbTableBase {
    public UsersTable(string a="") : base("users",a) {
        Id=new(this,"id");Name=new(this,"name");CreatedAt=new(this,"created_at");StatusId=new(this,"status_id");
        Age=new(this,"age");DepartmentId=new(this,"department_id");Salary=new(this,"salary");Amount=new(this,"amount");Date=new(this,"date");
        Data=new(this,"data");IsActive=new(this,"is_active");Region=new(this,"region");FirstName=new(this,"first_name");LastName=new(this,"last_name");
    }
    public DbColumn Id{get;} public DbColumn Name{get;} public DbColumn CreatedAt{get;} public DbColumn StatusId{get;}
    public DbColumn Age{get;} public DbColumn DepartmentId{get;} public DbColumn Salary{get;} public DbColumn Amount{get;} public DbColumn Date{get;}
    public DbColumn Data{get;} public DbColumn IsActive{get;} public DbColumn Region{get;} public DbColumn FirstName{get;} public DbColumn LastName{get;}
}
internal sealed class PostsTable : DbTableBase {
    public PostsTable(string a="") : base("posts",a) { Id=new(this,"id");Title=new(this,"title");Body=new(this,"body"); }
    public DbColumn Id{get;} public DbColumn Title{get;} public DbColumn Body{get;}
}
internal sealed class OrdersTable : DbTableBase {
    public OrdersTable(string a="") : base("orders",a) { Id=new(this,"id");UserId=new(this,"user_id");OrderDate=new(this,"order_date");Amount=new(this,"amount"); }
    public DbColumn Id{get;} public DbColumn UserId{get;} public DbColumn OrderDate{get;} public DbColumn Amount{get;}
}
internal sealed class ArchivedUsersTable : DbTableBase {
    public ArchivedUsersTable(string a="") : base("archived_users",a) { Id=new(this,"id");Name=new(this,"name");CreatedAt=new(this,"created_at"); }
    public DbColumn Id{get;} public DbColumn Name{get;} public DbColumn CreatedAt{get;}
}
internal sealed class TestTable : DbTableBase {
    public TestTable(string a="") : base("test_table",a) { Code=new(this,"code"); }
    public DbColumn Code{get;}
}
internal sealed class SalesTable : DbTableBase {
    public SalesTable(string a="") : base("sales",a) { Region=new(this,"region");Product=new(this,"product");Amount=new(this,"amount");Channel=new(this,"channel"); }
    public DbColumn Region{get;} public DbColumn Product{get;} public DbColumn Amount{get;} public DbColumn Channel{get;}
}
internal sealed class SeniorUsersCte : CteBase {
    public SeniorUsersCte(string name) : base(name) { SeniorId=new(this,"senior_id");SeniorName=new(this,"senior_name");SeniorAge=new(this,"senior_age"); }
    public DbColumn SeniorId{get;} public DbColumn SeniorName{get;} public DbColumn SeniorAge{get;}
}
internal sealed class AcctTable : DbTableBase {
    public AcctTable(string a="") : base("acct",a) { Id=new(this,"id");Total=new(this,"total"); }
    public DbColumn Id{get;} public DbColumn Total{get;}
}
internal sealed class LedgerTable : DbTableBase {
    public LedgerTable(string a="") : base("ledger",a) { Id=new(this,"id");Amount=new(this,"amount"); }
    public DbColumn Id{get;} public DbColumn Amount{get;}
}
internal sealed class RentalTable : DbTableBase {
    public RentalTable(string a="") : base("rental",a) { RentalId=new(this,"rental_id");CustomerId=new(this,"customer_id");RentalDate=new(this,"rental_date"); }
    public DbColumn RentalId{get;} public DbColumn CustomerId{get;} public DbColumn RentalDate{get;}
}
internal sealed class RentalArchiveTable : DbTableBase {
    public RentalArchiveTable(string a="") : base("rental_archive",a) { RentalId=new(this,"rental_id");CustomerId=new(this,"customer_id"); }
    public DbColumn RentalId{get;} public DbColumn CustomerId{get;}
}
"""


def code_blocks(path):
    out, in_code, lang, buf, start = [], False, None, [], 0
    for ln, line in enumerate(open(path, encoding="utf-8"), 1):
        if line.lstrip().startswith("```"):
            if in_code:
                out.append((start, buf)); buf = []
            else:
                lang = line.strip().lstrip("`"); start = ln
            in_code = not in_code
            continue
        if in_code and lang.startswith("csharp"):
            buf.append(line.rstrip("\n"))
    return out


def parse_block(buf):
    """Return (setups[str], builds[(expr, expected)]) splitting on `.Build(` statements."""
    setups, builds, cur, awaiting, exp, pending = [], [], [], False, [], None
    for line in buf:
        st = line.strip()
        if st.startswith("//"):
            if awaiting:
                exp.append(st[2:].strip())
            continue
        line = re.sub(r"\s*//.*$", "", line)  # strip inline comment
        st = line.strip()
        if st == "":
            if awaiting and exp:
                builds.append((pending, list(exp))); awaiting, exp, pending = False, [], None
            continue
        if awaiting:
            builds.append((pending, list(exp))); awaiting, exp, pending = False, [], None
        cur.append(line)
        if st.endswith(";"):
            stmt = "\n".join(cur); cur = []
            if ".Build(" in stmt:
                expr = re.sub(r"^\s*(?:SqlStatement|ISqlBuilder|var)\s+\w+\s*=\s*", "", stmt, count=1).rstrip().rstrip(";")
                pending, awaiting, exp = expr, True, []
            else:
                setups.append(stmt)
    if awaiting:
        builds.append((pending, list(exp)))
    return setups, builds


def clean_expected(lines):
    s = " ".join(x for x in lines if x)
    s = re.sub(r"\s{2,}\[[^\]]*\]\s*$", "", s)  # drop trailing "[:0 = ...]" annotation
    return re.sub(r"\s+", " ", s).strip()


# A one-line local declaration ("AcctTable t = new(...);") — requires a type
# token before the name so a plain field assignment inside a mis-parsed class
# body ("SeniorName = new DbColumn(...);") doesn't look like one.
LOCAL_DECL_RE = re.compile(r"^[A-Za-z_]\w*(?:<[^=;\n]*>)?(?:\[\])?\s+(\w+)\s*=\s*new\b")


def norm(s):
    s = re.sub(r"\s+", " ", s).strip()
    s = re.sub(r"\(\s+", "(", s)
    s = re.sub(r"\s+\)", ")", s)
    return s


def main():
    work = sys.argv[1] if len(sys.argv) > 1 else tempfile.mkdtemp(prefix="docverify-")
    os.makedirs(work, exist_ok=True)
    if not os.path.exists(SQLPROJ):
        print(f"ERROR: {SQLPROJ} not found (run from repo root)."); return 1

    cases = []
    for f in DOCS:
        if not os.path.exists(f):
            continue
        # A doc section sometimes splits one worked example across several
        # fenced blocks (a per-dialect variant reusing the prior block's
        # locals without redeclaring them). Track each file's declarations in
        # page order so a later block can borrow one from an earlier block.
        recent_decls = {}
        for start, buf in code_blocks(f):
            setups, builds = parse_block(buf)
            declared_here = {m.group(1) for s in setups for m in [LOCAL_DECL_RE.match(s.strip())] if m}
            sql_builds = [(e, clean_expected(x)) for e, x in builds
                          if any(t.upper().startswith(SQLKW) for t in x)]
            block_text = "\n".join(buf)
            for j, (expr, expected) in enumerate(sql_builds):
                injected_names = set(declared_here)
                injects = []
                # A page-local declaration from an earlier block in this same
                # worked example is more specific than the generic fallback
                # below, so it wins when both would apply to the same name.
                for name, decl in recent_decls.items():
                    if name in injected_names:
                        continue
                    if re.search(rf"\b{name}\b", block_text) and not re.search(rf"\b{name}\s*=", block_text):
                        injects.append(decl)
                        injected_names.add(name)
                for name, plain, aliased in INJECTABLE_LOCALS:
                    if name in injected_names:
                        continue
                    if re.search(rf"\b{name}\.", block_text) and not re.search(rf"\b{name}\s*=", block_text):
                        injects.append(aliased if f'"{name}"' in expected else plain)
                        injected_names.add(name)
                cases.append({"id": f"{f}:{start}#{j}", "expr": expr, "expected": expected,
                              "setups": setups, "injects": injects})
            for s in setups:
                m = LOCAL_DECL_RE.match(s.strip())
                if m:
                    recent_decls[m.group(1)] = s

    methods, calls = [], []
    for k, c in enumerate(cases):
        body = []
        for decl in c["injects"]:
            body.append("        " + decl)
        for s in c["setups"]:
            body.append("        " + s.replace("\n", "\n        "))
        expr = c["expr"].replace("\n", "\n            ")
        body.append(f'        Emit("{c["id"]}", ({expr}).Text);')
        methods.append(f"    static void M{k}() {{\n" + "\n".join(body) + "\n    }")
        calls.append(f'        try {{ M{k}(); }} catch (Exception ex) {{ Console.Error.WriteLine("THREW {c["id"]}: " + ex.Message); }}')

    prog = (SCAFFOLD + "\nstatic class Runner {\n"
            '    static void Emit(string id, string text) { Console.WriteLine("@@" + id + "\\t" + text); }\n'
            + "\n".join(methods)
            + "\n    static void Main() {\n" + "\n".join(calls) + "\n    }\n}\n")

    abs_proj = os.path.abspath(SQLPROJ)
    open(os.path.join(work, "Program.cs"), "w", encoding="utf-8").write(prog)
    open(os.path.join(work, "docverify.csproj"), "w", encoding="utf-8").write(
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
        "<TargetFramework>net8.0</TargetFramework><RollForward>Major</RollForward><Nullable>enable</Nullable>"
        "<ImplicitUsings>enable</ImplicitUsings></PropertyGroup>"
        f'<ItemGroup><ProjectReference Include="{abs_proj}" /></ItemGroup></Project>')

    print(f"generated {len(cases)} cases in {work}")
    run = subprocess.run(["dotnet", "run", "-c", "Release", "--project", work],
                         capture_output=True, text=True)
    if run.returncode != 0:
        print("BUILD/RUN FAILED:\n" + (run.stderr or run.stdout)[-2000:]); return 1

    actual = {}
    for line in run.stdout.splitlines():
        if line.startswith("@@"):
            i, t = line[2:].split("\t", 1)
            actual[i] = t
    for line in run.stderr.splitlines():
        if line.startswith("THREW"):
            print("  " + line)

    ok, fails = 0, []
    for c in cases:
        a = actual.get(c["id"])
        if a is not None and norm(a) == norm(c["expected"]):
            ok += 1
        else:
            fails.append((c["id"], c["expected"], a))
    print(f"\nMATCH {ok}/{len(cases)}")
    for i, exp, a in fails:
        print(f"\n[{i}]\n  doc : {exp}\n  real: {norm(a) if a else '<no output / threw>'}")
    return 0 if ok == len(cases) and cases else 1


if __name__ == "__main__":
    sys.exit(main())
