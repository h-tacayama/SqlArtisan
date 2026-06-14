# SqlArtisan 実装計画(需要 × 実装容易性のバランス)

策定日: 2026-06-14
前提資料: `docs/feature-demand-research.md`(競合 + Web + ゼロベース発見調査)

本計画は **需要(調査で実証)** と **実装容易性(本リポジトリのアーキテクチャ基準)** の
2 軸でスコアリングし、ROI の高い順に段階化したもの。**需要の低い機能は明示的に後回し/不採用**とする。

---

## 実装容易性の基準(このリポジトリ固有)

| 容易性 | 内容 | 該当パターン |
|:------:|------|------|
| **S** | 関数 4 点セット(node/keyword/factory/test)・方言差なし | `AbsFunction` 型。`add-sql-function` スキルでほぼ自動 |
| **M** | 関数だが方言で構文/名前が分岐 → dialect 層に 1 プロパティ追加 | CEIL/CEILING 等 |
| **L** | 新しい句 or 式ノード + 方言分岐(複数 DBMS で別構文) | 文字列集約、日付演算、FILTER |
| **XL** | 新しい文ビルダー + 方言ファミリ分岐 + バージョン考慮 | UPSERT/MERGE |

「方言ごとに別 API を素直に並べる」(Drizzle/Kysely 流)を一貫方針とする。
意味的リライト(FILTER→CASE 等)は設計哲学(非目標)に反するため行わない。

---

## ROI マトリクス

| 機能 | 需要 | 容易性 | 方言差 | 象限 |
|------|:----:|:------:|:------:|------|
| NULLIF | 中 | **S** | 無 | ★即実装(高ROI) |
| ROUND / FLOOR / POWER / SQRT / SIGN | 中 | **S** | 無 | ★即実装(高ROI) |
| CEIL / CEILING | 中 | **M** | 小 | ★即実装 |
| **UPSERT(ON CONFLICT / ON DUPLICATE KEY UPDATE)** | **最高** | **XL** | 大 | ◎戦略投資 |
| 日付/期間演算(DATE_TRUNC/DATEADD/DATEDIFF/INTERVAL) | 高 | **L** | 最大 | ◎戦略投資 |
| MERGE(Oracle/SQL Server) | 最高 | **XL** | 大 | ◎戦略投資(UPSERT後段) |
| 文字列集約(STRING_AGG/LISTAGG/GROUP_CONCAT) | 中〜高 | **L** | 大 | ○需要駆動 |
| FILTER (WHERE) on aggregate | 中〜高 | **L** | 中 | ○需要駆動 |
| PostgreSQL 配列演算子(= ANY 等) | 中〜高 | **L** | PG専用 | ○需要駆動 |
| DISTINCT ON (PG) | 中 | M | PG専用 | ○需要駆動 |
| pgvector 類似検索 | 中(上昇) | M | PG専用 | △様子見 |
| 全文検索 | 高 | L | 最大 | △様子見(スコープ判断) |
| GROUPING SETS / ROLLUP / CUBE | 低 | L | 大(SQLite不可) | ✕後回し |
| LATERAL / CROSS APPLY | 低 | L | 2系統 | ✕後回し |

---

## 段階的計画

### Phase 1 — クイックウィン(スカラー関数)〔容易性 S/M・需要 中〕

**狙い:** 軽さを一切損なわず、関数カバレッジを目に見えて拡大。リスクほぼゼロ。
`add-sql-function` スキルの 4 点セットを反復するだけ。

- `NULLIF`(2 引数。`COALESCE`/`NVL` があるのに欠落している穴埋め)
- `ROUND`(`Round(x)` と `Round(x, n)` の 2 オーバーロード)
- `FLOOR`, `POWER`, `SQRT`, `SIGN`
- `CEIL`/`CEILING`(dialect に `CeilingKeyword` を追加:SQL Server=CEILING、他=CEIL)

