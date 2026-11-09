# Decentralized Bradley–Terry Rating

Update a rating from a single match using nothing but the two players' current ratings and who won. No central server, no global match ledger, no player IDs. The repo ships two semantically identical implementations — TypeScript and C# — both built around pure functions.

**Languages:** [English](README.md) · [简体中文](docs/zh-CN.md) · [繁體中文](docs/zh-TW.md) · [Deutsch](docs/de-DE.md) · [Español](docs/es-ES.md) · [Français](docs/fr-FR.md) · [Italiano](docs/it-IT.md) · [日本語](docs/ja-JP.md) · [한국어](docs/ko-KR.md) · [Русский](docs/ru-RU.md) · [Tiếng Việt](docs/vi-VN.md)

## Packages

| Language | Package | Documentation |
| --- | --- | --- |
| TypeScript | [`decentralized-bradley-terry-rating`](https://www.npmjs.com/package/decentralized-bradley-terry-rating) | [typescript/README.md](typescript/README.md) |
| C# (.NET 8) | [`DecentralizedBradleyTerry`](https://www.nuget.org/packages/DecentralizedBradleyTerry) | [csharp/README.md](csharp/README.md) |

## The model

A rating `r ∈ (0, 1)` is the player's relative win rate: the probability of beating a neutral opponent rated `r = 0.5`. It corresponds to a Bradley–Terry strength of

```
γ = r / (1 - r)
```

and a match between two players is predicted as

```
P(A beats B) = γA / (γA + γB)
             = rA(1 - rB) / (rA(1 - rB) + rB(1 - rA))
```

Let `θ = logit(r) = log(γ)`, `p = P(A beats B)`, `s = 1` if A won and `0` otherwise, and `η` be the learning rate:

```
θA' = θA + η · (s - p)
θB' = θB - η · (s - p)
r'  = σ(θ')
```

This is one online gradient-ascent step on the global Bradley–Terry log-likelihood — Elo in log-odds form. The default `η = 0.25` is roughly equivalent to chess `K ≈ 43` (`η = K · ln 10 / 400`).

With a fixed learning rate, the online update settles into an `O(η)` neighbourhood of the global solution. For strict convergence to the maximum-likelihood solution, decay the rate with the number of matches played, for example `η_t = η0 / (t + τ)`. The library holds no state, so the schedule is yours to pass in.

## Properties

1. **Bounded** — inputs and outputs are always strictly inside `(0, 1)`. No negative win rates, no values above 100%.
2. **Neutral at 0.5** — `r = 0.5` is an even match against a neutral opponent, and every new player starts there.
3. **Symmetric** — swap the players and flip the result, and the new ratings swap with them. `P(A beats B) + P(B beats A) = 1`.
4. **Monotone** — the winner's rating goes up, the loser's goes down.
5. **Upsets count more** — an unexpected result moves the ratings further than an expected one, so beating a stronger player pays more than beating a weaker one.
6. **Zero-sum in log-odds** — `θA' + θB' = θA + θB`. A match never changes the pair's combined log-odds, so the rating pool as a whole does not drift. Start everyone at 0.5 and the total `θ` stays at 0, which is exactly the normalization used by the global Bradley–Terry maximum-likelihood solution.
7. **Convergent** — each update moves along the gradient of the global Bradley–Terry log-likelihood. Decay the learning rate to satisfy the Robbins–Monro conditions and the ratings converge to the global MLE; the test suites check this against a reference MLE computed with the MM algorithm.

## Draws

A match has exactly two outcomes: A wins or B wins. If your domain needs draws, do not pass them to this library as-is.

## Quick start

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

// alice wins
const { ratingA, ratingB } = updateRatings(a, b, true);
ratings.set('alice', ratingA);
ratings.set('bob', ratingB);

console.log(winProbability(ratingA, ratingB)); // updated prediction
```

C#:

```csharp
using DecentralizedBradleyTerry;

var ratings = new Dictionary<string, double>
{
    ["alice"] = Ratings.InitialRating,
    ["bob"] = Ratings.InitialRating,
};

// alice wins
var (newAlice, newBob) = Ratings.Update(ratings["alice"], ratings["bob"], aWon: true);
ratings["alice"] = newAlice;
ratings["bob"] = newBob;

double p = Ratings.WinProbability(newAlice, newBob);
```

The full API for each language lives in its package README.

## Repository layout

```
.
├── typescript/   # npm package
├── csharp/       # NuGet package
└── docs/         # translations of this README
```

## Running the tests

TypeScript (Node ≥ 23.6, which runs `.ts` files directly through native type stripping — no build step):

```bash
cd typescript
npm test
```

Optional:

```bash
npm install        # install TypeScript and @types/node
npm run typecheck  # type-check
npm run build      # emit dist/
```

C# (.NET 8 SDK):

```bash
dotnet test csharp/test/DecentralizedBradleyTerry.Tests/DecentralizedBradleyTerry.Tests.csproj
```

Both suites cover the prediction formula, symmetry, monotonicity, log-odds conservation, learning-rate effects, invalid input, and an end-to-end check that online updates converge to the global Bradley–Terry MLE.

## License

MIT
