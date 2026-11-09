# 去中心化 Bradley–Terry 評分

每場對局結束後，只要兩名玩家目前的相對勝率和比賽結果，就能算出新評分。不需要中央伺服器、全域對戰紀錄，也不需要玩家編號。本專案提供 TypeScript 與 C# 兩套語意一致的實作，核心皆為純函式。

**語言：** [English](../README.md) · [简体中文](zh-CN.md) · **繁體中文** · [Deutsch](de-DE.md) · [Español](es-ES.md) · [Français](fr-FR.md) · [Italiano](it-IT.md) · [日本語](ja-JP.md) · [한국어](ko-KR.md) · [Русский](ru-RU.md) · [Tiếng Việt](vi-VN.md)

## 套件

| 語言 | 套件 | 文件 |
| --- | --- | --- |
| TypeScript | [`decentralized-bradley-terry-rating`](https://www.npmjs.com/package/decentralized-bradley-terry-rating) | [typescript/README.md](../typescript/README.md) |
| C# (.NET 8) | [`DecentralizedBradleyTerry`](https://www.nuget.org/packages/DecentralizedBradleyTerry) | [csharp/README.md](../csharp/README.md) |

## 模型

評分 `r ∈ (0, 1)` 是玩家的相對勝率：面對評分為 `r = 0.5` 的中性對手時的獲勝機率。它對應 Bradley–Terry 強度

```
γ = r / (1 - r)
```

兩名玩家的對局預測為

```
P(A 勝 B) = γA / (γA + γB)
          = rA(1 - rB) / (rA(1 - rB) + rB(1 - rA))
```

設 `θ = logit(r) = log(γ)`，`p = P(A 勝 B)`，`s = 1` 表示 A 獲勝、`0` 表示 A 落敗，`η` 為學習率：

```
θA' = θA + η · (s - p)
θB' = θB - η · (s - p)
r'  = σ(θ')
```

這就是對全域 Bradley–Terry 對數似然做的一步線上梯度上升，也就是對數勝算（log-odds）形式的 Elo。預設 `η = 0.25`，約相當於西洋棋的 `K ≈ 43`（`η = K · ln 10 / 400`）。

學習率固定時，線上更新會穩定在全域解附近 `O(η)` 的範圍內。若要嚴格收斂到最大似然解，可讓學習率隨已進行的對局數 `t` 衰減，例如 `η_t = η0 / (t + τ)`。程式庫本身不保存狀態，衰減策略由呼叫端自行傳入。

## 特性

1. **值域有保證** —— 輸入與輸出始終嚴格落在 `(0, 1)` 內，不會出現負勝率或超過 100% 的數值。
2. **0.5 即中性** —— `r = 0.5` 表示與中性對手勢均力敵，新玩家也從這個值起步。
3. **對稱** —— 交換兩名玩家並翻轉比賽結果，輸出的兩個新評分也隨之互換。`P(A 勝 B) + P(B 勝 A) = 1`。
4. **單調** —— 勝者評分上升，敗者評分下降。
5. **爆冷權重更高** —— 意外結果對評分的推動比預期結果更大，所以擊敗強敵比擊敗較弱的對手收穫更多。
6. **對數勝算零和** —— `θA' + θB' = θA + θB`。一場對局不改變雙方對數勝算之和，整體評分不會發生漂移。所有人從 0.5 出發時，`θ` 的總和恆為 0，這恰好與全域 Bradley–Terry 最大似然解的歸一化方式一致。
7. **可收斂** —— 每次更新都沿著全域 Bradley–Terry 對數似然的梯度方向前進。讓學習率符合 Robbins–Monro 條件並逐步衰減，評分就會收斂到全域最大似然解；測試套件以 MM 演算法算出的參考 MLE 加以驗證。

## 平手

一場對局只有兩種結果：A 勝或 B 勝。如果你的業務情境需要平手，請不要直接交給這個程式庫處理。

## 快速開始

TypeScript：

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

// alice 獲勝
const { ratingA, ratingB } = updateRatings(a, b, true);
ratings.set('alice', ratingA);
ratings.set('bob', ratingB);

console.log(winProbability(ratingA, ratingB)); // 更新後的預測勝率
```

C#：

```csharp
using DecentralizedBradleyTerry;

var ratings = new Dictionary<string, double>
{
    ["alice"] = Ratings.InitialRating,
    ["bob"] = Ratings.InitialRating,
};

// alice 獲勝
var (newAlice, newBob) = Ratings.Update(ratings["alice"], ratings["bob"], aWon: true);
ratings["alice"] = newAlice;
ratings["bob"] = newBob;

double p = Ratings.WinProbability(newAlice, newBob);
```

各語言的完整 API 請見對應套件的 README。

## 專案結構

```
.
├── typescript/   # npm 套件
├── csharp/       # NuGet 套件
└── docs/         # 本 README 的各語言版本
```

## 執行測試

TypeScript（需要 Node ≥ 23.6，透過原生型別移除直接執行 `.ts` 檔，無需建置）：

```bash
cd typescript
npm test
```

選用指令：

```bash
npm install        # 安裝 TypeScript 與 @types/node
npm run typecheck  # 型別檢查
npm run build      # 輸出 dist/
```

C#（需要 .NET 8 SDK）：

```bash
dotnet test csharp/test/DecentralizedBradleyTerry.Tests/DecentralizedBradleyTerry.Tests.csproj
```

兩套測試涵蓋預測公式、對稱性、單調性、對數勝算守恆、學習率的影響、非法輸入，以及線上更新能否收斂到全域 Bradley–Terry MLE 的端對端驗證。

## 授權條款

MIT
