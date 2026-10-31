# エンジンでの確かめ — 生の結果の抜き出し

統合テストは作業ブランチで 4 回走らせた: run 41159339490（100 件）、41159686928（106 件、`2.0`
を追加）、41160677627（114 件、short・byte を追加。ログは読んでいない）、41160892388（117 件、
レビューの指摘を確かめる E4 を追加）。下の表は 1・2 回目。4 回目の E4 は末尾の節。

Example は 1 本の流れあたり 1 回目 100 本、2 回目 106 本（`DoubleWhole` = `2.0` を足した分）。
各行は `VR <lane> <id> <模型の結果> <期待> <観測> <判定>`。判定は OK / MISMATCH /
RECORDED（アナライザー・問い・委ねたセルなので観測だけ残す）。

| 流れ | 確かめた場所 | 1 回目 (run 41159339490) | 2 回目 (run 41159686928) |
|---|---|---|---|
| Sqlite (3.50.4 / Microsoft.Data.Sqlite 9.0.5) | CI + このコンテナ | 外れ 0 | 外れ 0（ローカル: OK 85 / RECORDED 21） |
| PostgreSql (16) | CI + このコンテナの 16.15 | 外れ 0 | 外れ 0（ローカル: OK 100 / RECORDED 6） |
| PostgreSql17 | CI | 外れ 0 | 外れ 0 |
| MySql (8.0, MySqlConnector 2.4.0) | CI | 外れ 7 | 外れ 7（同じ 7 本） |
| SqlServer (2022) | CI | 外れ 1（モデル側の誤り、2 回目で修正） | 外れ 0 |
| SqlServer2025 | CI | 外れ 1（同上） | 外れ 0 |
| Oracle (XE 21.3.0) | CI | bool の読み戻しで途中停止 | 外れ 9（下記） |
| Oracle23ai (23.26.3) | CI | 途中まで | 外れ 8（下記） |

CI のログは workflow run のジョブログ（`Standard Output Messages:` 節）にある。成果物の
zip はこのセッションのネットワーク方針で取得できなかった（blob ストレージへの接続拒否）。
CI の流れの OK / RECORDED の内訳は、ログを全部書き写していないため数えていない。

## MySQL 8.0 — 外れ 7 本（両回同じ）

```
E1-ORD-BindInt            Executes  期待 1,2,3,4,5            観測 5,4,3,2,1           MISMATCH
E3-ORD-Subquery-BindInt   Executes  期待 1,2                  観測 4,5                 MISMATCH
E3-ORD-Cte-BindInt        Executes  期待 1,2                  観測 4,5                 MISMATCH
E1-GRP-BindInt            Executes  期待 ENGINE-ERROR         観測 1|10,1|20,1|30,1|40,1|50  MISMATCH
E1-ORDX-WINDOW-BindInt    Executes  期待 1|1,...,5|5          観測 ENGINE-ERROR: Window '<unnamed window>': ORDER BY or PARTITION BY uses legacy position indication which is not supported
E1-PART-BindInt           Executes  期待 1|1,...,5|5          観測 ENGINE-ERROR: (同上)
E3-ORDX-AGG-BindInt       Executes  期待 e,d,c,b,a            観測 ENGINE-ERROR: Unknown column '2' in 'order clause'
```

RECORDED のうち問いに答えが出たもの: `ORDER BY NULL` は定数（1..5）、`GROUP BY NULL`
は 1 グループ、`PARTITION BY NULL` は 1 区画、`GROUP_CONCAT(... ORDER BY NULL)` は定数。
複合の末尾 `ORDER BY`: `Bind(2)` は位置（5..1）、`2.5` / `2.0` / `NULL` は定数（1..5）、
修飾列 `` `t`.b `` は `Table 't' from one of the SELECTs cannot be used in global ORDER clause`。
`col + "b"` は 0（数値加算）。

## Oracle XE 21.3.0 — 外れ 9 本（2 回目）

