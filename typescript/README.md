# decentralized-bradley-terry-rating

Bradley–Terry ratings, one match at a time. Every update needs only the two players' current ratings and the result — no central server, no global match ledger, no player IDs. The update is a pure function, so you can run it anywhere: a client, a serverless function, or a peer-to-peer network.

This is the TypeScript implementation. A semantically identical [C# version](https://github.com/Calbona/decentralized-bradley-terry-rating/blob/main/csharp/README.md) lives in the same repository.

## Install

```bash
npm install decentralized-bradley-terry-rating
```

The package is ESM-only and ships its own type declarations. It has no runtime dependencies.

## Usage

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

To use a different learning rate:

```ts
updateRatings(a, b, true, { learningRate: 0.1 });
```

To converge on the global maximum-likelihood solution, decay the rate as matches accumulate:

```ts
const learningRate = 5 / (matchesPlayed + 5);
const next = updateRatings(a, b, true, { learningRate });
```

## API

| Export | Description |
| --- | --- |
| `INITIAL_RATING` | Starting rating for a new player: `0.5`. |
| `DEFAULT_LEARNING_RATE` | Default step size: `0.25`. |
| `winProbability(ratingA, ratingB)` | Probability that A beats B. |
| `updateRatings(ratingA, ratingB, aWon, options?)` | Returns `{ ratingA, ratingB }` after one match. |

`updateRatings` accepts an optional `options` object with a `learningRate` field. Pass a non-negative, finite value; a value of `0` leaves both ratings unchanged.

A rating outside `(0, 1)`, or a learning rate that is negative or not finite, throws a `RangeError`.

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

Requires Node ≥ 23.6, which runs `.ts` files directly through native type stripping — no build step needed for tests.

```bash
npm test           # run the test suite
npm run typecheck  # type-check source and tests
npm run build      # emit dist/
```

## License

MIT
