# 誤りを入れた測定（Stryker.NET 5.0.0）

対象 9 ファイル（`SelectBuilder.cs` はファイル全体）。`coverage-analysis: perTest`、並列 4、
このコンテナ（4 コア）。測定した誤り 496 個（コンパイルできない 56 個、対象外の 153 個を除く）。
スコアは (捕まえた + 時間切れ) / 496。

| 回 | 走らせたテスト | 捕まえた | 時間切れ | 生き残り | 通らない | スコア | 時間 |
|---|---|---|---|---|---|---|---|
| 0（参考） | 単体のみ、`SelectBuilder.cs` の行範囲指定が効かず 335 個 | 317 | 0 | 13 | 5 | 94.63% | 2 分 50 秒 |
| 1 | 単体のみ（1,990 件） | 466 | 1 | 17 | 12 | 94.15% | 3 分 8 秒 |
| 2 | 単体 + 試しの DB なし層（1,992 件） | 466 | 1 | 17 | 12 | 94.15% | 6 分 30 秒 |
| 3 | 2 + short / byte の値を足した後 | 472 | 1 | 11 | 12 | 95.36% | 7 分 16 秒 |

- 回 2: 試しの DB なし層は 475 個の誤りを通ったが、単体テストが捕まえなかったものを新たに
  捕まえたものは **0**。
- 全回とも、初回のテスト実行で単体テストが 2 件失敗していた（`LineLengthSweepTests` と
  `CommentCapRatchetTests`。試しのファイルが行長・コメント長のゲートに触れていた。対象のコードを
  試すテストではないので測定値には効かない）。あとで試しのファイルを直し、2 件とも通るようにした。
- エンジンの Example は走らせていない（DB が要る）。F1〜F4 はコードの書き換えでは表れない種類の
  誤り（エンジンの読み方）で、この測定には映らない。

## 生き残り・通らない 29 個（回 1・2）の分類

### 答えを変えない誤り（11）

| id | 場所 | 誤り | 理由 |
|---|---|---|---|
| 611 | `SelectBuilder.cs:410` | `orderBy < 0` → `<= 0` | ORDER BY が部品の先頭（0）に来ることはない。同値 |
| 2171 | `GroupByItemResolver.cs:82` | `IndexOfAny(...) < 0` → `<= 0` | 数値の文字列が `.` で始まることはない。同値 |
| 2537 | `OrderByItemResolver.cs:89` | 同上 | 同上 |
| 2174 | `GroupByItemResolver.cs:90` | 位置名 `"GroupByItem"` → `""` | `Complex` を渡したときの例外の文言だけ。SQL は変わらない |
| 2543 | `OrderByItemResolver.cs:99` | `"OrderByItem"` → `""` | 同上 |
| 2261 | `InsertValueResolver.cs:28` | `"InsertValue"` → `""` | 例外の文言だけ |
| 3054 | `ExpressionResolver.cs:311` | `"DISTINCT ON"` → `""` | 例外の文言だけ |
| 4077 | `PartitionByClause.cs:15` | 空の PARTITION BY の文言 → `""` | 例外の文言だけ |
| 2873 | `DistinctOnItemResolver.cs:19` | null 配列の名前つき例外を消す | 次の行で NRE。大きく失敗することは変わらない |
| 3070 | `ExpressionResolver.cs:335` | 同上（可変長引数の null 配列） | 同上 |
| 2963 | `ExpressionResolver.cs:20` | 同上（ペア配列の null） | 同上 |

文言だけの誤りは、例外の文言を契約とする単体テストの規則（`unit-tests.md`）から見れば
「足すべきテスト」だが、この型（SQL の意味）からは外れる。

### Example を足すべきもの（6）→ 足した（回 3 で 6 個とも捕まえた）

| id | 場所 | 誤り |
|---|---|---|
| 2151, 2152 | `GroupByItemResolver.cs:64` | `sbyte or byte or short or ushort ...` の `or` → `and`（short / byte が整数として扱われなくなる） |
| 2519, 2520 | `OrderByItemResolver.cs:68` | 同上 |
| 2887, 2888 | `DistinctOnItemResolver.cs:26` | 同上 |

値の種類に short と byte を足した（`ValueKinds.cs`）。依頼の値の一覧（0・正・負・long）には
無かった区分で、軸の不足を測定が見つけた例。

### 記録して残すもの（12）

| id | 場所 | 誤り | 残す理由 |
|---|---|---|---|
| 588, 590, 592, 594, 596 | `SelectBuilder.cs:325-359` | 前置き（DISTINCT / DISTINCT ON / ヒント）付き `Select(...)` の項目を落とす | どのテストも通らない。集合演算の続き（`.UnionAll.Select(Distinct, ...)`）からしか届かない。プローブの `ISelectBuilder` の段は WITH のビルダーに当たり、ここには来ない。値の解決は `Select` と同じ。項目を落とすと Build が拒むと見ているが確かめていない |
| 619, 624, 625 | `SelectBuilder.cs:419-421` | 入れ子の複合の ORDER BY の後ろの部品（LIMIT など）を落とす | 型の外（入れ子の複合の描画）。黙って行が変わりうるので、別の型として残す |
| 618, 627 | `SelectBuilder.cs:418, 427` | 相関 DML ガードの状態の更新を落とす / 常に複合とみなす | 型の外（相関 DML ガード） |
| 2168 | `GroupByItemResolver.cs:75` | float の NaN を拒まない | float の区分が無い。拒まなくても `GROUP BY NaN.0` はエンジンが拒む（大きな失敗） |
| 3115 | `ExpressionResolver.cs:377` | `BindArray<bool>` を拒む | `BindArray` は表の外へ委ねた区分 |

回 3 の生き残り・通らない 23 個 = 答えを変えない 11 + 記録して残す 12。
