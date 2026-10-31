# 試し: 「値が、ある位置で、どこかのエンジンに別の意味で読まれる」型 — 報告

作業ブランチ `claude/trial-value-reading-model`（main ce84e78 から分岐）。PR・issue は作っていない。
不具合は直していない。成果物はこのディレクトリと `tests/SqlArtisan.IntegrationTests/ValueReading/`、
それに作業ブランチだけで書き換えた `.github/workflows/integration.yml`（試しの Example だけを
走らせ、結果を詳しく出す）。

## 結果（先に）

1. **モデルの大きさ** — 位置 724（418 メンバーの引数をコードから数えた）を 35 区分に分類、
   値の種類 29、文脈 8、方言と版 8。区分レベルのセル 9,744、位置レベルに展開すると
   724 × 29 × 8 = 167,968。人への問い 12 件（うち 10 件が計 287 セルに付く）。
2. **Example** — DB なしの Build 段 104,980 件（全位置 × 全値 × 5 方言の総当たり）。エンジンの
   Example は最終版で 1 本の流れあたり 117 件、8 本の流れすべてで実行して 117 × 8 = 936 件
   （統合テスト 4 回。SQLite・PostgreSQL 16 はこのコンテナでも実行）。導いた規則は § 3.2。
3. **誤りを入れた測定** — 対象 9 ファイルで 496 個。単体テストだけで 467 個（94.15%）。試しの
   DB なし層を足しても 467 個で、**新たに捕まえたのは 0**。生き残り 29 個の内訳は、答えを
   変えない 11・Example を足すべき 6・記録して残す 12。足すべき 6 個（short / byte の整数）を
   足した後は 473 個（95.36%）。
4. **レビュー** — 指摘 2 件。(a) モデルの中にあるのに Example が足りなかった 2、(b) 0、(c) 0。
   2 件ともエンジンで確かめて不具合だった（F3・F4）。逆に、レビューは F1・F2 を見つけなかった。
5. **見つけた不具合 4 件**（どれも黙って違う結果を返す）と **人への問い 12 件** — § 6・§ 7。
   - F1: MySQL で `Bind(n)`（整数）を ORDER BY / GROUP BY に置くと、列 n の位置として読まれる。
   - F2: Oracle で `OrderBy(2.5)` / `OrderBy(2.0)` / `OrderBy(2.5m)` が列 2 の位置として読まれる。
   - F3: SQLite と MySQL で `日時列 + TimeSpan`（`-` も）が日時の加算でなく数値の加算になる（レビュー由来）。
   - F4: MySQL で `Set(a == b, b == a)` の 2 つ目が、1 つ目で書いた後の値を読む（レビュー由来）。
6. **「モデルの範囲で網羅し、確かめた」と言えるか** — **言えない**。Build 段は全位置 × 全値 ×
   5 方言を総当たりし外れ 0。エンジン段はキーの 6 区分について方言ごとに全組を確かめた。
   ところがレビューが、モデルの中のセル（`+` / `-` の右辺 × TimeSpan、SET 代入 × 列）で
   Example が無かった所から不具合を 2 件出した。値の位置を「代表 1 つ」で済ませた導き方が
   穴だった。足りないものは § 8。
7. **時間** — § 9。表とプローブで約 15 分、エンジンの Example と統合テスト 4 回で約 25 分、
   Stryker 4 回で約 20 分、レビューのサブエージェントが約 18 分（ツールの実行時刻からの概算）。
8. **スキル化の判断材料** — § 10。既存スキルの置き換えではなく、足すほうに向く。
9. **ほかの型への展開** — § 11。価値はある。ただし「位置の区分」と「見分けるデータ」を型ごとに
   作る手間は残る。

---

## 1. 読んだもの

- issue / PR: #673 #678 #687 #691 #692 #694 #696 #697 #707 #757。
- 記録: `guards-and-empty-states.md`（境界表、Incomplete の列挙、§ bound-value、§ Null arguments、
  § GROUP BY ordinal、§ negative row count、§ function-argument enum）、`dbms-differences.md`、
  ADR 0001 / 0007 / 0010 / 0012 / 0016 / 0022。
- **`sql-building-style.md` の rule 8 は無かった**（main ce84e78 ではこのファイルは rule 7 まで、
  リポジトリは浅いクローンで履歴もたどれない）。どの記録を指したのかは問い Q1。

## 2. 危険のモデル（表）

コード: `tests/SqlArtisan.IntegrationTests/ValueReading/ValueReadingModel.cs`
（`ClassOf` が位置 → 区分、`Expect` が区分 × 値 × 流れ × 文脈 → 期待結果と根拠）。
書き出し: `model-cells.tsv`（9,744 セル、根拠つき）、`position-inventory.tsv`（724 位置）。

### 2.1 位置の数え方

`PositionInventory.cs` が出荷アセンブリを反射で走査する。対象は、公開型の公開メンバー
（メソッド・演算子・コンストラクター、`get_` と `object` 由来のものを除く、`[Obsolete(error)]`
を除く）の引数のうち、型が `object` / `object[]` / `(object, object)` / 行 / ジェネリック /
`SqlExpression` 系 / `ISubquery` / `IIncompleteExpression` 系 / 数値 / `string` / `char` /
`bool` / SqlArtisan の enum のもの。テーブル・列リスト以外の構造型（`SortOrder` など 303 引数）は
数えて外した。結果は 418 メンバー・724 位置（`object` 392、`object[]` 63、ペア 56、`SqlExpression`
系 69、文字列 46、数値 32、enum 24、ほか）。

区分は「値がどの解決経路を通るか」で決めた（`ExpressionResolver.Resolve` / `ResolveCompared` /
`ResolveQuantifiable` / `ResolveIntervalOperand` / `OrderByItemResolver` / `GroupByItemResolver` /
`ResolveKey` / `DistinctOnItemResolver` / `InsertValueResolver` / `SelectItemResolver` /
`Bind`、数値を文字に刷る引数、束縛する件数）。**全 724 位置が 1 つの区分に入ることをテストが
確かめる**（`Inventory_EverySlot_IsClassified`）。最初の実行で `BindValue` のコンストラクターが
漏れていたのをこのテストが捕まえた。

