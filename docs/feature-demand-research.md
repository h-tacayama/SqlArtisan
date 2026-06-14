# SqlArtisan 機能優先度 — 需要調査レポート

調査日: 2026-06-14
調査方法: 競合ライブラリ調査(C#/.NET + クロス言語)と Web 調査を 5 系統で並列実施し、
各主張を出典付きで収集。複数の独立した系統で相互裏付けが取れたものを高信頼度として扱う。

> 注意(信頼度の前提): 調査時、多くの一次情報サイト(GitHub, dev.mysql.com, stackoverflow など)が
> WebFetch で HTTP 403 を返したため、一部は検索結果のスニペット(出典ページを引用したもの)に依拠している。
> GitHub の reaction 数など定量値は「調査時点の概数」として扱い、確定値は各 URL で要確認。

---

## エグゼクティブサマリ

- **最大の需要は UPSERT/MERGE。** これは .NET でもクロス言語でも、また Stack Overflow 上でも
  一貫して「最も要望される SQL 機能」として現れた。SqlArtisan 未実装機能の中で最優先。
- **既存実装は需要の本丸を押さえている。** 実利用頻度データ(COUNT/SUM/AVG、JOIN 多用、Window 関数)で
  上位に来る機能は SqlArtisan が既にカバー済み。これは競合(特に EF Core)に対する明確な優位。
- **「軽さ」を保ったまま埋められる安価な穴**は、`NULLIF` と スカラー数値関数
  (`ROUND`/`CEIL`/`FLOOR`/`POWER`/`SQRT`)。方言差がほぼ無く、既存パターンの踏襲で実装できる。
- **方言差が大きい機能**(UPSERT、文字列集約、GROUPING SETS、LATERAL/APPLY)は、
  設計哲学どおり「方言ごとに別 API で素直に出す」方針が最適。これはクロス言語の成功例(Drizzle/Kysely/Knex)とも一致。

---

## 1. 需要ランキング(証拠の強さ順)

### S 位: UPSERT / MERGE / ON CONFLICT 〔最優先・強い証拠〕

複数系統で独立に「最大需要」と確認された唯一の機能。

- EF Core の **issue #4526「Merge/Upsert/AddOrUpdate support」は約 739 reactions** で、
  リポジトリ全体で最も賛同の多いオープン issue(2 位「squash migrations」419 を上回る)。EF Core はネイティブ未対応。
  出典: https://github.com/dotnet/efcore/issues/4526
- サードパーティ市場が需要を裏付け: `EFCore.BulkExtensions`(約 4k★)が `BulkInsertOrUpdate` を
  SQL Server/Oracle=`MERGE`、PostgreSQL=`ON CONFLICT`、MySQL=`ON DUPLICATE KEY UPDATE`、SQLite=`UPSERT` で実装。
  出典: https://github.com/borisdj/EFCore.BulkExtensions
- クロス言語でも要望トップ級: Knex #3186(統一 upsert API の起点)、Kysely #203(MERGE)、
  SQLAlchemy 1.1 の目玉が「PostgreSQL upsert」、Drizzle/Kysely も upsert を提供。
  出典: https://github.com/knex/knex/issues/3186 , https://github.com/kysely-org/kysely/issues/203 ,
  https://www.sqlalchemy.org/blog/2016/10/05/sqlalchemy-1.1.0-released/
- Stack Overflow でも頻出: 「upsert は SO の FAQ で非常に上位」(Cockroach Labs)。
  出典: https://www.cockroachlabs.com/blog/sql-upsert/

**方言差(大・3 系統に分かれる):**

| DBMS | 構文 | 要点 |
|------|------|------|
| PostgreSQL | `INSERT ... ON CONFLICT (target) DO UPDATE/NOTHING` | 衝突対象が必須、`EXCLUDED` 参照、`WHERE` 可。9.5+。MERGE は 15+ |
| SQLite | `INSERT ... ON CONFLICT(target) DO UPDATE` | PG とほぼ同形。参照は小文字 `excluded.`。3.24.0+ |
| MySQL | `INSERT ... ON DUPLICATE KEY UPDATE` | 衝突対象は暗黙(任意の unique index)。`VALUES()` は 8.0.20 で非推奨→行エイリアス推奨 |
| SQL Server | `MERGE ... WHEN MATCHED/NOT MATCHED ...;` | 末尾セミコロン必須。`NOT MATCHED BY SOURCE` 独自。並行性に `HOLDLOCK` 必要 |
| Oracle | `MERGE INTO ... WHEN MATCHED THEN UPDATE [DELETE WHERE] ...` | 句内 `DELETE WHERE`・句別 `WHERE` 拡張 |

出典: https://www.bytebase.com/blog/sql-upsert/ , https://wiki.postgresql.org/wiki/UPSERT ,
https://sqlite.org/lang_upsert.html , https://dev.mysql.com/doc/refman/8.0/en/insert-on-duplicate.html ,
https://oracle-base.com/articles/9i/merge-statement

**実装者が知るべき落とし穴:**
- SQL Server `MERGE` は内部的に INSERT/UPDATE/DELETE を独立実行するため、`HOLDLOCK` 無しだと競合・デッドロックの恐れ。
  歴史的バグも多く、コミュニティは長く分割文を推奨。2023 年再評価では「DELETE 動作と一時テーブル対象は避ける」が残課題。
  出典: https://www.mssqltips.com/sqlservertip/3074/use-caution-with-sql-servers-merge-statement/ ,
  https://sqlserverfast.com/blog/hugo/2023/09/an-update-on-merge/
- PostgreSQL は `ON CONFLICT` を単一行 upsert の並行安全策として意図的に MERGE と別物に保っている(15 で MERGE 追加後も)。
  出典: https://wiki.postgresql.org/wiki/UPSERT

### A 位: Window 関数 〔高需要だが SqlArtisan は実装済み = 競争優位〕

- EF Core #12747「Support SQL window functions」はオープンで約 96–107 reactions。
  EF はインメモリ評価との整合で難航し、サードパーティ(Zomp.EFCore.WindowFunctions)頼み。
  出典: https://github.com/dotnet/efcore/issues/12747
- 実利用でも ROW_NUMBER/RANK 等は「最も使われる分析構文」と繰り返し言及(ただし定量データは逸話的)。
- **示唆:** SqlArtisan は既にフレーム(ROWS/RANGE)込みで実装済み。新規実装ではなく**訴求ポイント**として活用すべき。

### A 位: Bulk Insert 〔高需要だが大半はスコープ外〕

- Dapper 系では 2011 年以来の長年の要望(Dapper-Plus, Dapper.Bulk 等が穴埋め)。
  ただし多くは SQL Server 限定(`SqlBulkCopy`)。OrmLite は方言別ネイティブ構文で横断対応。
  出典: https://github.com/zzzprojects/Dapper-Plus , https://docs.servicestack.net/ormlite/bulk-inserts
- **示唆:** Bulk は「実行」レイヤー(`SqlBulkCopy` 等)の話で、SQL 文字列生成器である SqlArtisan の本分とは外れる。
  ただし **複数行 INSERT(multi-row VALUES)は既に実装済み**で、文字列としての需要部分はカバー済み。

### B 位: 文字列集約(STRING_AGG / LISTAGG / GROUP_CONCAT)〔中〜高需要・方言差大〕

- EF Core 7 で `string.Join`→`STRING_AGG` を部分対応したが、MySQL `GROUP_CONCAT` は依然ギャップ(#26838)。
  「複数行を 1 行にまとめる/行→列ピボット」の定番回答として質問頻度が高い。
  出典: https://github.com/dotnet/efcore/issues/26838
- **方言差(最も激しい):** 名前(GROUP_CONCAT / STRING_AGG / LISTAGG)・順序指定(`ORDER BY` 内包 / `WITHIN GROUP` / 不可)・
  区切り指定・MySQL の `group_concat_max_len`(既定 1024 バイト)で**黙って切り捨て**る挙動まで分岐。
  出典: https://database.guide/mysql-group_concat-vs-t-sql-string_agg/ ,
  https://www.dbvis.com/thetable/a-complete-guide-to-the-mysql-group_concat-function/

### B 位: スカラー数値関数(ROUND / CEIL / FLOOR / POWER / SQRT)〔常用・方言差ほぼ無し・実装安価〕

- チュートリアルで「基礎関数」扱い。全 5 DBMS でほぼ統一。**唯一の分岐は `CEIL`(Oracle/MySQL/PG)vs `CEILING`(SQL Server/MySQL/PG)。**
  出典: https://www.databasestar.com/sql-ceil/ , https://www.databasestar.com/sql-round/
- **示唆:** 既存 `AbsFunction`/`ModFunction` と同型ノードで安価。「軽さを保って穴を埋める」に最適。

### B 位: NULLIF 〔常用・方言差ゼロ・実装最小〕

- ANSI/ISO 標準の CASE 略記で、Oracle/MySQL/PostgreSQL/SQL Server/SQLite で統一。スコープ内で最も方言差が小さい。
  出典: https://database.guide/sql-nullif-explained/
- **示唆:** 既に `COALESCE`/`NVL` はあるのに `NULLIF` だけ欠落。実装は極小で効果あり。

### C 位: GROUPING SETS / ROLLUP / CUBE 〔ニッチ・サポート断崖〕

- .NET では目立った要望 issue が見当たらず、明示需要は UPSERT/Bulk/Window より低い。
- **サポート断崖:** PG/SQL Server/Oracle は全対応、MySQL は `WITH ROLLUP` のみ(標準 `ROLLUP(...)` 形ですらない)、
  **SQLite は非対応(UNION ALL で代替)。**
  出典: https://www.cybertec-postgresql.com/en/postgresql-grouping-sets-rollup-cube/

### C 位: LATERAL / CROSS APPLY / OUTER APPLY 〔上級・需要はあるが入門外〕

- 「強力だが過小利用」「上級機能」と評される。LATERAL(PG/MySQL 8+/Oracle 12c+)と APPLY(SQL Server)の 2 系統に分裂。
  出典: https://www.heap.io/blog/postgresqls-powerful-new-join-type-lateral ,
  https://www.sqlines.com/sql-server-to-postgresql/cross_apply

### 参考: JSON 〔高需要だが大スコープ〕

- EF Core 7 で「強く要望されて」実装、追加マッピング需要も継続(#28592 ≈ 298 reactions)。
  ただし方言差が大きく実装規模も大きいため、「軽量」目標とはトレードオフ。

---

## 2. 実利用頻度データ(現状実装の妥当性検証)

唯一の大規模パース済みコーパス研究(Johnson et al., VLDB 2018, Uber の実クエリ 810 万件):

- 集約の内訳: **COUNT 51% > SUM 29% > AVG 8% > MAX 6% > MIN 5%** → いずれも SqlArtisan 実装済み。
- クエリの **約 62% が JOIN を使用**、**約 1/3 が集約クエリ** → JOIN・GROUP BY/集約とも実装済み。
- 出典: https://www.vldb.org/pvldb/vol11/p526-johnson.pdf
- 懐疑的注記: 単一組織(Uber)・分析寄りワークロードの偏りあり。一般化は慎重に。

DBMS 人気(SqlArtisan の `Dbms` enum と既定値の妥当性):

- Stack Overflow Developer Survey 2024: **PostgreSQL ≈ 49%(最多・2 年連続)** > MySQL ≈ 40% > SQLite ≈ 33% > SQL Server ≈ 25%。
  → 既定 DBMS を PostgreSQL にしている設計を支持。対応 5 DBMS は両サーベイの上位帯と一致(Oracle は dev 調査で弱いが企業で強い)。
  出典: https://survey.stackoverflow.co/2024/technology , https://www.jetbrains.com/lp/devecosystem-2024/

懐疑的注記: CASE/COALESCE/NULLIF/ROUND/STRING_AGG や Window 関数の「最頻」主張は、
公開情報では大半が逸話的(チュートリアル/リスト記事)で、パース済みコーパスの裏付けは見つからなかった。
Stack Overflow のタグ件数は環境制約(403)で実数取得不可。確定にはStack Exchange Data Explorer が必要。

---

## 3. 設計哲学との整合(方言の扱い方)

クロス言語の成功例は 2 派に分かれる:

- **jOOQ / SQLAlchemy:** 標準 `MERGE` を書かせて他方言にエミュレート(重い抽象化)。
- **Drizzle / Kysely / Knex:** **方言別メソッドを素直に分けて公開**(`onConflictDoUpdate` vs `onDuplicateKeyUpdate` 等)。

SqlArtisan の哲学「書いた SQL がそのまま走る/可搬性は非目標」には**後者が完全に一致**する。
したがって UPSERT/文字列集約/GROUPING/LATERAL は、単一 API への抽象化ではなく、
**方言ごとに別 API として並べる**(dialect 層で構文差のみ吸収)方針を推奨。
出典: https://orm.drizzle.team/docs/insert , https://kysely-org.github.io/kysely-apidoc/classes/OnConflictBuilder.html ,
https://blog.jooq.org/the-many-flavours-of-the-arcane-sql-merge-statement/

---

## 4. 推奨ロードマップ

| 優先 | 機能 | 根拠 | 方言差 | 実装コスト |
|:----:|------|------|:------:|:----------:|
| **1** | **UPSERT/MERGE**(ON CONFLICT / ON DUPLICATE KEY UPDATE / MERGE を方言別 API で) | 全系統で最大需要(EF #4526=739) | 大(3 系統) | 大 |
| **2** | **NULLIF** | 常用・既存の穴 | 無 | 極小 |
| **2** | **スカラー数値関数**(ROUND/CEIL/FLOOR/POWER/SQRT/SIGN) | 常用・基礎 | 極小(CEIL/CEILING) | 小 |
| **3** | **文字列集約**(STRING_AGG/LISTAGG/GROUP_CONCAT を方言別) | 中〜高需要 | 大 | 中 |
| 4 | GROUPING SETS / ROLLUP / CUBE | ニッチ・需要低 | 大(SQLite 非対応) | 中 |
| 4 | LATERAL / CROSS APPLY | 上級・需要あるが入門外 | 2 系統 | 中 |
| — | JSON | 高需要だが大スコープ | 大 | 大 |
| — | Bulk Insert | 高需要だが実行レイヤー=スコープ外(multi-row は実装済み) | — | — |

**着手順の推奨:**
1. まず **優先 2(NULLIF + 数値関数)** を即実装 — 軽量さを損なわず、確実な穴埋め。既存パターン踏襲で完結。
2. 次に **優先 1(UPSERT/MERGE)** — 方言別 API の設計方針(各 DBMS を別メソッドで並べる)を固めてから着手。最大の採用ドライバ。
3. 続いて **優先 3(文字列集約)** — dialect 層で名前・順序・区切りの差を吸収。

---

## 4.5 追補 — ゼロベース発見的調査(最初の提案が見落とした需要)

初回調査は「既出候補(UPSERT/ROUND/NULLIF 等)の需要検証」に偏っており(確証バイアス)、
未考慮カテゴリの発見にはなっていなかった。これを是正するため、**候補リストを除外指定した白紙調査**を
別途実施し、コードベースと突き合わせて「本当に未実装の新カテゴリ」だけを抽出した。

### 4.5.1 発見されたが実は実装済み(誤検出 — 重要な較正)

discovery で「ギャップ」として挙がったが、コード確認の結果**既にカバー済み**だったもの:

| 項目 | 実態 |
|------|------|
| クエリ/テーブルヒント | `SelectClauseWithHints` / `Sql.H` で実装済み |
| 動的な任意フィルタ合成 | `ConditionIf`(Sql.A)で実装済み = README の看板機能 |
| REGEXP マッチング述語 | `RegexpLikeCondition` で実装済み |
| 動的 IN リスト(C# コレクション→複数マーカー展開) | `In(params object[])` で実装済み(`AppendCsv` で展開) |

→ これらは再提案しない。SqlArtisan は当初想定より広くカバーしている。

### 4.5.2 最初の提案が完全に見落としていた本物のギャップ

| 機能 | 需要 | 方言差 | コード現状 | 出典 |
|------|------|:------:|------------|------|
| **日付/期間演算**(DATEADD/DATEDIFF/DATE_TRUNC/INTERVAL) | 非常に高(レポート/時系列の定番) | **最大**(引数順すら反転) | 部分のみ(AddMonths/Extract/MonthsBetween はあるが汎用演算なし) | https://docs.getdbt.com/sql-reference/datediff , https://docs.getdbt.com/sql-reference/date-trunc |
| **全文検索**(to_tsvector@@ / MATCH AGAINST / CONTAINS / FTS5) | 高(ほぼ全アプリ) | 最大(5者で全く別言語) | 無 | https://www.instaclustr.com/blog/postgresql-full-text-search/ |
| **PostgreSQL 配列演算子**(`= ANY`, `@>`, `&&`, `unnest`) | 高(PG中心アプリ・EF8 が投資) | PG 専用 | 無 | https://www.roji.org/queryable-pg-arrays-in-ef8 |
| **集約の FILTER (WHERE ...)** | 中〜高(ダッシュボード) | 中(PG/SQLite のみ。他は CASE) | 無 | https://www.datacamp.com/doc/postgresql/filter |
| **pgvector 類似検索**(`<->`,`<=>`,`<#>`) | 上昇中(AI/RAG) | PG 専用 | 無 | https://github.com/pgvector/pgvector |
| **DISTINCT ON**(PG) | 中(group 内最新行) | PG 専用(他は Window で代替) | 無 | https://github.com/dotnet/efcore/issues/27470 |

### 4.5.3 スコープ外と判断(SQL 文字列生成器の本分から外れる)

- ストアドプロシージャ実行(EXEC/CALL)、Bulk 実行(SqlBulkCopy)、列名マッピング(snake_case)、
  型ハンドラ/サイレント型変換 — いずれも「実行・マテリアライズ」レイヤーの話で、SqlArtisan の領域外。

### 4.5.4 較正後の総合所見

- **最大の見落としは「日付/期間演算の汎用サーフェス」**。需要が広く方言差が最悪だが、これは
  まさに「構文だけが違う/意味は同じ」= dialect 層の本来の守備範囲で、設計哲学とも矛盾しない。
- **全文検索・PG配列・pgvector**は需要は高いが、いずれも方言/PG専用色が強く、
  「方言別ヘルパーを素直に並べる」方針で出すのが適切(単一抽象化は哲学違反)。
- **FILTER / DISTINCT ON** は「他方言で CASE / Window に書き換える」と意味的リライト(非目標)になるため、
  対応するなら**ネイティブ構文をそのまま出す + 代替手段を文書化**に留めるべき。

### 較正後の優先度(初回 + 発見を統合)

1. UPSERT/MERGE(最大需要・方言別 API)
2. NULLIF + スカラー数値関数(安価・確実)
3. **日付/期間演算の汎用化(DATEADD/DATEDIFF/DATE_TRUNC/INTERVAL)** ← 発見で昇格
4. 文字列集約(方言別)
5. 全文検索 / PG配列 / pgvector(いずれも方言別ヘルパー)← 発見で新規
6. FILTER / DISTINCT ON / GROUPING SETS / LATERAL(ニッチ・哲学的注意)

---

## 5. 調査の限界

- WebFetch が多くの一次サイトで 403。定量値(reaction 数等)は検索スニペット経由で、確定値は各 URL 要確認。
- 唯一の大規模頻度研究は単一組織・分析寄りの偏りあり。OLTP アプリ SQL の頻度は別途検証が望ましい。
- Stack Overflow のタグ実数は未取得(環境制約)。Stack Exchange Data Explorer での再確認を推奨。
- 個別関数の「最頻」主張の多くは逸話的で、コーパス裏付けは限定的。
