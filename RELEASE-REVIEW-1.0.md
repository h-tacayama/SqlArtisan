# v1.0 リリース仕上げレビュー — 手順と進捗

v1.0.0 公開前の最終レビューの手順書。進捗はこのファイルのチェックボックスで管理する。
作業ブランチ: `claude/v1-0-release-review-01l4zw`

凡例: `[ ]` 未着手 / `[~]` 実施中 / `[x]` 完了 / `[-]` スキップ(理由を記載)

---

## Phase 1 — 品質ゲート(機械的検証)

コードを読む前に、リポジトリが「常に守るべきゲート」を全て通ることを確認する。

- [ ] `dotnet build SqlArtisan.sln`(警告ゼロ確認)
- [ ] `dotnet test tests/SqlArtisan.Tests`(ユニットテスト)
- [ ] `dotnet test tests/SqlArtisan.Analyzers.Tests`(アナライザーテスト)
- [ ] `dotnet test tests/SqlArtisan.TableClassGen.Tests`(TableClassGen テスト)
- [ ] `dotnet format SqlArtisan.sln --verify-no-changes`(スタイルゲート)

結果メモ:(未実施)

## Phase 2 — リリース成果物の点検

パッケージとして出荷されるメタデータ・ドキュメントの整合を確認する。

- [ ] `Directory.Build.props` のバージョン確認(現在 `0.8.0-beta.1`。1.0.0 への
      引き上げは Phase 7 の判断事項)
- [ ] 4 パッケージ(`SqlArtisan` / `ArrayBind` / `Dapper` / `TableClassGen`)の
      csproj メタデータ(説明・タグ・README 同梱・ライセンス)の点検
- [ ] `CHANGELOG.md` — Unreleased セクションの内容が 1.0.0 として釈明不要な形に
      整理されているか
- [ ] `docs/versioning.md` とバージョンポリシーの整合(beta → 1.0 で表明が変わる箇所)
- [ ] `dotnet pack` のドライランで 4 パッケージが警告なく生成されるか
- [ ] README — 1.0 の顔として成立しているか(インストール手順・バッジ・サンプル)

結果メモ:(未実施)

## Phase 3 — ドキュメント監査(コーパス全体)

`sa-docs-audit` スキルで README / docs/ / llms.txt / CHANGELOG を網羅監査する。
リンク・API カバレッジ・用語・出力 SQL の実証検証と、敵対的検証パスを含む。

- [ ] `sa-docs-audit` 実行
- [ ] 検出事項のトリアージと修正

結果メモ:(未実施)

## Phase 4 — 公開 API サーフェスのパネル監査

リリース後に変更しづらい「公開 API」を、3 モデル独立パネル(`sa-panel-audit`)で
現状ベース監査する。スコープは公開サーフェスに限定:

- `src/SqlArtisan/Sql/Sql.*.cs`(公開ファクトリ)
- `src/SqlArtisan/SqlBuilder/`・`src/SqlArtisan/SqlPart/`・`src/SqlArtisan/Metadata/`(公開型)

- [ ] `sa-panel-audit` 実行(スコープ: 公開 API サーフェス)
- [ ] 検出事項のトリアージと修正

結果メモ:(未実施)

## Phase 5 — 統合テスト(実エンジン検証)

出力 SQL が実際のエンジンで通ることの最終確認。`sa-run-integration-tests` スキルに従う。

- [ ] 統合テスト実行(MySQL / Oracle / PostgreSQL / SQL Server / SQLite)
- [ ] MatrixSweepTests(アナライザー方言マトリクスの実機検証)が緑であること

結果メモ:(未実施)

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

(まだなし)
