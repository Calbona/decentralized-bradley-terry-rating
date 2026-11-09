import assert from 'node:assert/strict';
import test from 'node:test';

import {
  DEFAULT_LEARNING_RATE,
  INITIAL_RATING,
  updateRatings,
  winProbability,
} from '../src/index.ts';

const logit = (r: number): number => Math.log(r) - Math.log1p(-r);

test('exports the documented constants', () => {
  assert.equal(INITIAL_RATING, 0.5);
  assert.equal(DEFAULT_LEARNING_RATE, 0.25);
});

test('two neutral players are even', () => {
  assert.equal(winProbability(0.5, 0.5), 0.5);
});

test('prediction matches the Bradley–Terry strength formula', () => {
  // r = 0.5 -> gamma = 1, r = 0.75 -> gamma = 3, so P(A beats B) = 1 / 4.
  assert.ok(Math.abs(winProbability(0.5, 0.75) - 0.25) < 1e-15);
  assert.ok(Math.abs(winProbability(0.75, 0.5) - 0.75) < 1e-15);
});

test('prediction is symmetric and within (0, 1)', () => {
  const pairs: Array<[number, number]> = [
    [0.01, 0.99],
    [0.2, 0.8],
    [0.4, 0.6],
    [0.1, 0.5],
  ];
  for (const [a, b] of pairs) {
    const p = winProbability(a, b);
    assert.ok(p > 0 && p < 1);
    assert.ok(Math.abs(p + winProbability(b, a) - 1) < 1e-15);
  }
});

test('prediction is increasing in the player own rating', () => {
  assert.ok(winProbability(0.6, 0.4) > winProbability(0.5, 0.4));
  assert.ok(winProbability(0.5, 0.4) > winProbability(0.4, 0.4));
});

test('the winner rises and the loser falls', () => {
  const ratings: Array<[number, number]> = [
    [0.5, 0.5],
    [0.2, 0.9],
    [0.9, 0.2],
    [0.4, 0.4],
  ];
  for (const [a, b] of ratings) {
    for (const eta of [0.05, 0.25, 1]) {
      const aWins = updateRatings(a, b, true, { learningRate: eta });
      assert.ok(aWins.ratingA > a, `A should rise for (${a}, ${b})`);
      assert.ok(aWins.ratingB < b, `B should fall for (${a}, ${b})`);

      const bWins = updateRatings(a, b, false, { learningRate: eta });
      assert.ok(bWins.ratingA < a, `A should fall for (${a}, ${b})`);
      assert.ok(bWins.ratingB > b, `B should rise for (${a}, ${b})`);
    }
  }
});

test('updated ratings stay strictly inside (0, 1)', () => {
  const extremes: Array<[number, number]> = [
    [1e-9, 1 - 1e-9],
    [1 - 1e-9, 1e-9],
    [0.5, 1 - 1e-12],
  ];
  for (const [a, b] of extremes) {
    for (const aWon of [true, false]) {
      const next = updateRatings(a, b, aWon, { learningRate: 1 });
      assert.ok(next.ratingA > 0 && next.ratingA < 1);
      assert.ok(next.ratingB > 0 && next.ratingB < 1);
    }
  }
});

test('extreme values keep the outputs strictly inside (0, 1)', () => {
  const huge = updateRatings(0.5, 0.5, true, {
    learningRate: Number.MAX_VALUE,
  });
  assert.ok(huge.ratingA > 0 && huge.ratingA < 1);
  assert.ok(huge.ratingB > 0 && huge.ratingB < 1);

  const nearEdges = updateRatings(
    1 - Number.EPSILON / 2,
    Number.MIN_VALUE,
    false,
    { learningRate: 1 },
  );
  assert.ok(nearEdges.ratingA > 0 && nearEdges.ratingA < 1);
  assert.ok(nearEdges.ratingB > 0 && nearEdges.ratingB < 1);
});

test('the pair log-odds are conserved (zero-sum update)', () => {
  const cases: Array<[number, number, boolean]> = [
    [0.3, 0.8, true],
    [0.3, 0.8, false],
    [0.55, 0.45, true],
    [0.05, 0.95, false],
  ];
  for (const [a, b, aWon] of cases) {
    const next = updateRatings(a, b, aWon, { learningRate: 0.7 });
    assert.ok(
      Math.abs(logit(next.ratingA) + logit(next.ratingB) - logit(a) - logit(b)) < 1e-12,
    );
  }
});

test('swapping the players and the result mirrors the update', () => {
  const ab = updateRatings(0.3, 0.8, true, { learningRate: 0.4 });
  const ba = updateRatings(0.8, 0.3, false, { learningRate: 0.4 });
  assert.ok(Math.abs(ab.ratingA - ba.ratingB) < 1e-12);
  assert.ok(Math.abs(ab.ratingB - ba.ratingA) < 1e-12);
});

test('a zero learning rate leaves the ratings unchanged', () => {
  const next = updateRatings(0.3, 0.8, true, { learningRate: 0 });
  assert.ok(Math.abs(next.ratingA - 0.3) < 1e-12);
  assert.ok(Math.abs(next.ratingB - 0.8) < 1e-12);
});

test('the update is pure and deterministic', () => {
  const first = updateRatings(0.42, 0.37, true);
  const second = updateRatings(0.42, 0.37, true);
  assert.deepEqual(first, second);
});

