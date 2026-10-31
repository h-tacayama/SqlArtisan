# 検証 3: 性質の違う型「名前の解決」 — 報告

価値の読み取りの試し（`trials/value-reading/REPORT.md`）と同じ手順を、別の型に当てた記録。
型の定義: **C# が名指した列・表を、どこかのエンジンが別の列・別のスコープに結びつける**
（静かに別の値を読む、または大きく失敗する）。issue も PR も作らず、不具合は直していない。

## 0. 結果（先に）

- **記録と食い違う不具合: 0 件。** 記録が「こう振る舞う」と決めているセルでは、エンジンの
  外れはすべて模型の誤りだった（§ 4.2、3 件）。
- **静かな誤りが出るのに、どの記録も決めていないもの: 7 種類**（§ 6 の問い）。重い順に並べる。
  1. 内側のブロックの関係が外側と同じ別名を持つと、外側を指す修飾列が内側に結びつく（N3）。
     8 レーンすべてで静かに起きる。`DELETE` では消す行が変わる。SQLite と SQL Server では、
     大文字と小文字だけが違う別名（`"x"` と `"X"`）でも起きる。
  2. 基表と同じ名前の CTE を置くと、文の中のその表への参照が全部 CTE を読む（N12）。MySQL・
     PostgreSQL・SQLite・SQL Server で静かに起きる。Oracle では起きない。CTE 名は小文字の
     引用付きで描かれ、表名は大文字に畳まれるので、名前が一致しないため。
  3. MySQL の upsert の副問い合わせで表に `new` という別名を付けると、`Excluded(...)` がその
     表を読む（N13）。
  4. 複合問い合わせの最上位の枝で、別の枝にしか載っていない表の裸の列が、その枝の同名の列に
     結びつく（N10）。8 レーンすべて。
  5. `Using(col)` の列の持ち主が、結合する 2 つの表のどちらでもなくても通る（N6）。MySQL・
     PostgreSQL・SQLite で静かに起きる。
  6. `DistinctOn(g)` の裸の列が、同名の選択別名に結びつく（N1 のうち DISTINCT ON）。
     PostgreSQL。
  7. MySQL の `VALUES` の中の列参照が、同じ行の先に書いた列を読む（N14）。
- **大きな失敗で、書く手段もないもの: 1 件（N11）。** MySQL の `ON DUPLICATE KEY UPDATE` で
  対象行の現在の値を読む式（`b = b + 1`）が、ライブラリが常に付ける `AS new` と曖昧になり、
  拒まれる。MySQL では INSERT の対象に別名を付けられない（ADR 0011）ので、ほかの書き方もない。
- 独立レビューの指摘 2 件のうち、1 件目（N3）は模型にあり Example もあった。2 件目（N12）は
  模型の外だった。どちらも型の内側にある。§ 7。
- 後追い: 11a6eca9（修正前）に戻して走らせると、既知の 6 件のうち 4 件を MISMATCH として捕まえた。
  ただし模型はそれらの修正を読んだ後で作ったので、目隠しの検証ではない。§ 8。
- 手間: 約 1 時間 40 分（04:54〜06:35 頃）。CI 3 回、Stryker 2 回、レビュー 1 体。§ 9。

## 1. 模型

### 1.1 軸と数え方