**成果物:** 関数 7 種。1〜2 PR。**まずここから着手推奨。**
**完了条件:** 各関数の `FunctionTests.<Letter>` で 5 方言の Text 完全一致。CHANGELOG/README 更新。

---

### Phase 2 — UPSERT(ON CONFLICT ファミリ)〔容易性 XL・需要 最高〕

**狙い:** 最大の採用ドライバ。デフォルト DBMS(PostgreSQL)と SQLite を最初に取り、
構文が最も単純で利用も多い `ON CONFLICT` から入る。

- 新ビルダー: `InsertBuilder` に `.OnConflict(...)` 系を追加
  - PostgreSQL/SQLite: `ON CONFLICT (target) DO UPDATE SET ... [WHERE ...]` / `DO NOTHING`
  - `EXCLUDED`(PG 大文字)/`excluded.`(SQLite 小文字)の差を dialect 層で吸収
- 続けて MySQL: `.OnDuplicateKeyUpdate(...)`(**8.0.20+ の行エイリアス構文**で出力し `VALUES()` 非推奨を回避)

**設計上の論点(着手前に確定):**
- API 形状を方言別メソッドに分ける(統一抽象化はしない)
- 衝突対象の指定モデル差(PG/SQLite=明示必須、MySQL=暗黙)を型で表現
- 空更新(`DO NOTHING` / `ignore`)の扱い

**成果物:** PG/SQLite/MySQL の upsert。2〜3 PR。**Phase 1 の次の本命。**

---

### Phase 3 — 日付/期間演算〔容易性 L・需要 高〕

**狙い:** 発見調査で昇格した最大の「隠れ穴」。個別関数はあるが汎用演算が無い。
方言差は本リポジトリ最大だが、「構文だけ違う/意味は同じ」= dialect 層の本来の守備範囲で哲学に合致。

- `DateTrunc`(unit, expr):PG `date_trunc` / SQL Server `DATETRUNC`(2022+)/ Oracle `TRUNC(,'MM')` / MySQL は近似(date_format)/ SQLite strftime
- `DateAdd` / 期間加減(`INTERVAL`):PG `+ INTERVAL` / SQL Server `DATEADD` / MySQL `DATE_ADD` / Oracle `+ INTERVAL` / SQLite `date(,'+1 month')`
- `DateDiff`(unit, start, end):引数順・戻り単位の差を吸収

**論点:** 単位(year/month/day/hour…)を型安全な enum(`DatepartKeywords` を流用)で表現。
方言で対応できない単位はビルド時例外で明示(サイレント回避)。

**成果物:** 3 関数 + dialect 拡張。2〜3 PR。Phase 2 と並行可(領域が独立)。

---

### Phase 4 — MERGE + 文字列集約〔容易性 XL/L・需要 最高/中〜高〕

- **MERGE**(Oracle / SQL Server、PG15+ は任意):`WHEN MATCHED/NOT MATCHED`。
  Oracle の句内 `DELETE WHERE`、SQL Server の `NOT MATCHED BY SOURCE`・末尾セミコロン・`HOLDLOCK` 注意を反映。
  → Phase 2 の upsert を MERGE ファミリで補完し、5 DBMS 全てで upsert 可能に。
- **文字列集約**:`StringAgg`/`ListAgg`/`GroupConcat` を方言別に。
  順序(`ORDER BY` 内包 / `WITHIN GROUP` / 不可)・区切り・MySQL の `group_concat_max_len` 切り捨てを文書化。

**成果物:** 上級 DML の完成 + レポート定番関数。3〜4 PR。

---

### Phase 5 — 需要駆動バックログ〔ユーザー要望が来たら〕

優先実装はしないが、要望が来れば取り込む候補(いずれも方言別ヘルパーで):

