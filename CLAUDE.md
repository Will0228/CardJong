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

### enum は 0 番目に `None` を置く

C# の enum は既定値が 0 で、フィールドや配列は初期化しなくても 0 になる。
0 に意味のある値を置くと「まだ何も入っていない」状態と区別が付かず、
初期化忘れが正常な値として通ってしまう。

```csharp
// OK
public enum ClaimType : byte
{
    /// <summary>未設定。</summary>
    None = 0,

    /// <summary>何もしない。</summary>
    Pass = 1,

    /// <summary>ポン。</summary>
    Pon = 2,
}

// NG（既定値が Pass になり、未設定と「見送り」が同じ値になる）
public enum ClaimType : byte
{
    Pass = 0,
    Pon = 1,
}
```

値が外部データやシリアライズ済みのアセットに保存されている場合、番号をずらすと
既存の値の意味が変わる。その場合は勝手に振り直さず、影響範囲を確認してから直す。

`Rank` のように 0 に置けない事情があるもの（`Ace = 1` から始めて数字と一致させたい等）は
例外にしてよい。その場合は理由をコメントに書く。

### VContainer で解決されるコンストラクタには `[Inject]` を付ける

VContainer はコンストラクタが 1 つならそれを自動的に選ぶので、付けなくても動く。
それでも付けるのは、**このコンストラクタが DI の入口だと読んで分かるようにするため**。

- コンストラクタが 2 つ以上に増えた瞬間、どれが使われるかが曖昧になる（VContainer は
  `[Inject]` が無ければ引数の多いものを選ぶ）。後から増やす人が事故らないように、
  最初から明示しておく。
- 引数を消したときに「DI 経由で誰かが渡している」ことに気付ける。

```csharp
// OK
[Inject]
public ClaimResolver(IHandAnalyzer handAnalyzer)
{
    _handAnalyzer = handAnalyzer;
}
```

対象は `LifetimeScope` に登録するクラス。引数の無いコンストラクタや、
DI を経由せず `new` するだけのクラスには不要。

### データを運ぶだけの型は `record` を検討する

等値比較・`ToString`・分解・`with` による複製をコンパイラが用意してくれるので、
手で書くボイラープレートが消える。値が変わらない前提も型に表れる。

```csharp
// 値の集まりを運ぶだけならこれで足りる
public sealed record RoundResult(int Round, int DealerSeat, IReadOnlyList<int> ScoreDeltas);
```

判断の目安:

| 用途 | 選ぶもの |
|---|---|
| 値の集まりを運ぶだけ。等値比較が意味を持つ | `record`（参照型）／`readonly record struct`（小さい値型） |
| 大量に生成され、コピーコストと GC を抑えたい | `readonly struct`（`Card` のような数バイトの型） |
| 状態を持ち、振る舞いで変化する | `class` |

すでに `readonly struct` で書かれていて `IEquatable<T>` を手実装している型は、
`readonly record struct` にすると実装を減らせることがある。ただし
レイアウトや equality の挙動が変わるので、置き換えるときは使用箇所を確認する。

### `static` は本当に必要なときだけ付ける

`static` にすると差し替えができなくなり、テストで入れ替える余地も消える。
「今はインスタンスの状態を使っていない」だけの private メソッドに付いていることが多いが、
それは `static` にする理由にはならない。

付けてよいのは次のような場合。

- そのクラスの状態と本質的に無関係な純粋関数（引数だけで答えが決まり、今後もそうだと言える）
- `Assets/Scripts/InGame/Cards/CardDeckFactory.cs` のような、生成専用のユーティリティ
- 定数、および読み取り専用の共有テーブル（`private static readonly int[] ...`）

迷ったらインスタンスメソッドにしておく。後から `static` にするのは簡単だが、
外に公開された `static` を戻すのは難しい。
