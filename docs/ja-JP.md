# 分散型 Bradley–Terry レーティング

1試合の結果と、対戦する2人の現在のレーティングさえあれば更新できます。中央サーバーも、全試合の記録も、プレイヤーIDも不要です。リポジトリには TypeScript と C# の2つの実装があり、意味はまったく同じで、どちらも純粋関数で構成されています。

**言語:** [English](../README.md) · [简体中文](zh-CN.md) · [繁體中文](zh-TW.md) · [Deutsch](de-DE.md) · [Español](es-ES.md) · [Français](fr-FR.md) · [Italiano](it-IT.md) · **日本語** · [한국어](ko-KR.md) · [Русский](ru-RU.md) · [Tiếng Việt](vi-VN.md)

## パッケージ

| 言語 | パッケージ | ドキュメント |
| --- | --- | --- |
| TypeScript | [`decentralized-bradley-terry-rating`](https://www.npmjs.com/package/decentralized-bradley-terry-rating) | [typescript/README.md](../typescript/README.md) |
| C# (.NET 8) | [`DecentralizedBradleyTerry`](https://www.nuget.org/packages/DecentralizedBradleyTerry) | [csharp/README.md](../csharp/README.md) |

## モデル

レーティング `r ∈ (0, 1)` は、そのプレイヤーの相対勝率です。つまり、レーティング `r = 0.5` のニュートラルな相手に勝つ確率です。これは Bradley–Terry の強さ

```
γ = r / (1 - r)
```

に対応します。2人の対戦予測は次のとおりです。

```
P(A が B に勝つ) = γA / (γA + γB)
                = rA(1 - rB) / (rA(1 - rB) + rB(1 - rA))
```

`θ = logit(r) = log(γ)`、`p = P(A が B に勝つ)`、`s = 1`（A の勝ち）または `0`（A の負け）、`η` を学習率とすると、

```
θA' = θA + η · (s - p)
θB' = θB - η · (s - p)
r'  = σ(θ')
```

これは大域的な Bradley–Terry 対数尤度に対するオンライン勾配上昇を1ステップ行うもので、対数オッズ形式の Elo に相当します。デフォルトの `η = 0.25` は、チェスの `K ≈ 43` にほぼ相当します（`η = K · ln 10 / 400`）。

学習率を固定すると、オンライン更新は大域解の `O(η)` 近傍に落ち着きます。最大尤度解に厳密に収束させたい場合は、対戦数 `t` に応じて学習率を減衰させます。たとえば `η_t = η0 / (t + τ)` です。ライブラリ自体は状態を持たないため、減衰スケジュールは呼び出し側が渡します。

## 性質

1. **範囲が保証される** — 入力も出力も常に `(0, 1)` の範囲内に収まります。勝率が負になったり、100% を超えたりすることはありません。
2. **0.5 が中立** — `r = 0.5` はニュートラルな相手との互角を意味し、新しいプレイヤーもここから始まります。
3. **対称** — 2人を入れ替えて結果を反転すると、新しいレーティングも入れ替わります。`P(A が B に勝つ) + P(B が A に勝つ) = 1`。
4. **単調** — 勝者のレーティングは上がり、敗者のレーティングは下がります。
5. **番狂わせほど大きく動く** — 予想外の結果は予想どおりの結果より大きくレーティングを動かします。強い相手に勝つほうが、弱い相手に勝つより多くの収穫があります。
6. **対数オッズのゼロサム** — `θA' + θB' = θA + θB`。1試合で2人の対数オッズの和は変わらないため、レーティング全体が漂流することはありません。全員が 0.5 から始めれば `θ` の総和は常に 0 になり、これは大域的な Bradley–Terry 最大尤度解の正規化とちょうど一致します。
7. **収束する** — 各更新は大域的な Bradley–Terry 対数尤度の勾配方向に進みます。Robbins–Monro 条件を満たすように学習率を減衰させれば、レーティングは大域 MLE に収束します。テストでは、MM アルゴリズムで求めた参照 MLE と照合してこれを確認しています。

## 引き分け

試合の結果は「A の勝ち」か「B の勝ち」の2つだけです。引き分けが必要な領域では、そのままこのライブラリに渡さないでください。

## クイックスタート

TypeScript:

```ts
import {
  INITIAL_RATING,
  updateRatings,
  winProbability,
} from 'decentralized-bradley-terry-rating';

const ratings = new Map<string, number>([
  ['alice', INITIAL_RATING],
  ['bob', INITIAL_RATING],
]);

const a = ratings.get('alice')!;
const b = ratings.get('bob')!;

// alice の勝ち
const { ratingA, ratingB } = updateRatings(a, b, true);
ratings.set('alice', ratingA);
ratings.set('bob', ratingB);

console.log(winProbability(ratingA, ratingB)); // 更新後の予測勝率
```

C#:

```csharp
using DecentralizedBradleyTerry;

var ratings = new Dictionary<string, double>
{
    ["alice"] = Ratings.InitialRating,
    ["bob"] = Ratings.InitialRating,
};

// alice の勝ち
var (newAlice, newBob) = Ratings.Update(ratings["alice"], ratings["bob"], aWon: true);
ratings["alice"] = newAlice;
ratings["bob"] = newBob;

double p = Ratings.WinProbability(newAlice, newBob);
```

各言語の詳しい API は、それぞれのパッケージの README を参照してください。

## リポジトリ構成

```
.
├── typescript/   # npm パッケージ
├── csharp/       # NuGet パッケージ
└── docs/         # この README の各言語版
```

## テストの実行

TypeScript（Node 23.6 以上。ネイティブの型除去で `.ts` を直接実行するため、ビルドは不要です）:

```bash
cd typescript
npm test
```

任意で:

```bash
npm install        # TypeScript と @types/node をインストール
npm run typecheck  # 型チェック
npm run build      # dist/ を出力
```

C#（.NET 8 SDK）:

```bash
dotnet test csharp/test/DecentralizedBradleyTerry.Tests/DecentralizedBradleyTerry.Tests.csproj
```

どちらのテストスイートも、予測式、対称性、単調性、対数オッズの保存、学習率の影響、不正な入力、そしてオンライン更新が大域的な Bradley–Terry MLE に収束することをエンドツーエンドで検証します。

## ライセンス

MIT
