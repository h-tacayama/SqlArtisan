# v1.0 リリース仕上げレビュー — 手順と進捗

v1.0.0 公開前の最終レビューの手順書。進捗はこのファイルのチェックボックスで管理する。
作業ブランチ: `claude/v1-0-release-review-01l4zw`

凡例: `[ ]` 未着手 / `[~]` 実施中 / `[x]` 完了 / `[-]` スキップ(理由を記載)

---

## Phase 1 — 品質ゲート(機械的検証)

コードを読む前に、リポジトリが「常に守るべきゲート」を全て通ることを確認する。

- [x] `dotnet build SqlArtisan.sln`(エラーゼロ。警告は NU1903 のみ → 検出事項 F-1)
- [x] `dotnet test tests/SqlArtisan.Tests` — 1045 件全て成功
- [x] `dotnet test tests/SqlArtisan.Analyzers.Tests` — 1082 件全て成功
- [x] `dotnet test tests/SqlArtisan.TableClassGen.Tests` — 158 件全て成功
- [x] `dotnet format SqlArtisan.sln --verify-no-changes` — 違反なし(exit 0)

結果メモ: 全ゲート通過。唯一の警告は IntegrationTests の推移的依存
`SSH.NET 2023.0.0` の既知脆弱性(NU1903、GHSA-q939-rpr3-3284)→ F-1。

## Phase 2 — リリース成果物の点検

パッケージとして出荷されるメタデータ・ドキュメントの整合を確認する。

- [x] `Directory.Build.props` のバージョン確認(現在 `0.8.0-beta.1`。1.0.0 への
      引き上げは Phase 7 の判断事項)
- [x] 4 パッケージ(`SqlArtisan` / `ArrayBind` / `Dapper` / `TableClassGen`)の
      csproj メタデータ(説明・タグ・README 同梱・ライセンス)の点検 — 内容は揃って
      いる。軽微な観察は F-3(著作権年)のみ
- [x] `CHANGELOG.md` — Unreleased は Docs / Added / Changed / Fixed に整理済みで、
      Breaking も明示されている。1.0.0 セクション化はバージョン引き上げ時(Phase 7)
- [x] `docs/versioning.md` — 「From 1.0, SemVer に従う」という表明は 1.0 公開後も
      そのまま正しい書き方になっており修正不要
- [x] `dotnet pack` ドライラン — 4 パッケージ + snupkg 生成成功。主パッケージに
      アナライザー DLL・buildTransitive props・README 同梱を確認。release.yml は
      4 プロジェクトを個別 pack しており Benchmark の混入はない
- [x] README — 構成・バッジ・サンプルは 1.0 の顔として成立。ただしインストール節の
      `--prerelease` 表記は公開時に除去必須 → F-2

結果メモ: F-1 は SSH.NET 2026.0.0 の直接参照で解消(修正済み)。F-2 はバージョン
引き上げと同時に対応(Phase 7)。

## Phase 3 — ドキュメント監査(コーパス全体)

`sa-docs-audit` スキルで README / docs/ / llms.txt / CHANGELOG を網羅監査する。
リンク・API カバレッジ・用語・出力 SQL の実証検証と、敵対的検証パスを含む。

- [x] `sa-docs-audit` 実行 — 同梱4スクリプトは全て緑:リンク解決(14ファイル)、
      API 網羅は欠落・幽霊なし(公開ファクトリ210)、用語・空白違反なし、
      ドキュメント記載の SQL 例 104/104 が実出力と一致。README のランディング主張
      (ベンチマークのスコープ付き表現・計測環境の明記)も確認済み。
      敵対的検証パス(独立 sa-reviewer)完了
- [x] 検出事項のトリアージと修正 — 検出3系統、全て修正済み(検出事項ログ D-1〜D-3)。
      修正後にスクリプト4本を再実行し全て緑、`llms-full.txt` 再生成済み

結果メモ: 敵対的検証は Medium 1件(全文検索の前提条件の過剰主張)+ Low 2件
(ADR 0020 境界違反)を検出。ベンチマークの最上級表現・検証済みエンジン主張・
バージョン境界レジスタ等への反証は全て不成立(主張は維持)。

## Phase 4 — 公開 API サーフェスのパネル監査

リリース後に変更しづらい「公開 API」を、3 モデル独立パネル(`sa-panel-audit`)で
現状ベース監査する。スコープは公開サーフェスに限定:

- `src/SqlArtisan/Sql/Sql.*.cs`(公開ファクトリ)
- `src/SqlArtisan/SqlBuilder/`・`src/SqlArtisan/SqlPart/`・`src/SqlArtisan/Metadata/`(公開型)