### 2.2 軸の値

| 軸 | 値 | 数 |
|---|---|---|
| 位置 | 724 位置、35 区分（`ValueArgument` 410、`ExpressionOnly` 69、`TextParameter` 42 …、キーの区分は `StatementOrderByKey` 5・`ExpressionOrderByKey` 2・`StatementGroupByKey` 2・`GroupingElement` 5・`PartitionKey` 1・`DistinctOnKey` 1） | 724 / 35 |
| 値の種類 | 整数 0・正・負・long・short・byte、double・整数値の double（`2.0`）・decimal、文字列、char、bool、関数引数の enum、ユーザー enum、DateTime・Guid・DateTimeOffset・TimeSpan、C# null、DBNull、`Bind(int)`・`Bind(string)`・`BindNull()`、`Sql.Null`、列、式、サブクエリ、ANY、INTERVAL | 29 |
| 文脈 | トップレベル、サブクエリ、相関サブクエリ、CTE、集合演算、ウィンドウの中、集約の ORDER BY、SET 代入（依頼の 7 つに足した） | 8 |
| 方言と版 | MySQL 8.0、Oracle XE 21.3、Oracle 23.26.3、PostgreSQL 16、PostgreSQL 17、SQLite 3.50.4、SQL Server 2022、SQL Server 2025 | 8 |

依頼の値の一覧に足したもの: double（記録に `OrderBy(2.5)` がある）、ユーザー enum（#678）、
`Sql.Null` と サブクエリ（記録に出る）、ANY と INTERVAL（#707）。`2.0`・short・byte は試しの
途中で足した（§ 3.4・§ 4）。

### 2.3 期待結果の種類

依頼の 4 つ（意図した意味で実行 / Build() で拒む / アナライザーが警告 / コンパイルで止まる）に
**「エンジンが大きく拒む」を足した**。記録は、黙って違う行を返さずエンジンが拒むものを
わざと守らずに置いている（例: `ORDER BY NULL` は PostgreSQL 16 で失敗）。この区別がないと
多くのセルを正しく書けない。ほかに、表の外へ委ねたセル（`Delegated`: ADR 0016 の文字列の
位置、DatepartValidity の値、`BindArray`、刷られる数値の値域など、既存のゲートが持つもの）と、
SQL を作らない位置（`NotASqlSlot`: 設定・メタデータ）を分けた。

| 結果 | セル数 |
|---|---|
| 実行（意図した意味） | 3,995 |
| Build() で拒む | 2,495 |
| コンパイルで止まる | 1,752 |
| 委ねた | 926 |
| SQL を作らない | 232 |
| アナライザーが警告 | 158 |
| 人への問い | 146（問いの付いたセルは計 287。実行など別の結果に問いを添えたものを含む） |
| エンジンが拒む | 40 |
| 計 | 9,744 |

### 2.4 根拠

各セルの `basis` に記録の節・ADR・エンジンの公式文書（PostgreSQL `select.sgml`、SQLite
`lang_select.html`、Oracle のエラー参照 ORA-01785 / ORA-22818）を書いた。根拠のないセルは
推測で埋めず問いにした。試しの途中でエンジンの観測から直したセルは、根拠欄に run 番号を
書いてある（§ 3.4）。

## 3. Example を導き、エンジンで確かめる

### 3.1 DB なしの Build 段（全位置の総当たり）

`PositionProbe.cs` が各位置に各値を入れ（他の引数は有効な値で埋める。`FactoryGuardSweepTests`
の流儀）、文に埋め込んで 5 方言で `Build` し、「コンパイルで止まる / 呼び出しで投げる /
Build で投げる / マーカーで束縛 / 文字として刷る / その他」を観測してモデルと比べる。
ビルダーの段階の位置は、その段階を宣言する呼び出しで作った新しいビルダーに当てる（1 つの
ビルダーは 1 つの鎖 #676）。

結果（値の種類 29、`build-level-probe.tsv.gz`）: 104,980 件中 OK 92,916、照合しない
11,599（委ねた・問い・非 SQL）、届かない 465（汎用の呼び出しで作れない位置、ほぼ文字列の位置）、
**外れ 0**。実行時間は約 3 秒。外れを 0 にするまでに直したのは、すべてモデルとプローブの側だった（C# の null は
参照型の引数ならコンパイルが通る、VALUES の null は NULL を入れる #169、など）。

### 3.2 エンジンの Example を導く規則

`EngineExamples.cs` の `ExampleCatalog.Derive()`。

- **E1**: キーの 6 区分（文の ORDER BY、ウィンドウの ORDER BY、文の GROUP BY、グループ要素、
  PARTITION BY、DISTINCT ON）× モデルでどこかの流れのエンジンに届く値の種類 **すべて**。
  Build で拒む値・コンパイルで止まる値は 3.1 で確かめ済みなので除く。
- **E2**: 値の位置の対照（SELECT 項目 × エンジンに届く値すべて）、比較の右辺 × {ANY, `Sql.Null`,
  列, `Bind`}、`+` の右辺 × {文字列, char}、ROUND の桁 × {負, 正}。
- **E3**: 既知の例が出た文脈。文の ORDER BY × {サブクエリ（派生表 + 件数）, CTE, 集合演算} ×
  {正の整数, `Bind(int)`, 2.5, 2.0, 負の整数, `Sql.Null`, 列}（#521 の入れ子のブロック、#757 の
  複合）。ウィンドウ外の集約 ORDER BY × {`Bind(int)`, `Sql.Null`, 列}（#678/#693）。相関
  サブクエリは既知の例に出ていないので当てていない。

**「位置 × 値」の網羅の取り方**: Build 段は全位置 × 全値を方言ごとに総当たりした（2 因子なので、
方言ごとの 2 因子網羅は総当たりと同じになる）。エンジン段はキーの区分（各 1 代表位置）について
方言ごとに全組、値の位置は区分ごとに 1 代表（SELECT 項目）で全値。値の位置を代表で済ませたのは、
Build 段で全位置が同じ解決経路を通ると確かめたうえでの仮定である（§ 8 の 2）。

- **E4**（手順 5 の後に足した）: `+` / `-` の右辺 × TimeSpan（日時列に足す）、SET 代入 × 列
  （`Set(b == g, g == b)` の入れ替え）。レビューの 2 件を確かめるためのもの。