| 軸 | 値 | 数え方 |
|---|---|---|
| 位置 | 33 | 公開ビルダー段のメソッド 141 個（`ClauseInventory`、`SqlArtisan` 名前空間で名前に `Builder` を含む interface と、`IPagination` などの能力 interface の宣言メソッド。`Build` は除く）を `PositionsOf` で位置に写した。ここで 30 種。残り 3 種（`WindowPartition`・`WindowOrder`・`AggregateOrder`）は列を取る `Sql` の工場（`PartitionBy`・`OrderBy`）から数えた。どの位置にも写らないメソッド（FROM・結合・CTE・ページング・分岐キーワード）は、名前を持ち込むか、何も描かない。`Positions_AreReachedByAStageOrAFactory` が、どの位置もどこかから届くことを確かめる |
| 参照の種類 | 5 | 修飾列（持ち主に別名）、裸の列（持ち主に別名なし）、選択別名（`ExpressionAlias`）、疑似行（`Excluded`/`Inserted`/`Deleted`）、表名（`Of(table)`） |
| 持ち主 | 4 | 自分のブロック／外側のブロック（相関）／どこにも載っていない／別表（対象の位置・疑似行のみ） |
| 衝突 | 9 | なし、同じブロックの別の関係に同名列、内側のブロックに同名列、同名の選択別名、同じ別名、大文字小文字だけ違う別名、同名の表（SQLite RETURNING）、USING でまとめた列、同名の CTE |
| ブロック | 7 | 最上位、述語の副問い合わせ、スカラー副問い合わせ、派生表、CTE 本体、LATERAL、複合の枝 |
| レーン | 8 | 試し 1 と同じ |

書けない組み合わせ（`Applies`）を除いて **3,053 キー × 8 レーン = 24,424 セル**
（`model-cells.tsv.gz`）。

| 結果 | セル |
|---|---|
| 結びつく（Binds） | 6,117 |
| Build() が拒む | 4,960 |
| エンジンが拒む | 5,039 |
| 記録が「書いたまま出す」と決めた（Permissive） | 280 |
| コンパイルが止める | 1,184 |
| 解析器（SQLA0102 など） | 728 |
| 行列に委ねる | 240 |
| 人への問い | 5,876 |

問いのセルは 24%。試し 1 は 10,080 セル中 202（2%）だった。名前の解決は、記録が決めていない組み合わせ（衝突 × 位置）が広い。

### 1.2 期待の根拠

期待は記録に合わせた（§ 14 の決定）。根拠として使った記録は次のとおり。

- guards-and-empty-states.md のうち、次の段落。
  - 「Every unaliased table a nested block reads…」（#677）
  - 「Each nested block defers the correlated-DML guard」（#253）
  - 「A bare SET target is a column…」（#674、#675、#708）
  - 「A sort key emitted as a bare name」（#672）
  - 「A same-named relation under SQLite's RETURNING」（#595）
  - 「An OUTPUT item reading the target bare」（#669）
  - 「Joined-target alias requirement」（#258）
- `docs/query-statements.md` の ON CONFLICT 節（#721）
- DialectMatrix

記録のない「曖昧な列は拒まれる」「範囲外の修飾子は拒まれる」の 2 つは、SQL 標準を根拠として
書いた。各エンジンの文書とは突き合わせていないので、エンジンの実行で確かめた（全レーンで
一致）。

## 2. Example

- 82 本（`NameResolutionExamples.cs`）。1 本が複数のキーを持つことがあり、キーは 94 個。
- 区別のためのデータ: `nr_t` と `nr_s` は `id`・`b`・`c` を共有し、`b` と `c` は 2 つの表で
  並べ替えてある。だから別の表に結びつくと別の行が返る。
- 意図した結果（Intended）は、C# が名指した列を読んだときの行。「もう一方」（Other）は、衝突の
  相手に結びついたときの行。どちらも手で計算した。
- **網羅**:
  - どの軸も全値を最低 1 回は通った（位置 33/33、参照 5/5、持ち主 4/4、衝突 9/9、
    ブロック 7/7）。
  - 位置 × 参照の組は 131 のうち 50。残り 81 のうち 25 は、コンパイルが止める組だけでできて
    いて、エンジンでは確かめられない。**残る 56 組は Example がない。手順の「対ごとに最低
    1 回」は満たしていない。**
  - 試し 1 では、生成器が反射で Example を作った。今回の型では、組ごとに文の形を手で組む必要が
    あり、その手間で止めた。

## 3. エンジン

CI を 3 回（run 41171688902・41172682600・§ 10 の 3 回目）、ローカルで SQLite 3.50.4 と
PostgreSQL 16.15 を走らせた。2 回目の判定の内訳（8 レーン × 83 行）は次のとおり。