- 集約の `FILTER (WHERE ...)`(PG/SQLite ネイティブ。他は CASE を文書案内、自動書換はしない)
- PostgreSQL 配列演算子(`= ANY`, `@>`, `&&`, `unnest`)
- `DISTINCT ON`(PG)
- pgvector 類似検索(`<->`,`<=>`,`<#>`)— AI/RAG 需要が伸びれば前倒し
- 全文検索 — 需要は高いが方言が完全別言語のため、スコープ採否を別途判断

---

## 明示的に後回し / 現時点不採用〔需要低〕

ユーザー要望が複数集まるまで着手しない:

- **GROUPING SETS / ROLLUP / CUBE** — .NET で明示需要が乏しく、SQLite 非対応の断崖あり
- **LATERAL / CROSS APPLY** — 「強力だが過小利用」の上級機能、需要が薄い
- **スコープ外**(ライブラリの本分=SQL 文字列生成 から外れる):
  ストアド実行(EXEC/CALL)、Bulk 実行(SqlBulkCopy)、列名マッピング/型ハンドラ

---

## Bulk Insert 方針(確定)

「Bulk Insert」は性質の異なる2種に分け、DBMS ごとに最適手段を割り当てる。
判断軸は「性能が本当に改善するか」。複数行 VALUES は1行ループ比で大きく改善するが
bulk-copy のスループットには届かず、パラメータ上限で頭打ちになる。

### DBMS 別の手段割り当て

| DBMS | 複数行 VALUES | 採用手段 | 配置 | 備考 |
|------|:---:|------|------|------|
| PostgreSQL / MySQL / SQLite / SQL Server | ○ | 複数行 INSERT(SQL構文) | **コア** | 数百〜数千行をカバー。哲学準拠・依存ゼロ |
| Oracle | ✕(VALUES 不可) | bulk-copy / 配列バインド | **別パッケージ** | VALUES が無いため高速化は bulk-copy 一択 |

### コア側(選択肢1+2)

- `.Values(IEnumerable<T>)` でコレクション駆動の複数行 INSERT を生成、方言別パラメータ上限で自動チャンク。
- Oracle は `INSERT ALL` も SQL 構文としては出せるが**高速用途では非採用**(遅く上限あり)。
- `ON CONFLICT` と組み合わせて複数行 UPSERT へ発展。

### 別パッケージ側(選択肢3 = `SqlArtisan.BulkCopy` 仮)

- **「Oracle 専用」ではなく拡張可能な器**として設計。将来 SQL Server 等で 10万行超が要れば `SqlBulkCopy` を追加できる構造に。
- **Oracle の実装方式は要検証**:
  - 基本線 = **配列バインド**(`OracleCommand.ArrayBindCount`)。全 ODP.NET で確実、bulk-copy 匹敵速度、依存リスク低。
  - 切替 = **`OracleBulkCopy`**(最速級だが `Oracle.ManagedDataAccess.Core` での可否を要確認)。
- **存在意義 = schema クラス(TableClassGen 生成)による「POCO→列」自動マッピング**。
  これが無ければ生 ODP.NET 直書きと変わらず、別パッケージ化の意味が薄い。
- 検証は実 DB 統合テストが必要(コアの「Text 完全一致」モデルが使えない)。

---

## まとめ(着手順の推奨)

```
Phase 1 (S/M, 即) ──► Phase 2 (XL, 本命) ──► Phase 3 (L, 並行可)
   NULLIF/数値関数        UPSERT(ON CONFLICT)      日付/期間演算
                              │
                              └──► Phase 4 (XL/L)  ──► Phase 5(需要駆動)
                                   MERGE/文字列集約      FILTER/PG配列/...
```

- **今すぐ価値が出る**のは Phase 1(低リスク・高 ROI)。
- **採用を最も動かす**のは Phase 2(難度は高いが需要が突出)。
- **隠れた大穴**は Phase 3(日付演算)。
- 需要の低い GROUPING SETS / LATERAL は計画から外し、バックログに置く。
