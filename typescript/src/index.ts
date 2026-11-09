/**
 * Decentralized Bradley–Terry rating.
 *
 * A rating is a relative win rate in (0, 1): the probability that the player
 * beats a neutral opponent whose rating is 0.5. Equivalently, the player's
 * Bradley–Terry strength is gamma = r / (1 - r), and r = sigmoid(log(gamma)).
 *
 * Every update needs only the two players' current ratings and the result of
 * their match. The update is one online gradient-ascent step on the global
 * Bradley–Terry log-likelihood, so repeated updates (with a decaying learning
 * rate) converge to the global maximum-likelihood strengths.
 */

/** Rating of a player who is even with a neutral opponent. */
export const INITIAL_RATING = 0.5;

/**
 * Default learning rate in log-odds units per match. Roughly equivalent to an
 * Elo K-factor of 43 (eta = K * ln(10) / 400).
 */
export const DEFAULT_LEARNING_RATE = 0.25;

/** Optional parameters for {@link updateRatings}. */
export interface RatingUpdateOptions {
  /**
   * Non-negative, finite step size in log-odds units. Defaults to
   * {@link DEFAULT_LEARNING_RATE}. Use a decaying value for convergence to the
   * global Bradley–Terry maximum-likelihood solution.
   */
  readonly learningRate?: number;
}

/** The two players' ratings after a match. */
export interface RatingUpdate {
  readonly ratingA: number;
  readonly ratingB: number;
}

/**
 * Probability that player A beats player B under the Bradley–Terry model.
 *
 * @throws RangeError if either rating is not a finite number in (0, 1).
 */
export function winProbability(ratingA: number, ratingB: number): number {
  validateRating(ratingA, 'ratingA');
  validateRating(ratingB, 'ratingB');
  return winProbabilityUnchecked(ratingA, ratingB);
}

/**
 * Applies one match result and returns both players' new relative win rates.
 *
 * The update is a pure function: it does not mutate its arguments and always
 * returns the same outputs for the same inputs.
 *
 * @param ratingA Current relative win rate of player A, in (0, 1).
 * @param ratingB Current relative win rate of player B, in (0, 1).
 * @param aWon    True if player A won, false if player B won. Draws are not
 *                supported.
 * @param options Optional learning rate.
 * @throws RangeError if a rating is outside (0, 1) or the learning rate is
 *         negative or not finite.
 */
export function updateRatings(
  ratingA: number,
  ratingB: number,
  aWon: boolean,
  options: RatingUpdateOptions = {},
): RatingUpdate {
  validateRating(ratingA, 'ratingA');
  validateRating(ratingB, 'ratingB');

  const learningRate = options.learningRate ?? DEFAULT_LEARNING_RATE;
  if (!Number.isFinite(learningRate) || learningRate < 0) {
    throw new RangeError(
      `learningRate must be a finite number >= 0, got ${learningRate}`,
    );
  }

  const score = aWon ? 1 : 0;
  const delta =
    learningRate * (score - winProbabilityUnchecked(ratingA, ratingB));

  // The log-odds change of A is the negative of B's: the pair's total
  // log-odds are conserved, so no global reference drift is introduced.
  return {
    ratingA: clampRating(logistic(logit(ratingA) + delta)),
    ratingB: clampRating(logistic(logit(ratingB) - delta)),
  };
}

/** Largest double strictly below 1. */
const MAX_RATING = 1 - Number.EPSILON / 2;
/** Smallest positive double. */
const MIN_RATING = Number.MIN_VALUE;

/** Guards the documented (0, 1) range against floating-point rounding. */
function clampRating(rating: number): number {
  return Math.min(MAX_RATING, Math.max(MIN_RATING, rating));
}

function winProbabilityUnchecked(ratingA: number, ratingB: number): number {
  const oddsA = ratingA * (1 - ratingB);
  const oddsB = ratingB * (1 - ratingA);
  return oddsA / (oddsA + oddsB);
}

function logit(rating: number): number {
  return Math.log(rating) - Math.log1p(-rating);
}

function logistic(x: number): number {
  // Numerically stable for large |x|: never evaluates exp(x) for positive x.
  return x >= 0
    ? 1 / (1 + Math.exp(-x))
    : Math.exp(x) / (1 + Math.exp(x));
}

function validateRating(value: number, name: string): void {
  if (!Number.isFinite(value) || value <= 0 || value >= 1) {
    throw new RangeError(
      `${name} must be a finite number strictly between 0 and 1, got ${value}`,
    );
  }
}
