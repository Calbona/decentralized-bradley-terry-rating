# 去中心化 Bradley–Terry 评分

每局比赛结束后，只凭两名玩家当前的相对胜率和比赛结果，就能算出新评分。不需要中心服务器，不需要全局对战记录，也不需要玩家编号。仓库提供 TypeScript 和 C# 两套语义一致的实现，核心都是纯函数。

**语言：** [English](../README.md) · **简体中文** · [繁體中文](zh-TW.md) · [Deutsch](de-DE.md) · [Español](es-ES.md) · [Français](fr-FR.md) · [Italiano](it-IT.md) · [日本語](ja-JP.md) · [한국어](ko-KR.md) · [Русский](ru-RU.md) · [Tiếng Việt](vi-VN.md)

## 发布包

| 语言 | 包 | 文档 |
| --- | --- | --- |
| TypeScript | [`decentralized-bradley-terry-rating`](https://www.npmjs.com/package/decentralized-bradley-terry-rating) | [typescript/README.md](../typescript/README.md) |
| C# (.NET 8) | [`DecentralizedBradleyTerry`](https://www.nuget.org/packages/DecentralizedBradleyTerry) | [csharp/README.md](../csharp/README.md) |

## 模型

评分 `r ∈ (0, 1)` 是玩家的相对胜率：面对评分为 `r = 0.5` 的中性对手时的获胜概率。它对应 Bradley–Terry 强度

```
γ = r / (1 - r)
```

两名玩家的对局预测为

```
P(A 胜 B) = γA / (γA + γB)
          = rA(1 - rB) / (rA(1 - rB) + rB(1 - rA))
```

设 `θ = logit(r) = log(γ)`，`p = P(A 胜 B)`，`s = 1` 表示 A 获胜、`0` 表示 A 落败，`η` 为学习率：

```
θA' = θA + η · (s - p)
θB' = θB - η · (s - p)
r'  = σ(θ')
```

这就是对全局 Bradley–Terry 对数似然做的一步在线梯度上升，也就是对数几率形式的 Elo。默认 `η = 0.25`，大致相当于国际象棋的 `K ≈ 43`（`η = K · ln 10 / 400`）。

学习率固定时，在线更新会稳定在全局解附近 `O(η)` 的范围内。若要严格收敛到最大似然解，可让学习率随已进行的对局数 `t` 衰减，例如 `η_t = η0 / (t + τ)`。库本身不保存状态，衰减策略由调用方自行传入。

## 特性

1. **取值有保证** —— 输入和输出始终严格落在 `(0, 1)` 内，不会出现负胜率或超过 100% 的数值。
2. **0.5 即中性** —— `r = 0.5` 表示与中性对手势均力敌，新玩家也从这个值起步。
3. **对称** —— 交换两名玩家并翻转比赛结果，输出的两个新评分也随之互换。`P(A 胜 B) + P(B 胜 A) = 1`。
4. **单调** —— 胜者评分上升，败者评分下降。
5. **冷门权重更高** —— 意外结果对评分的推动比预期结果更大，所以战胜强手比战胜弱手收获更多。
6. **对数几率零和** —— `θA' + θB' = θA + θB`。一局比赛不改变双方对数几率之和，整体评分不会发生漂移。所有人从 0.5 出发时，`θ` 的总和恒为 0，这恰好与全局 Bradley–Terry 最大似然解的归一化方式一致。
7. **可收敛** —— 每次更新都沿全局 Bradley–Terry 对数似然的梯度方向前进。让学习率满足 Robbins–Monro 条件并逐步衰减，评分就会收敛到全局最大似然解；测试套件用 MM 算法算出的参考 MLE 对此做了验证。

## 平局

一局比赛只有两种结果：A 胜或 B 胜。如果你的业务场景需要平局，请不要直接交给这个库处理。

## 快速开始

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

// alice 获胜
const { ratingA, ratingB } = updateRatings(a, b, true);
ratings.set('alice', ratingA);
ratings.set('bob', ratingB);

console.log(winProbability(ratingA, ratingB)); // 更新后的预测胜率
```

C#：

```csharp
using DecentralizedBradleyTerry;

var ratings = new Dictionary<string, double>
{
    ["alice"] = Ratings.InitialRating,
    ["bob"] = Ratings.InitialRating,
};

// alice 获胜
var (newAlice, newBob) = Ratings.Update(ratings["alice"], ratings["bob"], aWon: true);
ratings["alice"] = newAlice;
ratings["bob"] = newBob;

double p = Ratings.WinProbability(newAlice, newBob);
```

各语言的完整 API 见对应包的 README。

## 仓库结构

```
.
├── typescript/   # npm 包
├── csharp/       # NuGet 包
└── docs/         # 本 README 的各语言版本
```

## 运行测试

TypeScript（需要 Node ≥ 23.6，借助原生类型擦除直接运行 `.ts` 文件，无需构建）：

```bash
cd typescript
npm test
```

可选命令：

```bash
npm install        # 安装 TypeScript 和 @types/node
npm run typecheck  # 类型检查
npm run build      # 输出 dist/
```

C#（需要 .NET 8 SDK）：

```bash
dotnet test csharp/test/DecentralizedBradleyTerry.Tests/DecentralizedBradleyTerry.Tests.csproj
```

两个测试套件覆盖预测公式、对称性、单调性、对数几率守恒、学习率的影响、非法输入，以及在线更新能否收敛到全局 Bradley–Terry MLE 的端到端验证。

## 许可证

MIT