Example の数: 統合テスト 1 回目 100、2 回目 106（`2.0`）、3 回目 114（short・byte）、4 回目 117（E4）。

### 3.3 見分けるデータ

```
vr_t: id | a | b  | g        vr_s: id | b
      1  | e | 50 | 1              1  | 40
      2  | d | 40 | 1              2  | 20
      3  | c | 30 | 2
      4  | b | 20 | 2
      5  | a | 10 | 3
```

列 2（b）は id と逆順に並ぶ。ORDER BY のキーの後ろに必ず `id` を足すので、「列 2 で並べた」
（5,4,3,2,1）と「定数で並べた」（1,2,3,4,5）は必ず別の行になる。GROUP BY は `SELECT COUNT(id), b`
で、位置なら 5 グループ、定数なら厳しいエンジンでは失敗、SQLite では 1 グループ。PARTITION BY は
ROW_NUMBER が id と同じ（1 区画）か全部 1（b ごと）かで分かれる。

### 3.4 エンジンの結果（詳細は `engine-results.md`）

最終の 4 回目（run 41160892388、1 本 117 件）の外れ:

| 流れ | 外れ | 中身 |
|---|---|---|
| SQLite 3.50.4（CI + このコンテナ） | 2 | F3（`+` / `-` TimeSpan） |
| PostgreSQL 16（CI + このコンテナの 16.15） | 0 | |
| PostgreSQL 17（CI） | 0 | |
| SQL Server 2022（CI） | 2 | `datetime2 + time` を拒む（大きな失敗。モデルの期待が「実行」だったので外れ） |
| SQL Server 2025（CI） | 外れあり | ジョブは失敗。ログは読んでいない（2022 と同じ 2 件と見ているが未確認） |
| MySQL 8.0（CI） | 10 | F1 の 7、F3 の 2、F4 の 1 |
| Oracle XE 21.3（CI） | 外れあり | 読んだ範囲で F2 の 4（派生表・CTE）。トップレベルの F2 は 1・2 回目で観測、4 回目はログの末尾に入らず未読 |
| Oracle 23.26.3（CI） | 外れあり | ジョブは失敗。4 回目のログは読んでいない（2 回目で F2 の 6 を観測） |

「方言の表で使えるとされる所では、返る行がエンジンどうしで同じになる」の確認は、期待する行を
データから一つに決めて全流れで比べる形で入れた（流れごとに期待を変えるのは、モデルが
「その方言では別の結果」と根拠つきで言うセルだけ）。この確認が F1・F2 を捕まえた。

エンジンの観測で直したモデルの誤り（製品の不具合ではない。どれも大きな失敗か記録の読み落とし）:

- SQLite には ANY が無い（方言の表で `false`）。比較の右辺の ANY は SQLite で SQLA0100。
- 複合の末尾 ORDER BY は、PostgreSQL は出力列の名前と番号だけ、SQLite は結果列と同じ式なら可、
  Oracle は番号だけ（ORA-01785、負の数も不可）、MySQL は修飾列が不可。
- `Sql.Null` をキーにしたときの記録（PARTITION BY NULL は 1 区画、GROUP BY・グループ要素・
  DISTINCT ON の NULL は PostgreSQL 16 が拒む）を最初は読み落としていた。
- グループ要素のサブクエリ（`ROLLUP((SELECT ...))`）は Oracle（ORA-22818）と SQL Server が拒む。
  記録は PostgreSQL 16 で動くことだけを書いている。
- Oracle XE 21 で束縛した bool を SELECT で読み戻すと ODP.NET が `InvalidCastException`、
  Guid は両版で `ArgumentException`（ドライバーの型の対応。問い Q10）。

## 4. 誤りを入れて Example の強さを測る

結果の表と、生き残り 29 個の 1 つずつの分類は `stryker-results.md`。要点:

| 回 | 走らせたテスト | 捕まえた / 496 | スコア | 時間 |
|---|---|---|---|---|
| 1 | 単体のみ | 467 | 94.15% | 3 分 8 秒 |
| 2 | 単体 + 試しの DB なし層 | 467（新たに 0） | 94.15% | 6 分 30 秒 |
| 3 | 2 + short / byte を足した後 | 473 | 95.36% | 7 分 16 秒 |

分類（回 1・2 の 29 個）: 答えを変えない 11（同値の比較 3、例外の文言だけ 5、null 配列の
名前つき例外が NRE に変わるだけ 3）、Example を足すべき 6（short / byte の整数。足して回 3 で全部
捕まえた）、記録して残す 12（集合演算の続きの前置き付き `Select` 5、入れ子の複合の描画 3、相関
DML ガード 2、float NaN 1、`BindArray<bool>` 1）。

読み取れること: この型に関わるコードは既存の単体テストがすでに強く（94%）、DB なし層は
コードの誤りに対してはほとんど上乗せしない。この試しで見つかった不具合 4 件は、どれもコードの
書き換えではなく「エンジンがどう読むか」の問題で、この測定には映らない。

設定: `stryker-unit.json`（単体テストだけ）、`stryker-unit-plus-trial.json`（単体テスト +
`ValueReadingModelTests`）。Stryker.NET 5.0.0、`coverage-analysis: perTest`、並列 4。
対象 9 ファイル: `ExpressionResolver.cs`、`OrderByItemResolver.cs`、`NumericSortKey.cs`、
`GroupByItemResolver.cs`、`NumericGroupKey.cs`、`DistinctOnItemResolver.cs`、`SelectItemResolver.cs`、
`InsertValueResolver.cs`、`OrderByClause.cs`、`PartitionByClause.cs`、`SelectBuilder.cs`。
（最初は `SelectBuilder.cs` を行範囲で絞ったが Stryker の範囲指定が効かず、ファイル全体にした。）

エンジンの Example は Stryker では走らせていない（DB が要る）。F1・F2 は単体テストでも DB なし層でも
捕まらない種類の誤りで、Stryker の値には表れない。

## 5. レビューを 1 回だけ流して指摘を分ける