- OK 330
- RECORDED 190（問い・Permissive・委ねたセル。観測だけを残す）
- DELEGATED 142（そのエンジンにない構文）
- MISMATCH 2

全表は `run2-table.md`。

### 3.1 Permissive の確かめ

記録が「書いたまま出す」と決めた 3 つの形は、8 レーンすべてで「もう一方」に結びついた。

- 最上位でどこにも載っていない表の裸の列
- #672 の ORDER BY の衝突（記録は PostgreSQL と SQLite のみ実測）
- 選んでいない別名での ORDER BY

記録どおりで、静かな誤りが記録の判断として残っている。

## 4. 模型の誤り（エンジンが見つけた）

### 4.1 一覧

| 回 | Example | 誤り | 直し |
|---|---|---|---|
| 1 | UPS-B-VALUE（MySQL） | #721 の記録は ON CONFLICT だけを決めているのに、MySQL の ODKU にも広げて Binds とした | MySQL は問い N11 にした。観測は大きな失敗 |
| 1 | FOU-Q-OWN（Oracle 2 レーン） | `Of(table)` は Oracle では SQLA0100（行列）なのに Binds とした | 参照の種類に「表名」を足した。Oracle の表名は委ねるセルにし、Oracle の `Of(column)` の Example を足した |
| 2 | MRG-BRANCH-Q（Oracle 2 レーン） | `WhenMatched(cond)` は Oracle では SQLA0100 なのに、Example を Oracle でも走らせた | Example の対象を PostgreSQL と SQL Server にした |

行列の見落としが 2 件あった。模型を作るときに DialectMatrix を引く段を入れていなかったため。

### 4.2 記録との食い違い

なし。

## 5. Stryker

（§ 10 で埋める）

## 6. 人への問い

| ID | 問い | エンジンの答え |
|---|---|---|
| N1 | 裸の列と同名の選択別名（GROUP BY・HAVING・WHERE・DISTINCT ON） | GROUP BY・HAVING・WHERE は 8 レーンとも列（意図どおり）。DISTINCT ON は PostgreSQL で別名（静か） |
| N2 | 同じ衝突の OVER・WITHIN GROUP・集約の ORDER BY（MySQL・Oracle・SQL Server） | 6 レーンとも列（意図どおり） |
| N3 | 内側の関係が外側と同じ別名を持つ | SELECT・UPDATE・DELETE で 8 レーンとも内側（静か）。大文字小文字だけ違う別名では SQLite と SQL Server 2 版が内側、MySQL・Oracle・PostgreSQL は意図どおり |
| N4 | 同じブロックに同じ別名が 2 つ | 8 レーンとも大きな失敗 |
| N5 | USING でまとめた列の修飾参照 | Oracle 2 版は ORA-25154。ほかは意図どおり |
| N6 | `Using(col)` の持ち主が結合の外 | MySQL・PostgreSQL・SQLite は静かに通る。Oracle は ORA-25154（同じ文の修飾参照による） |
| N7 | `GroupBy(alias)` | Build() が拒む（`Invalid type for GroupByItem`） |
| N8 | 複合の末尾 ORDER BY で、後の枝だけにある別名 | SQLite は意図どおり。ほかは大きな失敗 |
| N9 | FOR UPDATE OF に載っていない表 | 対応する 5 レーンで大きな失敗 |
| N10 | 最上位の複合の枝で、別の枝の表の裸の列 | 8 レーンとも、その枝の同名列（静か） |
| N11 | MySQL ODKU の値で対象の裸の列 | 大きな失敗（`Column 'b' in field list is ambiguous`）。ほかの書き方なし |
| N12 | 基表と同じ名前の CTE | MySQL・PostgreSQL 2 版・SQLite・SQL Server 2 版は CTE（静か）。Oracle 2 版は意図どおり |
| N13 | 内側の表の別名が `new`（MySQL） | `Excluded` が内側の表を読む（静か） |
| N14 | INSERT の VALUES の中の列参照 | MySQL は同じ行の列（静か）。ほかは大きな失敗 |
| Q13 | 複合の末尾 ORDER BY の修飾列（試し 1 から） | MySQL・Oracle は大きな失敗。SQL Server は意図どおり |