- [x] `sa-panel-audit` 実行 — スコープ 55 ファイル(約 6,200 行)、3 座席
      (Sonnet / Opus / Fable)全て報告完了(3-of-3 パネル成立)
- [x] 検出事項の裁定(主要ソースに対する再導出)と修正 — 6 系統全て CONFIRMED、
      全て修正済み(検出事項ログ P-1〜P-6)
- [x] 修正後状態への最終レビューパス(sa-reviewer 1体)— 判定「マージ可」。
      全ガードのメッセージ逐語一致・修正前挙動の再現・正規経路の無変化を実機検証済み。
      検出は Low 1件(ガード規約台帳への記載漏れ)のみ → 修正済み

結果メモ: 座席間の収束は P-1(Sonnet+Fable)、P-2(3座席全員)。P-3(High)は
Opus のみが検出 — 単独検出でも裁定側ハーネスで再現確認のうえ採用(多数決はしない)。
P-4 は Fable が SQLite 実機で「全方言で無効」ではないことまで検証済みのため、
文法違反ではなく規約の非対称(AsTable は投げるのに As は素通し)として修正。

## Phase 5 — 統合テスト(実エンジン検証)

出力 SQL が実際のエンジンで通ることの最終確認。`sa-run-integration-tests` スキルに従う。