独立したサブエージェント 1 つ（general-purpose）に、表・結果・仮説を渡さずに頼んだ。頼み方:
「`src/SqlArtisan/` で、公開 API に渡した値が、ある位置で、あるエンジンに、呼び手の意図と別の意味で
読まれる不具合を探して。試しのディレクトリは読まないこと。PostgreSQL 16.15 と SQLite はその場で
実行してよい。記録が意図と書いている振る舞いは、その前提が偽だと示せない限り不具合ではない」。

返ってきた指摘は 2 件。サブエージェントは約 18 分、ツール呼び出し 93 回。どちらも
PostgreSQL 16 と SQLite でその場で確かめ、MySQL は文書を記憶から引いた（dev.mysql.com は
このセッションのネットワーク方針で開けなかった）と明記していた。私は 4 回目の統合テストで
8 本の流れで確かめた。

| # | 指摘 | 確かめた結果 | 分類 |
|---|---|---|---|
| R1 | `col + TimeSpan`（`-` も）が SQLite（と恐らく MySQL）で数値の加算になる | SQLite: `2024-01-01 10:00:00` + 2 時間 → `2026`、- → `2022`。MySQL: `20240101120000`（数値。桁が繰り上がらない値なので時刻に見えるだけ）。SQL Server: `Operand data type datetime2 is invalid for add operator`（大きな失敗）。PostgreSQL 16・Oracle XE 21: 意図どおり。→ **F3** | (a) `PlusOperand` / `MinusOperand` × `TimeSpan` のセルはあった（期待「値として束縛して実行」）。エンジンの Example は `+` × {文字列, char} だけで、TimeSpan を当てていなかった |
| R2 | MySQL の UPDATE で、後の SET 代入が前の代入で書いた値を読む | MySQL 8.0: `(b, g) = (50, 1)` を入れ替える文で `(1, 1)`。PostgreSQL・SQLite・SQL Server 2022・Oracle XE 21: `(1, 50)`。→ **F4** | (a) 文脈「SET 代入」× 値「列」のセルはあった（期待「書いたとおり実行」）。SET 代入は Build 段だけで、エンジンの Example が無かった |

(b)（モデルの外）は 0 件なので、軸や値は足していない。ただし (a) の 2 件は「値の位置を代表 1 つで
済ませる」導き方の穴を示している。手順 2 からやり直すなら、範囲は次のとおり:
- 値の位置の区分（`PlusOperand`、`MinusOperand`、`ArithmeticOperand`、比較、代入）ごとに、
  エンジンに届く値すべてを当てる（今は SELECT 項目だけが全値）。
- 演算子の区分では、左辺の型（日時・文字・数値の列）も見分ける軸として足す。今のモデルは
  右辺の値だけを見ていて、左辺が日時のときの読み方は表せない。
- SET 代入（と ON DUPLICATE KEY UPDATE）の文脈を、全流れのエンジン Example に入れる。

逆向きの比較: レビューは F1・F2 を見つけなかった（「整数・decimal・束縛値のキーは記録と twin で
閉じている」と判断していた）。記録が「確かめた」と書く方言の組の外を、記録を信じたまま
読んだためと見ている（推定）。

## 6. 見つけた不具合

### F1: MySQL で `Bind(n)`（整数）を ORDER BY / GROUP BY に置くと、列 n の位置として読まれる

- **呼び出し**: `Select(t.Id, t.B).From(t).OrderBy(Bind(2), t.Id)`。SqlArtisan の出す文は
  `SELECT ... ORDER BY ?0, `t`.id`（MySQL のマーカー）、パラメーター `2`。
- **起きること**（MySQL 8.0、SqlArtisan.Dapper + MySqlConnector 2.4.0、CI run 41159339490 と
  41159686928 の MySql 流れ）:
  - 文の ORDER BY: 列 2（b）で並ぶ（5,4,3,2,1）。ほかのエンジンでは定数（1,2,3,4,5）か大きな失敗。
  - 派生表・CTE の中の ORDER BY + LIMIT: 取る行が変わる（{4,5}、期待は {1,2}）。
  - 文の GROUP BY（`SELECT COUNT(id), b ... GROUP BY ?`）: 列 2 でグループ化（5 グループ）。
    定数なら ONLY_FULL_GROUP_BY で失敗するはずの文が黙って通る。
  - 複合の末尾 ORDER BY: 列 2 で並ぶ（問い Q13 のセル）。
  - `OVER (ORDER BY ?)` / `PARTITION BY ?`: `uses legacy position indication which is not supported`
    で失敗、`GROUP_CONCAT(... ORDER BY ?)`: `Unknown column '2'` で失敗（大きな失敗）。
- **食い違う記録**: `guards-and-empty-states.md` § bound-value の「`Bind(value)` keeps a deliberate
  constant writable」。さらに `OrderByClause.cs:84-88` の例外メッセージは、ウィンドウの中の数値を
  拒むときに「`Bind(value)` for a deliberate constant」を勧める。MySQL でそれに従うと、ウィンドウでは
  大きな失敗、文の ORDER BY に同じ考えで書くと黙って位置になる。
- **原因の見立て（未確認）**: MySqlConnector が既定でパラメーターを文字として埋め込んでから送るため、
  `?` が `2` という整数リテラルになる、と見ている。ドライバーの文書・ソースでは確かめていない。
  確かめたのは「結果が整数リテラルを書いたときと同じになる」ことだけである。
- **確かめ方**: 作業ブランチで `integration.yml` を手動実行し、MySql ジョブのログの
  `E1-ORD-BindInt` / `E3-ORD-Subquery-BindInt` / `E3-ORD-Cte-BindInt` / `E1-GRP-BindInt` の行を見る。

### F2: Oracle で整数でない数値の ORDER BY キーが列の位置として読まれる


- **呼び出し**: `Select(t.Id, t.B).From(t).OrderBy(2.5, t.Id)`（`2.5m`、`2.0` も同じ）。
  SqlArtisan は `ORDER BY 2.5, "t".id`（`2.0` は `ORDER BY 2.0, ...`）を出す。
- **起きること**（Oracle XE 21.3.0 と 23.26.3、CI run 41159339490 / 41159686928）: 列 2（b）で
  並ぶ（5,4,3,2,1）。派生表・CTE の中でも取る行が変わる（{4,5}）。複合の末尾 ORDER BY でも位置。
  MySQL と SQLite では定数（並べない）、PostgreSQL と SQL Server では `Build()` が拒む。
