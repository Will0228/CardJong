# CardJong

トランプに麻雀の要素を取り入れた対戦ゲーム。ルールの仕様は [README.md](README.md) を参照。

## コーディング規約

### メソッド内のローカル変数は `var` で宣言する

同じ型名を左右に二度書かないため。宣言の並びが揃って、変数名と代入内容に目が行く。

対象は**メソッド内のローカル変数だけ**。フィールド・プロパティ・引数・戻り値は
API の一部なので、従来どおり明示的に型を書く。

```csharp
// OK
var player = _model.GetPlayer(seat);
var hand = new List<Card>(player.ConcealedCards);
var deltas = new int[model.PlayerCount];

for (var i = 0; i < cards.Count; i++)
foreach (var card in player.ConcealedCards)
using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
if (_handAnalyzer.TryDecompose(hand, melds, out var decomposition))

// NG
PlayerModel player = _model.GetPlayer(seat);
List<Card> hand = new List<Card>(player.ConcealedCards);
for (int i = 0; i < cards.Count; i++)
```

### `var` が書けない形になったら、書ける形に直す

`var` が使えないのは言語仕様上どうにもならない次の場合だが、いずれも
書き方を変えれば避けられることが多い。まず避ける方を検討する。

| 場面 | 回避の仕方 |
|---|---|
| 初期化子を伴わない宣言（`int x;` と書いて後の分岐で代入する） | 値を決める処理を補助メソッドや三項演算子に切り出し、`var x = ...` の形にする |
| 推論される型と必要な型が違う（`TKey?` が欲しいのに `TKey` になる等） | 変数を分ける、あるいは戻り値の型で表現してメソッドに切り出す |
| ダウンキャストを兼ねる `foreach`（`Enum.GetValues` など非ジェネリックな `Array` を回す場合） | 型付きの配列を用意して回す |

切り出しでかえってネストが深くなったり、意味の無いメソッドが増えるようなら、
明示型のまま残してよい。その場合は理由をコメントに書く。

現状、`Assets/Scripts` 配下のローカル変数はすべて `var` で書かれている。