## 7. 独立レビュー（表を見せずに 1 体）

指摘は 2 件で、型の内側を外れたもの（b）の扱いと誤り（c）は次のとおり。

| 指摘 | 分類 | 対応 |
|---|---|---|
| High 1: 内側の同じ別名が外側の修飾参照を捕まえる（SELECT・DELETE・UPDATE・MERGE・派生表・LATERAL・大文字小文字違い・MySQL `new`） | SELECT の形は模型にあり（N3）、Example もあった。DML・派生表・LATERAL は（a）模型にあるが Example がなかった。大文字小文字違いと MySQL `new` は（b）模型の外 | （a）UPDATE・DELETE の Example を足した。（b）衝突に「大文字小文字だけ違う別名」を足し、疑似行 × 同じ別名のセルを足した（N13）。やり直しの範囲は修飾列の全位置の衝突列。Example を 2 本足した |
| High 2: 基表と同じ名前の CTE が参照を捕まえる | （b）模型の外 | 衝突に「同名の CTE」を足した。やり直しの範囲は、問い合わせ・対象・DML の WHERE の位置 × 最上位・述語の副問い合わせ。Example を 2 本足した |
| 「確かめて外した」一覧（GROUP BY などの別名衝突、DO UPDATE の WHERE ほか） | エンジンの観測と一致 | なし |
| （c）誤り | なし | — |

レビューの費用: 133,864 トークン、ツール 32 回、7 分 40 秒。

## 8. 後追い（11a6eca9）

修正前の 11a6eca9 に今の模型と Example を入れて、SQLite と PostgreSQL 16 で走らせた
（`backtest/`）。

| PR | 捕まえたか | Example |
|---|---|---|
| #688 入れ子の未掲載の裸の表 | 捕まえた | PRED・SCALAR・DERIVED・CTE・LATERAL-B-OUTER、UPS-B-OUTER |
| #689 SET の対象・列リストの別表 | 捕まえた | UPD-B-OTHER、UPD-Q-OTHER-SWAP、INS・INSSET・UPS-B-TARGET・CONFLICT・MRG の OTHER |
| #708 疑似行の別表 | 捕まえた | UPS-P-OTHER |
| #721 upsert の対象の修飾 | 捕まえた（PostgreSQL） | UPS-B-VALUE、UPS-WHERE-B |
| #745 過剰な拒否の取り消し | 捕まえていない | 対象を自分で載せた副問い合わせ・結合で載せ直した対象・Oracle 23 の DELETE ... FROM の Example がない |
| #753 SQLite RETURNING の書き方での修飾 | 捕まえていない | RET の 2 本は修正前も同じ結果 |

ほかに、WORD-A（OVER の中の別名）が修正前は大きな失敗、今は Build() が拒む。6 件の外の修正
による差。

**注意**: 模型を作る前に #674・#677・#721・#708 を読んでいた。だから 4/6 は「この手順なら
見つけられた」ではなく、「知っていることを模型に書けた」の確かめにとどまる。

## 9. 手間と時間

| 段 | 時刻 | 中身 |
|---|---|---|
| 準備（記録と PR を読む） | 04:54〜05:00 | |
| 模型・Example・runner | 05:00〜05:12 | 3 ファイル、初回のコンパイルで通った |
| CI 1 回目 | 05:13〜05:15 | MISMATCH 3（模型の誤り 2 種） |
| 模型の直し・位置の穴埋め・レビュー反映 | 05:15〜05:27 | |
| CI 2 回目 | 05:28〜05:30 | MISMATCH 2（Example の誤り 1 種） |
| Stryker 1・2 回目 | 05:27〜 | § 5 |
| 後追い | 05:29〜05:33 | |

試し 1（価値の読み取り）との比較は § 10。

## 10. 残り

（Stryker と CI 3 回目の後に埋める）