```
E1-ORD-Double             期待 1,2,3,4,5  観測 5,4,3,2,1   (1 回目で観測)
E1-ORD-Decimal            期待 1,2,3,4,5  観測 5,4,3,2,1   (1 回目で観測)
E1-ORD-DoubleWhole        2 回目のログの末尾に入らず、行を読んでいない（23ai では 5,4,3,2,1）
E3-ORD-Subquery-Double    期待 1,2  観測 4,5
E3-ORD-Subquery-DoubleWhole 期待 1,2  観測 4,5
E3-ORD-Cte-Double         期待 1,2  観測 4,5
E3-ORD-Cte-DoubleWhole    期待 1,2  観測 4,5
E3-ORD-SetOperationBranch-IntNegative  期待 1..5  観測 ORA-01785（モデル側の誤り、大きな失敗）
E2-SEL-Bool               期待 1  観測 DRIVER-ERROR: InvalidCastException（Q10、大きな失敗）
```

`E2-SEL-Guid` は `DRIVER-ERROR: ArgumentException`（Q10、期待 `~` なので OK 扱い）。
複合の末尾 `ORDER BY`: `Bind(2)` / `NULL` は ORA-01785、`2.5` / `2.0` は位置（5..1）、
修飾列は ORA-00904。`ROLLUP((SELECT ...))` は ORA-22818。

## Oracle 23.26.3 — 外れ 8 本

XE 21 の 9 本から `E2-SEL-Bool` を除いたもの（bool は 1 で読み戻せた）。`E1-ORD-DoubleWhole`
は 5,4,3,2,1 を観測。`GROUP BY 2` は ORA-03162（`group_by_position_enabled` に触れる文言）。

## SQL Server 2022 / 2025 — 2 回目は外れ 0

問いに答えが出たもの（両版同じ）:
- `ORDER BY @0`（`Bind`）: `The SELECT item identified by the ORDER BY number 1 contains a
  variable as part of the expression identifying a column position.`（大きな失敗）
- `GROUP BY @0` / `ROLLUP(@0)`: `Each GROUP BY expression must contain at least one column
  that is not an outer reference.`（大きな失敗）
- `OVER (ORDER BY @0, id)` / `PARTITION BY @0` / `STRING_AGG ... WITHIN GROUP (ORDER BY @0, id)`:
  定数として実行（1..5 / e,d,c,b,a）
- `ORDER BY NULL`: `A constant expression was encountered in the ORDER BY list`、
  `OVER (ORDER BY NULL)`: `Windowed functions ... do not support constants as ORDER BY clause
  expressions.`（どちらも大きな失敗）
- 複合の末尾 `ORDER BY "t".b`: 実行（5..1）
- `ROLLUP((SELECT ...))`: `Cannot use an aggregate or a subquery in an expression used for the
  group by list of a GROUP BY clause.`

## 4 回目（run 41160892388）の E4 — レビューの指摘の確かめ

```
                  E4-PLUSDATE-TimeSpan          E4-MINUSDATE-TimeSpan         E4-SETSWAP-Column
期待              2024-01-01T12:00:00           2024-01-01T08:00:00           1|1|50
SQLite            2026                 外れ     2022                 外れ     1|1|50
PostgreSQL 16     期待どおり                    期待どおり                    1|1|50   (このコンテナ)
MySQL 8.0         20240101120000       外れ     20240101080000       外れ     1|1|1    外れ
SQL Server 2022   Operand data type datetime2 is invalid for add / subtract operator  1|1|50
Oracle XE 21.3    期待どおり                    期待どおり                    1|1|50
```

SQL Server 2025・Oracle 23ai・PostgreSQL 17 の 4 回目のログは読んでいない（PostgreSQL 17 の
ジョブは成功、SQL Server 2025・Oracle 23ai は失敗）。4 回目の Oracle XE 21 は、ほかに F2 の
派生表・CTE の 4 件を外れとして読んだ。複合の `-1` は、モデルを直した後なので OK（ORA-01785 を期待）。
