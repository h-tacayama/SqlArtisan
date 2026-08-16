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

- [~] `sa-docs-audit` 実行 — 同梱4スクリプトは全て緑:リンク解決(14ファイル)、
      API 網羅は欠落・幽霊なし(公開ファクトリ210)、用語・空白違反なし、
      ドキュメント記載の SQL 例 104/104 が実出力と一致。README のランディング主張
      (ベンチマークのスコープ付き表現・計測環境の明記)も確認済み。
      敵対的検証パス(独立 sa-reviewer)を実行中
- [ ] 検出事項のトリアージと修正

結果メモ: スクリプト検証は全て通過。敵対的検証の結果待ち。

## Phase 4 — 公開 API サーフェスのパネル監査

リリース後に変更しづらい「公開 API」を、3 モデル独立パネル(`sa-panel-audit`)で
現状ベース監査する。スコープは公開サーフェスに限定:

- `src/SqlArtisan/Sql/Sql.*.cs`(公開ファクトリ)
- `src/SqlArtisan/SqlBuilder/`・`src/SqlArtisan/SqlPart/`・`src/SqlArtisan/Metadata/`(公開型)

- [~] `sa-panel-audit` 実行中 — スコープを 55 ファイル(約 6,200 行)に解決し、
      Release ビルドゲート通過を確認のうえ、同一ブリーフィングで 3 座席
      (Sonnet / Opus / Fable)を独立起動済み
- [ ] 検出事項の裁定(主要ソースに対する再導出)と修正

結果メモ: Sonnet 座席は報告済み(Medium 2 件)。両検出とも裁定側ハーネスで再現を
確認済み(CONFIRMED)— 修正は全座席の報告が揃ってから適用する:
- P-1: `Sql.Values` の rows に null 行 → 素の `NullReferenceException`(#403 の
  規約では `ArgumentNullException` を投げるべき)。`Sql.V.cs:26-28`
- P-2: `DbColumn` が空・null の列名を素通しし、`SELECT "a". FROM t "a"` /
  `SELECT  FROM t` という無効 SQL を静かに生成(#405 のテーブル名ガードの列版が
  欠落)。`DbColumn.cs:12` と各 `Column(string)` ファクトリ経由で到達可能
Opus / Fable 座席の報告待ち。

## Phase 5 — 統合テスト(実エンジン検証)

出力 SQL が実際のエンジンで通ることの最終確認。`sa-run-integration-tests` スキルに従う。

- [~] 統合テスト実行(MySQL / Oracle / PostgreSQL / SQL Server / SQLite)
  - SQLite レーンはローカル実行で 71/71 成功(この環境に Docker がないため、
    コンテナ5レーンは CI で検証)
  - `integration.yml` をこのブランチに対して workflow_dispatch 済み(SSH.NET
    2026.0.0 更新の実挙動確認を兼ねる)。結果待ち
- [ ] MatrixSweepTests(アナライザー方言マトリクスの実機検証)が緑であること
      (上記 CI 実行に含まれる)

結果メモ: CI マトリクス実行中。

## Phase 6 — ベンチマーク確認(任意)

README の比較数値が現状のコードと乖離していないかの確認。相対比較と B/op のみ有効。

- [ ] `sa-run-benchmark` で相対比較・アロケーション数値を確認し、README 記載値と照合

結果メモ:(未実施)

## Phase 7 — 最終判定とリリース手順

- [ ] Phase 1–6 の検出事項が全て修正済みまたは「1.0 でやらない」と明示的に判断済み
- [ ] バージョン引き上げ(`0.8.0-beta.1` → `1.0.0`)— **ユーザー判断事項**
- [ ] CHANGELOG の 1.0.0 セクション確定(日付入り)
- [ ] リリース手順の確認: タグ `v1.0.0` push → `release.yml`(full verify →
      統合テスト → 4 パッケージ pack & push)
- [ ] タグ push — **ユーザー実施事項**(このレビューでは行わない)

結果メモ:(未実施)

---

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