- [x] 統合テスト実行(MySQL / Oracle / PostgreSQL / SQL Server / SQLite)
  - SQLite レーンはローカル実行で 71/71 成功
  - `integration.yml` の workflow_dispatch(run #235)で 6 レーン全て success:
    MySql / Oracle / Oracle23ai / PostgreSql / Sqlite / SqlServer。
    SSH.NET 2026.0.0 更新込みのコミットで実行(F-1 修正の実挙動確認を兼ねる)
- [x] MatrixSweepTests(アナライザー方言マトリクスの実機検証)— 上記実行に含まれ緑

結果メモ: 全レーン緑。ガード修正込みの最新 head(5078793)でもマトリクスを
再実行し、run #236 として 6 レーン全て success を確認 — Phase 5 完了。

## Phase 6 — ベンチマーク確認(任意)

README の比較数値が現状のコードと乖離していないかの確認。相対比較と B/op のみ有効。

- [x] `sa-run-benchmark` — validate モードで全エントラントの等価性確認後、
      全ビルダー比較を実行(このコンテナでは相対順位と B/op のみ有効)

結果メモ: README の主張は現コードで成立 — SqlArtisan はビルダー中、最小割当
(2.22 KB vs Sqlify 3.13 / InterpolatedSql 4.95 / DapperSqlBuilder 5.49 /
linq2db 19.31 / SqlKata 60.94)かつ最速(1,473 ns、次点 Sqlify 1,895 ns)。
`SqlArtisan_DapperDynamicParams` の 2.84 KB は README 記載値と完全一致。
SqlKata の増加(40.54→60.94 KB ≒ 1.5 倍)も README 脚注³の記載どおり。
絶対値の微差(2.16→2.22 KB 等)はランタイム差(README は .NET 8.0.28 で計測、
本実行は .NET 10)によるもので、README は計測環境を明記済みのため修正不要。

## Phase 7 — 最終判定とリリース手順

- [x] Phase 1–6 の検出事項が全て修正済みまたは判断待ちとして明示済み
      (残タスクは F-2 / F-3 のみで、いずれもバージョン引き上げと同時に行うもの)
- [ ] バージョン引き上げ(`0.8.0-beta.1` → `1.0.0`)— **ユーザー判断事項**
- [ ] CHANGELOG の 1.0.0 セクション確定(日付入り)
- [x] リリース手順の確認: タグ `v1.0.0` push → `release.yml`(full verify →
      統合テスト → 4 パッケージ pack & push。バージョンはタグではなく
      `Directory.Build.props` から取られるため、タグと props の同期が必須)
- [ ] タグ push — **ユーザー実施事項**(このレビューでは行わない)

### リリースコミットの手順(バージョン引き上げ決定後に 1 コミットで)

1. `Directory.Build.props`: `<VersionPrefix>1.0.0</VersionPrefix>` にし、
   `<VersionSuffix>beta.1</VersionSuffix>` の行を削除
2. **F-2**: プレリリース表記の除去(4 ファイル)
   - `README.md`: 「Packages are pre-release, so pass `--prerelease`:」の行と
     3 コマンドの `--prerelease` フラグ
   - `docs/guides/dapper-quickstart.md`: 2 コマンド+ツールインストールの計 3 箇所
   - `docs/guides/oracle-array-bind.md`: 説明文 1 行+コマンド 1 箇所
   - `src/SqlArtisan.TableClassGen/README.md`: 注記 1 行+コマンド 1 箇所
3. **F-3**(任意): 4 csproj の `Copyright` を `2025-2026` に更新
4. `CHANGELOG.md`: `## [Unreleased]` → `## [1.0.0] - <日付>` に確定
5. `llms-full.txt` を再生成(`LlmsFullTests.cs` ヘッダーのコマンド)
6. ゲート一式(`dotnet test` ×3、`dotnet format --verify-no-changes`)を通す
7. main へマージ → `git tag v1.0.0 && git push origin v1.0.0`

結果メモ: レビューとしての判定は「**1.0 リリース可**」。コード・docs・パッケージング
の全ゲート緑、3 モデルパネル+敵対的検証の検出は全て修正済み。残るのは
バージョン確定という判断のみ。

---

## 第2ラウンド — レビュー手法の精度検証

第1ラウンドと**同一の手法・同一のブリーフィング**(ゲート数値のみ現状に更新)で
再実施し、手法自体の精度を測る。実施前に裁定基準を固定:

| 分類 | 意味 | 導かれる改善 |
|---|---|---|
| (a) 前回見逃した実欠陥 | 手法の網羅性の穴 | チェックリスト/ブリーフィングに観点を追加 |
| (b) 前回の修正が持ち込んだ欠陥 | 修正品質の穴 | 修正後レビューの強化 |
| (c) 規約が判定を決めきれない指摘 | ゴールの曖昧さ | ルール/ADR に判定基準を明文化 |
| (d) 反証で落ちる指摘 | レビューアのノイズ | 手法は健全(独立コンテキストの分散は正常) |

前提の明文化: パネルレビューは決定手続きではなく**確率的探索**。第2ラウンドの
指摘ゼロは「完璧」の証明にはならず、指摘が出ること自体も手法の欠陥を直ちには
意味しない。意味を持つのは上の分類 — (a)(b) は手法改善の材料、(c) はプロジェクト
側の規約整備の材料、(d) のみが「ノイズ」。

- [x] パネル 3 座席(Sonnet / Opus / Fable)+ docs 敵対的検証 1 体を
      第1ラウンドと同一プロンプトで再実施(4体とも完了)
- [x] 裁定・分類・改善策の導出(下表)

### 第2ラウンドの検出と裁定

| ID | 検出(座席) | 裁定 | 分類 |
|---|---|---|---|
| R2-1 | `Column(DbColumn)`/`Column(ExpressionAlias)` の null → 素の NRE(Sonnet) | **却下**(規約を明文化) | **(c) ゴールの曖昧さ** |
| R2-2 | `default(OutputParameter)` がコンストラクタガードを迂回し `INTO :` を静かに生成(Fable+Opus 収束) | **修正** — フォーマット時バックストップ | (a) 手法の穴 |
| R2-3 | `Cte.As(null)` が防御的 `?.` により `WITH "c" AS ()` を静かに生成(Opus) | **修正** — ctor ガード+`?.` 除去 | (a) 手法の穴 |
| R2-4 | `new BindValue(null)` がファクトリ `Bind(null)` と非対称に素通り(Fable) | **修正** — ctor に同一ガード | (a) 手法の穴 |
| R2-5 | `DbSequence` のガードメッセージが文法規約違反(Fable) | **修正** | (a) 手法の穴(軽微) |
| R2-6 | `Wait(-1)` がインラインの負値リテラルを生成(Fable) | **保留** — ADR 0012 条件1(全エンジンで無効)の実機証明が必要。フォローアップ課題として記録 | 保留 |
| R2-D1 | README「正規化は**2項目だけ**」が不完全 — `EXCLUDED`/`new`・DML エイリアス `AS`・MERGE `;` も方言正規化(docs 敵対) | **修正** — 「機械的な方言差」+例示に拡張 | (a) 手法の穴 |
| R2-D2 | バインド上限値(2100/999)をゲート外で再述(docs 敵対) | **修正** — 数値を落としエンジンマニュアルへ委譲 | (a) 手法の穴 |

**R2-1 の裁定詳細(このラウンドの核心)**: 同一事象を Sonnet は「欠陥」として提出、
Opus は「アノテーションで強制する方針が一貫していると読める。規約が無いので指摘
にせず作者への質問とする」と提出見送り — 判定が座席間で割れたこと自体が、規約の
未定義の証明。実測: ライブラリ全体で明示 `ThrowIfNull` は 7 箇所 vs 公開ファクトリ
338(NRT 任せが支配的規約)。しかも第1ラウンドで私が入れた `DbColumn` の owner
ガードがこの不文律からの逸脱で、Sonnet はそれを「規約の証拠」として引用していた
(= 第1ラウンドの修正が第2ラウンドの指摘を誘発)。解消として
`guards-and-empty-states.md` に境界を明文化: **静かに受理(ビルドが成功)する
null は型を問わずガード必須/呼び出し箇所でうるさく即死(NRE)する単独の非 null
許容参照パラメータは NRT が契約であり、レビューで指摘しない**。

### 手法の評価と改善(ユーザーの問いへの回答)

1. **第2ラウンドの指摘は「ゼロ」ではなかった** — 修正6件+規約化1件+保留1件。
   内訳が答えを与える: (b)(前回の修正が壊した)は **0件** — 修正品質は健全。
   (d)(ノイズ)も **0件** — 全指摘が裁定側で再現した。残りは (a) と (c)。
2. **(a) が示す手法の性質**: パネルは決定手続きではなく確率的探索であり、各座席は
   誤用空間の異なるスライスを標本抽出する。第2ラウンドの (a) 5件は第1ラウンドと
   同じ欠陥クラス(静かな無効 SQL・ファクトリ/ctor 非対称・数量詞の過剰主張)の
   **別インスタンス**で、新クラスは 1 つもない。つまり手法の観点(what to hunt)は
   正しく、不足していたのは**クラス内の網羅**。改善: 座席が欠陥を 1 件見つけたら、
   そのクラスで表面を掃く(例: 「ガードを迂回できる構築経路 — struct default・
   派生 ctor・防御的 `?.` — を列挙して全て突く」)ことをブリーフィングに明記する。
3. **(c) が示すゴールの曖昧さ**: null ガードと NRT の境界が不文律だった。今回
   `guards-and-empty-states.md` に明文化して解消。曖昧さは「レビューが暴くまで
   見えない」ので、判定が座席間で割れた事象は必ず規約化する — これが精度向上の
   最も確実な経路。
4. **収束の読み方の実証**: R2-2 は 2 座席収束(優先検証シグナルとして機能)、
   R2-3 は Opus 単独・R2-D1 は docs 敵対単独だが全て実欠陥 — 「単独検出でも
   同じ検証を通す/多数決をしない」という裁定規則の正しさを再確認。

第2ラウンド修正後の最終レビューパス(sa-reviewer 1体)も完了 — 4 ガードの
メッセージ逐語一致・正規経路の無変化・README の正規化例の事実性を実機検証し
「マージ可」。検出2件はいずれも**新設した規約文自体**への指摘(「呼び出し箇所で
即死」という前提が `Exists(null)` では Build() 時 NRE で偽/frontmatter の
自動ロードパス漏れ)で、両方修正済み。判定軸「ビルドが成功するか」は維持。

## レビュー成果の恒久化 — ファクトリガード掃引ゲート

第1+2ラウンド最大の欠陥クラス「静かに受理される縮退引数」を機械化した
`FactoryGuardSweepTests` を追加。リフレクションで公開 `Sql` ファクトリ全件
(299 メソッド)に null / 空文字列 / 空配列 / null 要素を注入し、
**「うるさく落ちる(eager または Build() 時)」か「正確な SQL 付きで受理カタログに
登録済み」かのどちらか**でなければ失敗する。

初回実行の成果 — **47 ケースの実欠陥を即検出**(24 箇所のガード追加で解消。
全て「null の句オブジェクトが小オーバーロード用の内部 nullable スロットに流れ、
書いた句が SQL から消える」という同一形状):
- `Avg`/`Count`/`Sum`/`GroupConcat` の `DISTINCT` が null で静かに消失
- 全 `Case(...)` オーバーロードの `ELSE` 腕が null で静かに消失(ctor 2箇所の
  ガードで 22 ファクトリを一括カバー)
- `GroupConcat` の `ORDER BY`/`SEPARATOR`、`StringAgg` の `ORDER BY` が静かに消失
- `IntervalLiteral` のフィールド指定が null で消失(Oracle フィールド形式が
  裸の PG 形式に化ける)
- `ToTsvector`/`ToTsquery`/`PlaintoTsquery` の設定名が null/空で消失または
  空リテラル `''` として生成

全てにガードを追加(ゲート再実行で解消を確認)。正当な 18 ケース(空 params 尾 =
小さい正規呼び出し、`GROUP_CONCAT`/`STRING_AGG` の空セパレータ、`Group()` の
grand-total 行)は正確な SQL 付きでカタログ化し、挙動が回帰契約として固定された。
今後、ガード無しで縮退入力を静かに受理する新ファクトリは CI で自動的に落ちる —
座席がクラス内を掃き切れない問題自体の解消。

**このコミット自体への `sa-diff-review` が2件を検出**(Medium・Low):
`TryBuild` が埋め込めない戻り型を無記録でスキップしており、その死角に
`Sql.Hints(null/"")` という実際のサイレント受理が1件隠れていた(既存コードの
コメントに「意図的」と明記されていたため、ガードではなく理由付きでカタログ化)。
もう1件は「25件」という記述がどの数え方でも再現せず、実測は 47 件だった
(件数を訂正)。両方修正しコミット済み — レビューの入れ子適用が機能した
もう一つの実例。

## 第3ラウンド — メタ層監査(検証インフラ自体の網羅監査)

対象はコードでも user docs でもなく**検証インフラそのもの**:CLAUDE.md、
`.claude/rules/` 7ファイル、`docs/adr/` 21ファイル、ゲート/パリティテスト20本+
`MatrixSweepCatalog`(計約50ファイル・6,800行)。観点は5つ:

1. 規約↔強制の双方向パリティ(規約の事実主張がコードに対して真か/強制済みの
   慣習が規約に書かれているか)
2. ゲートのカバレッジ vs 主張(無記録スキップ・skip-not-fail なパーサ・
   陳腐化検出のないカタログ)
3. 指摘→ゲート台帳の完全性(本リリースレビューの全指摘クラスが
   ゲート化/規約化/明示的見送りのいずれかに着地しているか)
4. 曖昧さ(2人のレビューアが異なる判定を下しうる規約の未定義領域)
5. ゲートテスト品質(守っている性質が退行したら本当に落ちるか —
   スクラッチパッドでのコピー変異プローブ許可)

- [x] パネル 3 座席(Sonnet / Opus / Fable)完了(初回はセッション利用上限で
      3 座席全滅 → リセット後に同一ブリーフィングで再実施、3-of-3 成立)
- [x] 裁定・修正(下表)
- [x] ラチェット規約の追記(CLAUDE.md の規約リストに「レビュー指摘はゲート/
      規約/明示的見送りのいずれかに着地して初めて閉じる」を追加)

### 第3ラウンドの検出と裁定

| ID | 検出(座席) | 裁定 |
|---|---|---|
| M3-1 **High** | `MatrixSweepCatalogTests`(マトリクス↔スイープ完全性ゲート)が Engine trait を持たず**どの CI トリガーでも実行されない**。4表面がゲートとして引用(Opus+Fable 収束) | **修正** — DB 不要(56ms)なので ci.yml と release.yml の verify に追加 |
| M3-2 | release.yml の verify が3ユニットスイートのうち1つしか実行しないのに CLAUDE.md は「Full verify」(Opus+Fable 収束) | **修正** — release.yml に Analyzers / TableClassGen / カタログゲートを追加(記述側でなく実体側を主張に合わせた) |
| M3-3 | `EveryBound_HasDocsProvenance` がファイル全体の部分文字列検索で、69 セル中 25 が1段の再バウンドを素通し(Opus、変異リプレイで実証) | **修正** — 構文名を含む行にバージョントークンを要求する行アンカー方式へ |
| M3-4 | 掃引ゲートの規約記述が死角を「pending 型のみ」と過小表現(実際は完全な句オブジェクト8型を含む42型)+死角に陳腐化検出なし(Opus+Fable 収束) | **修正** — `UnembeddedReturnTypes` 台帳+`ReturnTypes_AreEmbeddableOrRecorded` ゲート追加、規約文を実測に一致させた |
| M3-5 | **実欠陥**: `.Over(null句)` が `OVER ()` を静かに生成 — 掃引対象外のインスタンスメンバー面に残存していた消失句クラス(Fable) | **修正** — `OverClause.Of` 1箇所のガードで全 `.Over` を被覆+テスト |
| M3-6 | null ガード規約の免除節が配列要素を未決定のまま残し、出荷コードに相反する前例(`Values [null]` はガード済み vs `InsertInto [null]` は素の NRE)(Opus) | **修正** — 「免除は CS8604 が効く単独パラメータのみ、要素はアノテーション不可視なのでガード必須」と明文化+`InsertInto` 要素ガード追加 |
| M3-7 | `CookbookTests` が「ドリフト不可能」を主張するが実体は手書きミラー(パーサなし)、docs 監査スクリプトも cookbook を除外(Sonnet+Fable 収束) | **修正(文言)+明示的見送り** — 両表面を実機構の記述に修正。スクリプト拡張は抽出器がローカル関数形式を解釈できないため理由付きで見送り(バックログ) |
| M3-8 | `DateTimePart` を取るファクトリ11個に対し SQLA0104 の消費者リスト9個が無ゲート(Opus) | **修正** — 「消費者リスト ∪ eager ガード経路」への全数写像ゲートを追加 |
| M3-9 | CLAUDE.md の `IDbmsDialect` メンバー列挙が 6 中 2、root 名前空間の must-name リストに `TableReference` 欠落(Fable) | **修正** |
| M3-10 | ADR 0009 の設定サーフェス列挙が ADR 0019 以前のまま/ADR README の統合トリガー算術が判定不能/`dbms-differences` の frontmatter パス不足/`MatrixSweepCatalog` の stale cref(Opus/Fable) | **修正**(ADR 0019 refinement note、算術の明文化、パス追加、cref 修正) |

ユニット 1066 件・アナライザー 1083 件(いずれもゲート追加分増)・全ゲート緑。
行アンカー化した provenance ゲートは現行レジスタに対して偽陽性ゼロで通過。

第3ラウンド修正への最終レビューパス(sa-reviewer 1体)は4件を検出、全て修正:
- 行アンカー化した provenance ゲートも `Contains` のため**接頭辞変異**
  (`8.0.31`→`8.0`)は同一行で素通しできた → 数字境界付き正規表現に強化
- 私が書いた ADR 統合トリガーの算術が今日の状態で既に「閾値到達」と評価される
  自己矛盾 → 「各リスト単独で5」と確定(今日は 3 と 2 で未到達)
- 規約の「`[null]` に警告は出ない」が偽(リテラルは CS8625)→ 実際の穴
  (計算された要素はフロー未追跡)を正確に記述
- `InsertIgnoreInto` の要素ガードに双子テスト欠落 → 追加

3ラウンドを通じ、最終パスは毎回「そのラウンドで書かれたばかりの検証装置・
規約文自体」に有効な指摘を出した — 入れ子レビューの価値の一貫した実証。

## 第4ラウンド — レビュープロセス基盤の監査(未掃引メタ表面)

第3ラウンドが意図的に除外した表面を監査する: `.claude/skills/` 10本(+
docs 監査スクリプト4本)と `.claude/workflows/sa-audit-sweep.js`(計16ファイル・
約2,800行)。skills はレビュー手順そのものを定義するため、ここの欠陥は今後の
すべてのレビューに伝播する。観点は第3ラウンドの5観点を手続き面に適合:

1. 手順↔実態パリティ(パス・コマンド・参照名が現リポジトリに対して真か)
2. スクリプトのカバレッジ vs 主張(静かなスキップ・ドリフトしたリスト —
   壊した入力のコピーで「本当に落ちるか」まで検証)
3. 手順の曖昧さ(2エージェントが異なる行動を取りうる箇所)
4. skill 間の相互参照整合
5. 本ブランチの変更(新ゲート・規約改定)に対する陳腐化

- [x] パネル 3 座席(Sonnet / Opus / Fable)完了(初回はセッション利用上限で全滅、
      リセット後に再実施。3-of-3 成立)
- [x] 裁定・修正(12 系統、全て対応)

### 第4ラウンドの検出と裁定

| ID | 検出(座席) | 裁定 |
|---|---|---|
| S4-1 **High** | `sa-panel-audit` が存在しない `sa-panel-review` を3回参照(実名は `sa-panel-diff-review`)。skill の本文が「そのファイルを読め」で委譲する先が不在(3座席全員。第1ラウンドで私自身も踏んで推測で回避していた) | **修正** — 3参照+§6→§5 の帰属誤りも修正 |
| S4-4 **High** | ハーネステンプレート csproj が `RollForward` 欠落で、この環境の pinned toolchain 上で**そのままでは実行不能**。全レビュー skill の実証検証がここに依存(3座席全員。私も遭遇済み) | **修正** — `<RollForward>Major</RollForward>` 追加(`verify_sql_examples.py` と同じ既知の解) |
| S4-2 | `sa-run-integration-tests`「ci.yml はこれらを決して実行しない」が第3ラウンドの修正で偽に(Sonnet+Fable) | **修正**(+「1レーン=1クラス」の陳腐化も) |
| S4-3 | `sa-add-sql-function` が Concat 分割を「未着地」と記述 — 着地済みで `public-api-design.md` と矛盾(3座席全員) | **修正** |
| S4-5 | ハザード形状テンプレート: (a) が無ガードでクラッシュし (b)-(d) が実行されない+(c) の観測対象「漏れた WHERE」が freeze ガード(#245)以後は発生不能(Sonnet+Fable) | **修正** — try/catch ラッパー+(c) の期待を freeze スローに更新 |
| S4-6 | `bulk-pass.md` が `GenerateDocumentationFile` オフ前提で、「手順を戻せ」指示が今や出荷 XML と CS1591 ゲートを落とす有害操作(Fable+Opus) | **修正** — 恒久オンを前提に書き換え、revert 指示を削除 |
| S4-7 | docs 監査スクリプト3本が `docs/guides/oracle-array-bind.md` を無記録で除外(+coverage は `versioning.md` も)。「全ページを掃く」という主張と乖離 — 壊した入力の注入で盲点を実証(3座席全員) | **修正** — 4リストに追加、再実行緑(リンク15ファイルに) |
| S4-8 | `sa-docs-audit` skill「every doc example を実行」— 実際は参照3ページのみ(Fable+Opus) | **修正** — 実スコープ+cookbook/guides の扱いを明記 |
| S4-9 | 「SqlCondition は Internal にある」という偽のコメント(Fable+Opus) | **修正** |
| S4-10 | Listagg 例と引用ガードメッセージの不対応。Fable の「文字列がソースに無い」は補間生成(`Invalid type for {position}`)のため**部分反証** — 「例が別分岐に当たる」に狭めて採用(Fable+Opus、裁定側で再導出) | **修正** — 実メッセージ+兄弟分岐の説明に |
| S4-11 | `check_api_coverage.py` の正規表現がジェネリックファクトリ(`BindArray<T>`)を除外し `Sql` クラス宣言を誤カウント(Opus、合成ツリーで実証) | **修正** — `<T>` 対応+`Sql` 除外(カウント 210 は −1+1 で不変、内訳が正しく) |
| S4-12 | workflow コメントが実在しないファイル名 `sa-diff-review-orchestrator.md` を参照(Fable) | **修正** — 実ファイル名に |

**このラウンドの意義**: High 2件はどちらも「手順書が現実と乖離し、従う
エージェントを最初の一歩で躓かせる」クラスで、うち2件(S4-1, S4-4)は本レビュー
中に私自身が実際に踏んで無自覚に回避していたもの — 未掃引メタ表面の監査価値の
直接実証。S4-2 は第3ラウンドの修正が生んだ陳腐化で、「変更はその記述を持つ全
表面を掃く」原則の反例がまた1つ機械外で発生したことを示す。

第4ラウンド修正への最終レビューパス(sa-reviewer 1体)は「マージ可」・Low 1件:
S4-2 の修正自体が同じクラスの掃き残しをしていた — ci.yml を記述する2表面のうち
skill 側だけを直し、CLAUDE.md の CI テーブル行が3スイート列挙のまま残存 → 修正済み。
ハーネステンプレート・ハザード4形状・スクリプト3本の新規掃引対象は全て実機検証
(壊した注入が3本とも exit 1)で確認された。

## 第5ラウンド — アナライザーエンジン + agents の監査(最後の高リスク未掃引面)

対象: `src/SqlArtisan.Analyzers/**`(34ファイル・4,772行)+ `.claude/agents/`
2ファイル。選定理由: マトリクスの**データ層**は各種パリティゲートで厚く検証済み
だが、**エンジン層**(実際のコード形状で解決・報告できるか)は未監査で、偽陰性は
沈黙と見分けがつかない。1.0 で凍結される面(診断ID・メッセージ・設定キー文法・
報告挙動)のうち最後の未監査領域。

観点: ①実コード形状での偽陰性(文書化された沈黙契約の**外側**のみ — スクラッチ
プロジェクトに実アナライザーを配線して実ビルドで検証)②設定エッジでの偽陽性
③設定解決の正しさ ④凍結ハザード ⑤エンジン内の無記録な静かな範囲縮小
⑥agents 定義の手順↔実態パリティ

- [~] パネル 3 座席 — Sonnet / Fable 報告済み、Opus は利用上限で1回脱落し再投入中
- [ ] 裁定・修正

暫定裁定メモ(Opus 待ち、修正は座席読了後):
- **A5-1(Sonnet, Medium)**: `FluentChain` の文頭クライムが三項演算子を透過しない
  ため、`(flag ? Select(...) : Select(...)).Where(...)` 形で SQLA0200/0203/0300 が
  沈黙。docs の閉じた沈黙リストの外側 — スクラッチ検証器で期待1/実測0を実証済み。
  SQLA0100 は三項越しでも発火(対照実験)し、欠陥はクライム固有
- **A5-2(Fable, Medium)**: SQLA0103 のコンストラクタ判定が型名リテラル一致
  (`ConstructorIdentifierParams`)のため、生成テーブルクラス経由のエイリアス
  (`new UsersTable("70字")`)を検査しない — ライブラリの主経路で、docs 自身の
  用例形状。裁定側で `IdentifierLengthRule.cs:29-38` の辞書と
  `CorrelatedDmlRule.DerivesFromDbTableBase`(:357、流用可能な既存機構)を確認済み
- **A5-3/A5-4(Fable, Low)**: agents ファイル名↔frontmatter 名の不一致/
  `sa-reviewer.md` のルール列挙が7本中6本(`code-comments` 欠落)

## 検出事項ログ

修正した事項・見送った事項をここに記録する。

- **F-1(修正済み)**: `tests/SqlArtisan.IntegrationTests` の推移的依存
  `SSH.NET 2023.0.0` に高深刻度の既知脆弱性(NU1903 / GHSA-q939-rpr3-3284、
  修正版は 2026.0.0)。テスト専用依存で出荷物には含まれないが、ビルドログを恒常的に
  汚していた。TableClassGen csproj の SQLitePCLRaw と同じ手法(直接参照で持ち上げ)で
  `SSH.NET 2026.0.0` を明示参照し、警告ゼロを確認。実挙動は Phase 5 の統合テストで検証。
- **F-2(Phase 7 で対応)**: プレリリース表記(`--prerelease` フラグと「pre-release
  なので〜」の注記)が README.md・`docs/guides/dapper-quickstart.md`・
  `docs/guides/oracle-array-bind.md`・`src/SqlArtisan.TableClassGen/README.md` にあり、
  1.0.0 公開と同時に嘘になる。バージョン引き上げと同一コミットで除去し、
  `llms-full.txt` を再生成する(`LlmsFullTests.cs` ヘッダーの手順)。
- **F-3(要判断・軽微)**: 4 csproj の `Copyright (c) h.tacayama 2025` — 2026 年の
  リリースなので `2025-2026` 等への更新を推奨。あわせて `PackageReleaseNotes` が
  主パッケージにしかない点は任意(そのままでも害はない)。

### パネル監査(Phase 4)— 全て修正済み

- **P-1(修正済み・Sonnet/Fable)**: `Sql.Values` が要素レベル未ガード — null 行で
  素の NRE、null/空の列名要素が `(, c2)` という無効識別子を静かに生成。要素ごとの
  eager ガードを追加(`ArgumentNullException` / `ArgumentException`)。
- **P-2(修正済み・3座席全員)**: `DbColumn` が null owner(素の NRE)と null/空の
  列名(`SELECT "u". FROM ...` / 空の選択項目)を素通し。コンストラクタに
  `A column requires a name.` ガードを追加。全 `Column(string)` アクセサを一括で保護。
- **P-3(修正済み・High・Opus)**: `InsertInto(table, columns)` /
  `InsertIgnoreInto(table, columns)` に空配列を渡すと、名前付き INSERT が黙って
  位置ベース INSERT になり #397 の幅ガードも無効化。空リストは eager に
  `ArgumentException`。列なしオーバーロードが位置ベースの正規の綴りとして残る。
- **P-4(修正済み・Opus/Fable)**: `As(alias)` が null/空エイリアスで `""` を生成
  (隣の `AsTable` は投げる非対称)。`ExpressionAlias` コンストラクタでガード。
- **P-5(修正済み・Opus)**: `SqlExpression.As(string)` の XML ドキュメントが実出力に
  ない `AS` キーワードを記載 → 除去。
- **P-6(修正済み・Opus)**: `Sql.OrderBy` の XML ドキュメントが `.Asc()` / `.Desc()`
  とメソッド呼び出しで記載(実際はプロパティ)→ `.Asc` / `.Desc` に修正。

新ガードには全てメッセージ逐語一致のユニットテストを追加(+10 件、計 1055 件緑)。

### docs 敵対的検証(Phase 3)— 全て修正済み

- **D-1(修正済み・Medium DEFECT)**: `docs/expressions.md` の「全エンジンが全文検索
  インデックス必須」— PostgreSQL は index なしで実行可能(自前の統合スイープが
  index なしスキーマで実行して証明)。MySQL / Oracle / SQLite / SQL Server に
  スコープし、PostgreSQL は手引きへのリンクに変更。`docs/functions.md` の相互参照も修正。
- **D-2(修正済み・Low)**: ADR 0020 境界違反 2 件 — `Exists(Select(1)...)` の
  「equivalent」表現と、`FILTER` 句の「says the same thing」を等価主張なしの表現に修正。
- **D-3(修正済み・Low)**: `WITH TIES` の注記をエンジン文法主張と読める形から
  SqlArtisan の API サーフェスの記述にスコープ変更。
- 上記に伴い `llms-full.txt` を再生成(`LlmsFullTests` バイト一致ゲート緑)。