- **食い違う記録**: `guards-and-empty-states.md:218-221`（Bounded exception）の「MySQL, SQLite and
  Oracle accept the no-op ordering」と、`OrderByItemResolver.cs:87-88` の「a decimal point ... so a
  whole value cannot silently re-read as an ordinal」。Oracle は `2.0` も `2.5` も位置として読む。
  `ArgumentValueValidity` には ORDER BY のこの値の行が無く、アナライザーも黙る。
- **GROUP BY は別**: Oracle の `GROUP BY 2.5` は定数として扱われた（b を選ぶと ORA-00979）。
  記録（#684）と合っている。
- **確かめ方**: 作業ブランチで `integration.yml` を手動実行し、Oracle / Oracle23ai ジョブのログの
  `E1-ORD-Double` / `E1-ORD-Decimal` / `E1-ORD-DoubleWhole` / `E3-ORD-*-Double*` の行を見る。

### F3: SQLite と MySQL で `日時列 + TimeSpan`（`-` も）が数値の加算になる（レビュー R1 由来）

- **呼び出し**: `Select(t.Column("d") + TimeSpan.FromHours(2)).From(t)`。出す文は
  `SELECT ("t".d + :0) FROM vr_t "t"`、パラメーターは TimeSpan。
- **起きること**（run 41160892388。SQLite はこのコンテナでも）: SQLite は `2026`（年の数値 +
  `'02:00:00'` の先頭 2）、MySQL は `20240101120000`（日時を数値にして足した値）。PostgreSQL 16・
  Oracle XE 21 は `2024-01-01 12:00:00`、SQL Server 2022 は大きく失敗。
- **食い違う記録**: `+` の隣の文字列・char には SQLA0104 の行がある（ADR 0022 第 6 の値域、#742）が、
  TimeSpan には行も文書の注記も無い（`docs/functions.md` は TimeSpan を束縛できる型として挙げるだけ）。
  モデルのセルの期待も「値として束縛して実行」にとどまり、エンジンの読み方を書いていなかった。
- **確かめ方**: SQLite は
  `dotnet test tests/SqlArtisan.IntegrationTests --filter "Engine=Sqlite&FullyQualifiedName~ValueReading"` の
  `E4-PLUSDATE-TimeSpan` / `E4-MINUSDATE-TimeSpan`。MySQL は統合テストの MySql ジョブの同じ行。

### F4: MySQL で、後の SET 代入が前の代入で書いた値を読む（レビュー R2 由来）

- **呼び出し**: `Update(u).Set(u.Column("b") == u.Column("g"), u.Column("g") == u.Column("b"))
  .Where(u.Column("id") == 1)`。出す文は全方言で `UPDATE vr_t SET b = g, g = b WHERE id = :0`（マーカー以外同じ）。
- **起きること**（run 41160892388）: MySQL 8.0 は `(b, g) = (1, 1)`。PostgreSQL・SQLite・SQL Server
  2022・Oracle XE 21 は入れ替わって `(1, 50)`。レビューは MySQL の文書（単一表の UPDATE は左から
  評価）を挙げたが、文書は記憶からで未取得（私も取得していない）。確かめたのはエンジンの結果だけ。
- **食い違う記録**: 記録に該当する条項は見つからなかった（UPDATE の節にも注記なし）。
  `OnDuplicateKeyUpdate(...)` で同じことが起きるかは確かめていない。
- **確かめ方**: 統合テストの MySql ジョブの `E4-SETSWAP-Column` の行（取り消すトランザクション内で実行）。

## 7. 人への問い

事実はこの試しで測れたが、意図した結果は人が決めるものを含む。

| # | 問い | 測れた事実 |
|---|---|---|
| Q1 | 依頼の「sql-building-style.md の rule 8」はどの記録か。そのファイルは rule 7 まで。 | — |
| Q2 | `OrderBy(-1)` は MySQL・Oracle で定数として通る（Bounded exception）。`GroupBy(-1)` は「読むエンジンは別の意味で読む」として全方言で拒む（#684）。ORDER BY も同じ規則にするか、非対称は意図か。 | MySQL・Oracle とも定数（1..5）。Oracle の複合では ORA-01785。 |
| Q3 | `OrderBy(2.5)` に SQLA0104 の行は要るか。記録は「アナライザーから見えない」と書くが、リテラルは呼び出し側の定数で、`GroupBy(2.5)` には行がある。（F2 の直し方とも絡む） | MySQL・SQLite は定数、Oracle は位置（F2）。 |
| Q4 | MySQL での `Bind(n)` キーの意図した意味は、記録の約束する定数か、ドライバーの結果か。（F1） | 位置になる（F1）。 |
| Q5 | SQL Server での `ORDER BY @0` / `GROUP BY @0` / `OVER (ORDER BY @0)` の結果を記録するか。 | 文の ORDER BY・GROUP BY・ROLLUP は大きな失敗、OVER・PARTITION BY・STRING_AGG WITHIN GROUP は定数として実行。 |
| Q6 | `Sql.Null` をキーにしたときの MySQL・Oracle・SQL Server での結果を記録するか。 | MySQL・Oracle: ORDER BY NULL は定数、GROUP BY NULL は 1 グループ、PARTITION BY NULL は 1 区画。SQL Server: ORDER BY NULL・OVER (ORDER BY NULL) は失敗、GROUP BY NULL も失敗。 |
| Q7 | `OrderBy(subquery)` が `Invalid type for OrderByItem` で拒まれるのは、`GroupBy(subquery)` と同じ決定か（記録は GROUP BY だけ）。 | — |
| Q8 | ウィンドウ・集約の ORDER BY の数値（ライブラリは全方言で拒む）について、MySQL・Oracle・SQL Server の読み方を記録するか。 | MySQL は `OVER (ORDER BY 2)` 相当を「legacy position indication」で拒む（F1 の経路で観測）。Oracle・SQL Server の束縛値は定数。 |
| Q9 | SUBSTR の開始 0・負、INSTR の位置、REGEXP_* の位置・出現回数など、エンジンごとに整数の意味が違う関数引数は、この型に入るか（ROUND の負の桁は #742 で SQLA0104 に入った）、それとも ADR 0003 の「エンジン定義の意味、文書の問題」か。 | 測っていない。 |
| Q10 | 値の位置でのドライバーの型の対応（Oracle 21c の bool、Guid、SQLite / MySQL の TimeSpan・DateTimeOffset）はこの型に入るか、ADR 0004 のドライバーの問題か。 | Oracle XE 21: bool の読み戻しで InvalidCastException。Oracle 両版: Guid で ArgumentException。ほかは値が返る（表現は流れごとに違う）。 |
| Q12 | INSERT / MERGE / VALUES の行の C# null が NULL を入れる（#169）のは、§ Null arguments（「Sql.Null を渡せ」）の記録された例外か。規則の条項に名前が無い。 | Build 段で確認（全方言で `NULL` を刷る）。 |
| Q13 | 複合の末尾 ORDER BY に束縛値・`Sql.Null`・修飾列・式を置いたときの MySQL・Oracle・SQL Server の結果を記録するか。 | MySQL: Bind は位置（F1）、NULL と 2.5 は定数、修飾列は失敗。Oracle: Bind・NULL は ORA-01785、2.5 は位置（F2）、修飾列は ORA-00904。SQL Server: Bind・NULL は失敗、修飾列は実行。 |

