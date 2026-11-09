# DecentralizedBradleyTerry

Bradley–Terry ratings, one match at a time. Every update needs only the two players' current ratings and the result — no central server, no global match ledger, no player IDs. The update is a pure function, so you can run it anywhere: a client, a serverless function, or a peer-to-peer network.

This is the C# implementation targeting .NET 8. A semantically identical [TypeScript version](https://github.com/Calbona/decentralized-bradley-terry-rating/blob/main/typescript/README.md) lives in the same repository.

## Install

```bash
dotnet add package DecentralizedBradleyTerry
```

The package has no dependencies beyond the .NET 8 base class library.

## Usage

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

To use a different learning rate:

```csharp
Ratings.Update(a, b, aWon: true, learningRate: 0.1);
```

To converge on the global maximum-likelihood solution, decay the rate as matches accumulate:

```csharp
double learningRate = 5d / (matchesPlayed + 5d);
var (nextA, nextB) = Ratings.Update(a, b, aWon: true, learningRate);
```

## API

| Member | Description |
| --- | --- |
| `Ratings.InitialRating` | Starting rating for a new player: `0.5`. |
| `Ratings.DefaultLearningRate` | Default step size: `0.25`. |
| `Ratings.WinProbability(ratingA, ratingB)` | Probability that A beats B. |
| `Ratings.Update(ratingA, ratingB, aWon, learningRate = 0.25)` | Returns `(RatingA, RatingB)` after one match. |

Pass a non-negative, finite `learningRate`; a value of `0` leaves both ratings unchanged.

A rating outside `(0, 1)`, or a learning rate that is negative or not finite, throws an `ArgumentOutOfRangeException`.

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
7. **Convergent** — each update moves along the gradient of the global Bradley–Terry log-likelihood. Decay the learning rate to satisfy the Robbins–Monro conditions and the ratings converge to the global MLE.

## Draws

A match has exactly two outcomes: A wins or B wins. If your domain needs draws, do not pass them to this library as-is.

## Development

Requires the .NET 8 SDK.

```bash
dotnet test test/DecentralizedBradleyTerry.Tests/DecentralizedBradleyTerry.Tests.csproj
```

## License

MIT
