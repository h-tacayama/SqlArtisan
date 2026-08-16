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

- [~] パネル 3 座席(Sonnet / Opus / Fable)+ docs 敵対的検証 1 体を
      第1ラウンドと同一プロンプトで再起動済み。報告待ち
- [ ] 裁定・分類・改善策の導出

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