（Q11 は試しの途中で取り下げた: 派生表に件数を付けたので SQL Server でも ORDER BY が書ける。）

## 8. 「モデルの範囲で網羅し、確かめた」と言えるか

**言えない。** 言えるのは次の 2 つまで。

- Build 段: 724 位置 × 29 値 × 5 方言を総当たりし、モデルの期待と外れは 0（届かない 465 件を除く）。
- エンジン段: キーの 6 区分について、方言ごと・8 本の流れすべてで、届く値の全種類を見分けるデータで
  確かめた。ここで F1・F2 が出た。

足りないもの:

0. **値の位置と演算子をエンジンで確かめていない**: レビューの F3・F4 がこの穴から出た。セルの期待が
   「束縛して実行」で止まっていて、エンジンの読み方（演算子の左辺の型、代入の評価順）を表していない。

1. **委ねたセル 846**: 文字列の位置（ADR 0016、`ForcedLiteralSweepTests`）、DateTimePart の値
   （`DatepartCells`）、`BindArray`、刷られる数値の値域。既存のゲートがあるが、このモデルの中では
   確かめていない。
2. **値の位置を代表で済ませた**: `ValueArgument` 410 位置などはエンジン段で SELECT 項目 1 つに
   代表させた。Build 段で同じ解決経路だと確かめたが、関数ごとの意味（Q9）は見ていない。
3. **文脈の一部**: 相関サブクエリは Example が無い（既知の例に出ていないため規則どおり外した）。
   SET 代入の文脈は Build 段だけ。文脈 × 値は文の ORDER BY と集約の ORDER BY だけを当てた。
4. **問い 12 件が未決**: 期待結果そのものが決まっていないセルが 288 ある。
5. **版**: MySQL は 8.0 の 1 点。Oracle の `2.0` のトップレベルは XE 21 のログ末尾に入らず、行を
   読んでいない（23ai では観測）。

## 9. かかった時間と手間

ツールの実行時刻（UTC）から読んだ概算。考える時間を含む。開始時刻は記録していない。

| 段階 | 時間 | 主な手間 |
|---|---|---|
| issue・記録を読む | 開始〜01:56 頃 | 10 件の issue / PR、規則 2 本、ADR 6 本 |
| 表を作る（数え上げ・区分・期待結果） | 01:56〜02:10（約 15 分） | 区分の規則と根拠を書く。プローブで外れを 0 にするまでに 5 回直した（ほぼモデルとプローブの側） |
| Example を導く・ローカルで確かめる | 02:10〜02:17（約 7 分） | テンプレートと見分けるデータ、SQLite・PostgreSQL 16 で 3 回 |
| 統合テスト | 4 回（02:17、02:22、02:35、02:38 に起動。各回 2 分前後で全流れ終了） | ワークフローを作業ブランチで絞る、ログから結果を読む（成果物 zip は取得できず、ログは 1 流れずつ取得） |
| 誤りの測定 | 2 分 50 秒（参考）、3 分 8 秒（単体）、6 分 30 秒（単体 + 試し）、7 分 16 秒（Example 追加後） | 行範囲指定の失敗の切り分け、生き残り 29 個の分類 |
| レビュー | サブエージェント約 18 分（並行）、私の確かめは Example 3 件の追加と統合テスト 1 回 | 指摘の確かめ |
| 報告 | 約 15 分 | |

## 10. スキルにするときの判断材料

### 既存スキルとの比較（この試しで分かった範囲）

| | この試し | sa-clause-audit | sa-diff-review | sa-panel-audit |
|---|---|---|---|---|
| 見る範囲 | 1 つの危険の型を、全位置 × 全値 × 全方言で | 1 単位の、規則の条項ごと | ブランチの差分 | 名指しした範囲を 3 モデルで |
| 見つけたもの | F1・F2（記録が確かめた方言の組の外で、エンジンでしか見えない黙った誤り） | （今回は走らせていない） | （今回は走らせていない） | （今回は走らせていない） |
| 見落としたもの | F3・F4（モデルの中だが Example を代表で済ませた所）。手順 5 のレビュー 1 回が見つけた | | | |
| 見つけにくいもの | 型の外（文字の位置、関数ごとの意味） | エンジンの観測が要る誤り（読むだけでは F1・F2 は出にくい。既知の例の issue はどれも「live」で確かめて書かれている） | 差分の外 | 同左 |

既存スキルは今回走らせていないので、それらとの比較は推定である。手順 5 のレビュー（普段どおりの
1 回）との比較ははっきりしていて、**見つけたものが重ならなかった**: モデル側は F1・F2、レビュー側は
F3・F4。手間は、モデル側が表作りから統合テストまで約 40 分（その後の回帰は数分）、レビューは
サブエージェント約 18 分・トークン約 28 万。

### 置き換えか、足すか

**足すほうに向く。** 根拠:

- 今回の結果では、モデルとレビューが見つけたものが重ならなかった。どちらか片方に置き換えると、
  もう片方が見つけた 2 件を落とす。
- この方法が強いのは「エンジンが黙って別の意味で読む」ことの網羅で、既存スキルは読む・差分を見る
  ことが中心。重なりが小さい。
- 表を作るには型ごとに区分と根拠を書く手間がかかり、差分レビューの代わりに毎回走らせるものではない。
- 一方で、一度作った表は回帰のゲートとして安く回る（Build 段 104,980 件で約 3 秒、エンジン段は流れ
  あたり 20〜75 秒）。

### 決まった形にできるところ / 人の判断が残るところ

決まった形にできる:
- 位置の数え上げ（反射）と「全位置が区分に入る」ゲート。
- 値の種類のサンプル、コンパイルできるかの判定、Build 段の総当たりプローブと照合。
- Example の導き方（区分 × 届く値、文脈は既知の例から）、見分けるデータの型紙（列 2 が id と逆順、
  キーの後ろに id）。
- 統合テストの実行と、ログからの結果の抜き出し。
- Stryker の設定と生き残りの一覧。

人の判断が残る:
- 区分を決めること（どの解決経路が「同じ意味」か）。最初の数え上げで漏れはゲートが捕まえたが、
  区分の境目そのものは判断。
- 各セルの期待結果と根拠。特に「記録の前提がエンジンで偽だった」とき、記録とエンジンのどちらを
  期待にするか（F1・F2 は記録を期待に置いたので外れとして出た）。
- 型の境界（Q9・Q10）。
- 生き残った誤りの分類。

## 11. ほかの型にも広げる価値

**ある。** 根拠:
- この型では、既知の 10 件の issue / PR が直した後の状態に対して、新しい黙った誤りが表から 2 件出た。
  どちらも記録が「確かめた」と書いた方言の組の外（MySQL の束縛値、Oracle の非整数）で、表を全方言に
  広げたことで初めて当たった。
- 一方で、表の中なのに Example を代表で済ませた所からレビューが 2 件出した。広げるなら「代表で
  済ませない」Example の導き方とセットにする必要がある。
- 数え上げ・プローブ・Example の仕組みは型によらず使える。型ごとに作り直すのは「区分」「期待結果」
  「見分けるデータ」の 3 つ。

広げるときの注意:
- 区分と見分けるデータを作るのに、この型で約 20 分かかった。型が「行の数え方」（NULL の扱いなど）の
  ようにデータの作り方が難しいものだと、もっとかかる見込み（推定）。
- 期待結果の根拠を記録から拾う作業で、記録の読み落としが 3 件あった。表を作る人と記録を書いた人が
  違うと起きやすい。

## 12. ファイル

| パス | 中身 |
|---|---|
| `tests/SqlArtisan.IntegrationTests/ValueReading/PositionInventory.cs` | 位置の数え上げ |
| `.../ValueKinds.cs` | 値の種類とサンプル、コンパイル判定 |
| `.../ValueReadingModel.cs` | 区分・期待結果・根拠・問い（表の本体） |
| `.../PositionProbe.cs`、`ValueReadingModelTests.cs` | Build 段の総当たり、区分のゲート |
| `.../EngineExamples.cs`、`EngineExampleLanes.cs` | エンジンの Example と 8 本の流れ（+ このコンテナの PostgreSQL 用） |
| `.../ModelDumpTests.cs`、`InventoryDumpTests.cs` | 表と数え上げの書き出し |
| `trials/value-reading/model-cells.tsv` | 表（9,744 セル） |
| `trials/value-reading/position-inventory.tsv` | 724 位置 |
| `trials/value-reading/build-level-probe.tsv.gz` | Build 段の 104,980 件 |
| `trials/value-reading/engine-results.md` | エンジンの結果の抜き出し |
| `trials/value-reading/stryker-*.json`、`stryker-results.md` | 誤りの測定の設定と結果 |
| `.github/workflows/integration.yml` | 作業ブランチだけの変更（試しの Example だけを詳しい出力で走らせる） |

ローカルでの走らせ方:

```bash
# DB なし（区分のゲートと Build 段の総当たり）
dotnet test tests/SqlArtisan.IntegrationTests --filter "FullyQualifiedName~ValueReadingModelTests"
# SQLite と、手元の PostgreSQL（VR_PG_CONNECTION を設定）
VR_PG_CONNECTION="Host=localhost;Username=postgres;Password=postgres;Database=postgres" \
  dotnet test tests/SqlArtisan.IntegrationTests \
  --filter "Engine=Sqlite&FullyQualifiedName~ValueReading|Engine=LocalPostgreSql"
```

## 13. 再測定: 値の位置にもエンジンで全値を当てる（E5）

§ 8 の 0 番（値の位置をエンジンで代表 1 つしか当てていない）を埋めるための再測定。
コード: `tests/SqlArtisan.IntegrationTests/ValueReading/ValuePositionExamples.cs`、`ValuePositionOracle.cs`。
統合テスト run 41162510744（8 本すべて実行）と、このコンテナの SQLite・PostgreSQL 16。

### 導き方

- **位置**: 比較 12（`==` `!=` `<` `<=` `>` `>=` `Like` `NotLike` `Between` `NotBetween` `In` `NotIn`）、
  算術 5（`+` `-` `*` `/` `%`）、単純 CASE の WHEN 値、`Coalesce`、INSERT の値、UPDATE の SET 値、
  SELECT 項目、THEN、`Bind`、`Abs`、`Upper`。演算子は 1 つずつ別の位置として当てた。
- **左辺の型**（新しい軸）: 整数列・文字列・日時列。比較・算術・CASE・Coalesce・INSERT・SET に掛けた。
- **値の種類**: 29 すべて。
- 1 本の流れあたり 1,972 件、8 本で計 15,776 件。

### 判定の仕方

- **意図した結果が一つに決まる組**（値の種類と左辺の型がそろう組。例: 整数列 `<` 2、日時列 + TimeSpan、
  文字列 `Like` 'b'）は、種データから C# で意図した結果を計算して照合した（`ValuePositionOracle`）。473 件。