test('a larger learning rate moves the ratings further', () => {
  const small = updateRatings(0.5, 0.5, true, { learningRate: 0.1 });
  const large = updateRatings(0.5, 0.5, true, { learningRate: 0.4 });
  assert.ok(large.ratingA > small.ratingA);
  assert.ok(large.ratingB < small.ratingB);
});

test('an upset moves the ratings more than an expected win', () => {
  const favourite = 0.8;
  const underdog = 0.2;
  const expectedWin = updateRatings(favourite, underdog, true, { learningRate: 0.25 });
  const upset = updateRatings(favourite, underdog, false, { learningRate: 0.25 });

  assert.ok(
    Math.abs(logit(upset.ratingA) - logit(favourite)) >
      Math.abs(logit(expectedWin.ratingA) - logit(favourite)),
  );
});

test('one win between equals moves each log-odds by half the learning rate', () => {
  const eta = 0.25;
  const next = updateRatings(0.5, 0.5, true, { learningRate: eta });
  assert.ok(Math.abs(logit(next.ratingA) - eta / 2) < 1e-12);
  assert.ok(Math.abs(logit(next.ratingB) + eta / 2) < 1e-12);
  // The post-update predicted probability is sigmoid(eta).
  assert.ok(
    Math.abs(winProbability(next.ratingA, next.ratingB) - 1 / (1 + Math.exp(-eta))) <
      1e-12,
  );
});

test('rejects ratings outside (0, 1)', () => {
  for (const bad of [0, 1, -0.1, 1.1, Number.NaN, Number.POSITIVE_INFINITY]) {
    assert.throws(() => winProbability(bad, 0.5), RangeError);
    assert.throws(() => winProbability(0.5, bad), RangeError);
    assert.throws(() => updateRatings(bad, 0.5, true), RangeError);
    assert.throws(() => updateRatings(0.5, bad, true), RangeError);
  }
});

test('rejects a negative or non-finite learning rate', () => {
  for (const bad of [-0.1, Number.NaN, Number.POSITIVE_INFINITY]) {
    assert.throws(
      () => updateRatings(0.5, 0.5, true, { learningRate: bad }),
      RangeError,
    );
  }
});

test('online updates converge to the global Bradley–Terry MLE', () => {
  // Deterministic three-player round robin generated from known strengths.
  const strengths = [1, 2, 4];
  const gamesPerPair = 200;
  const random = mulberry32(12345);
  const games: Array<{ a: number; b: number; aWon: boolean }> = [];

  for (let a = 0; a < strengths.length; a++) {
    for (let b = a + 1; b < strengths.length; b++) {
      const p = strengths[a] / (strengths[a] + strengths[b]);
      for (let k = 0; k < gamesPerPair; k++) {
        games.push({ a, b, aWon: random() < p });
      }
    }
  }

  const mle = maximumLikelihoodRatings(strengths.length, games);

  // Decentralized online updates with a decaying learning rate.
  const ratings = new Array(strengths.length).fill(INITIAL_RATING);
  let step = 0;
  for (let epoch = 0; epoch < 200; epoch++) {
    for (const game of games) {
      const learningRate = 5 / (step + 5);
      const next = updateRatings(ratings[game.a], ratings[game.b], game.aWon, {
        learningRate,
      });
      ratings[game.a] = next.ratingA;
      ratings[game.b] = next.ratingB;
      step++;
    }
  }

  for (let a = 0; a < strengths.length; a++) {
    for (let b = a + 1; b < strengths.length; b++) {
      const got = winProbability(ratings[a], ratings[b]);
      const want = winProbability(mle[a], mle[b]);
      assert.ok(
        Math.abs(got - want) < 0.005,
        `P(${a} beats ${b}): online ${got.toFixed(5)}, MLE ${want.toFixed(5)}`,
      );
    }
  }
});

/**
 * Reference global Bradley–Terry MLE via the standard minorization–maximization
 * iteration, normalized so the geometric mean strength is 1 (sum of log-odds 0).
 */
function maximumLikelihoodRatings(
  playerCount: number,
  games: Array<{ a: number; b: number; aWon: boolean }>,
): number[] {
  const wins = new Array(playerCount).fill(0);
  const played = Array.from({ length: playerCount }, () =>
    new Array(playerCount).fill(0),
  );

  for (const game of games) {
    played[game.a][game.b]++;
    played[game.b][game.a]++;
    if (game.aWon) {
      wins[game.a]++;
    } else {
      wins[game.b]++;
    }
  }

  let strength = new Array(playerCount).fill(1);
  for (let iteration = 0; iteration < 5000; iteration++) {
    const next = strength.slice();
    for (let i = 0; i < playerCount; i++) {
      let denominator = 0;
      for (let j = 0; j < playerCount; j++) {
        if (j !== i) {
          denominator += played[i][j] / (strength[i] + strength[j]);
        }
      }
      if (wins[i] === 0 || denominator === 0) {
        throw new Error('MLE reference requires at least one win per player');
      }
      next[i] = wins[i] / denominator;
    }

    const logMean =
      next.reduce((sum: number, value: number) => sum + Math.log(value), 0) /
      playerCount;
    strength = next.map((value: number) => Math.exp(Math.log(value) - logMean));
  }

  return strength.map((value: number) => value / (1 + value));
}

/** Small deterministic PRNG so the convergence test never flakes. */
function mulberry32(seed: number): () => number {
  let state = seed;
  return () => {
    state = (state + 0x6d2b79f5) | 0;
    let t = Math.imul(state ^ (state >>> 15), 1 | state);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