- **一つに決まらない組**（型がそろわない組、整数の割り算、時差、ドライバーの型の対応）は、正解を置かず、
  エンジンどうしで「値を返すか、失敗するか、返す値は同じか」を比べた。1,499 件（うち 306 件は全方言で
  Build が拒む）。

### 結果

**意図した結果が決まる組（473 件 × 8 本）**: 黙って違う結果を返したのは F3 だけ（SQLite と MySQL の
`日時列 ± TimeSpan`、計 4 件）。ほかの外れはすべて大きな失敗（SQLite の ANY、PostgreSQL の `整数列 LIKE NULL`、
SQL Server の `datetime2 + time`、Oracle の BindNull を CASE・COALESCE に置いたとき、Oracle XE 21 の bool
の読み戻し）。PostgreSQL 17 は 16 と、SQL Server 2025 は 2022 と、全 1,972 件の結果が一致した（ハッシュ一致）。

**意図した結果が決まらない組（エンジンに届く 1,193 件）**:

| どうなったか | 件数 |
|---|---|
| 全方言で失敗 | 18 |
| 全方言で同じ値 | 66 |
| 全方言で値を返すが、値が違う | 19 |
| 一部の方言だけ値を返し、返した値は同じ | 880 |
| 一部の方言だけ値を返し、返した値が違う | 210 |

値を返した件数（黙って受け入れた件数）は方言で大きく違う: SQLite 1,157、MySQL 1,093、SQL Server 299、
Oracle 23ai 281、Oracle XE 21 253、PostgreSQL 113。

「値が違う」229 件（19 + 210）を族に分けた:

| 族 | 例 | 方言ごとの読み方 | 記録 |
|---|---|---|---|
| 型の違う比較 | `文字列 < 2`、`整数列 == "b"`、`整数列 == true` | MySQL は文字を数にして比べる（全行が該当）、SQLite は型の順で比べる（該当なし）。PostgreSQL・SQL Server・Oracle は多くが失敗 | SQLA0205 の領分（テーブルクラスにメタデータがあるときだけ報告。今回の試しのテーブルには無い） |
| 日時列と数・日時の算術（**F5 候補**） | `日時列 + 2`、`日時列 - 日時` | `+ 2`: Oracle は 2 日足す、MySQL は日時を数にして足す（`20240101100002`）、SQLite は年に足す（`2026`）、PostgreSQL・SQL Server は失敗。`日時 - 日時`: PostgreSQL・Oracle は間隔、MySQL は数の差、SQLite は 0 | 見つからない |
| 整数どうしの割り算（**F6 候補**） | `整数列 / (SELECT MAX(...))`、`b / (b + id)` | PostgreSQL・SQLite・SQL Server は切り捨て（1,1,0,0,0）、MySQL・Oracle は小数。Oracle XE 21・23ai は `b / (b + id)` の桁数が .NET の decimal に収まらずドライバーが失敗 | 見つからない |
| 整数列に小数を書く（**F7 候補**） | `INSERT ... VALUES (2.5)` を整数列へ | PostgreSQL は double なら 2・decimal なら 3、SQL Server は 2、MySQL・Oracle は 3、SQLite は 2.5 のまま保存 | 見つからない |
| `+` の文字列 | `文字列 + "b"` | SQL Server は連結、MySQL・SQLite は 0 | SQLA0104（#742） |
| ドライバーの型の対応 | DateTimeOffset・TimeSpan・DateTime を値や文字列列に | 表記がエンジンとドライバーで違う | 問い Q10 |

`Upper(数)` や `Coalesce(整数, 日時)` など、型の変換の表記が違うだけのものも同じ表に入る（残りの件数）。

### この再測定で分かったこと

- 意図した結果が決まる組では、新しい不具合は出なかった（F3 を全方言で確かめ直しただけ）。値の位置の
  解決そのものは、エンジン段でもモデルどおりだった。
- 新しく見えたのは、**意図した結果が一つに決まらない組で、エンジンが黙って別々の答えを返す**ものだった。
  F5〜F7 はどれも記録が見つからない。ただし「呼び手の意図」が一つに決まらないので、不具合と言い切れず、
  候補にとどめた（問い Q14〜Q16）。
- **見分けるデータの穴が 1 つあった**: `整数列 / 2` は、列の値が 10 の倍数なので、切り捨てと小数で答えが
  同じになった。整数の割り算の違いは、`/ (b + id)` と `/ (SELECT ...)` でしか見えなかった。演算子ごとに
  「誤った読み方で答えが変わるか」を確かめる手順が要る。
- モデルの側: 今の表には左辺の型の軸が無く、値の位置のセルの期待は「値として束縛して実行」で止まっている。
  型がそろわない組の期待は、SQLA0205 の領分か、問いにするかを、区分ごとに決め直す必要がある。

### 割合（前の回答の物差しで）

エンジンで実際に当てた位置は、約 13 位置から約 40 位置（724 位置の約 5.5%）に増えた。値を受け取る
区分 35 のうち、エンジンで全値を当てた区分は 7（キーの 6 と SELECT 項目）から 17 になった（残りは文字列・char・enum・件数・
刷られる数値・非 SQL など、委ねた区分と構造の区分、および MySQL だけの AGAINST・OUTPUT / RETURNING・
`new BindValue`・INTERVAL の `+`）。

### 追加の問い

| # | 問い |
|---|---|
| Q14 | 日時列と数・日時の算術（`d + 2`、`d - d`）を、この型として扱うか（Oracle は日数、MySQL・SQLite は数として読む）。扱うなら SQLA0104 の行か、文書の注記か。 |
| Q15 | 整数どうしの `/` の切り捨て・小数の違いは、ADR 0003 の「エンジン定義の意味、文書の問題」か、この型か。 |
| Q16 | 整数列に小数を書いたときの丸め・切り捨て・そのまま保存の違いは、この型か、エンジンの型変換（ライブラリの外）か。 |

ファイル: `e5-baseline-pg16.tsv`（最初に作った PostgreSQL の基準。最終の判定は C# の照合に切り替えた）、
`e5-local-PostgreSql.tsv.gz`・`e5-local-Sqlite.tsv.gz`（このコンテナの全件）、`e5-ci/*.txt`（CI 6 本の
要約・外れ・符号行）。
