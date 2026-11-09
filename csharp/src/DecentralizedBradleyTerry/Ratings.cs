using System;

namespace DecentralizedBradleyTerry;

/// <summary>
/// Decentralized Bradley–Terry rating.
/// <para>
/// A rating is a relative win rate in (0, 1): the probability that the player
/// beats a neutral opponent whose rating is 0.5. Equivalently, the player's
/// Bradley–Terry strength is gamma = r / (1 - r), and r = sigmoid(log(gamma)).
/// </para>
/// <para>
/// Every update needs only the two players' current ratings and the result of
/// their match. The update is one online gradient-ascent step on the global
/// Bradley–Terry log-likelihood, so repeated updates (with a decaying learning
/// rate) converge to the global maximum-likelihood strengths.
/// </para>
/// </summary>
public static class Ratings
{
    /// <summary>Rating of a player who is even with a neutral opponent.</summary>
    public const double InitialRating = 0.5;

    /// <summary>
    /// Default learning rate in log-odds units per match. Roughly equivalent to
    /// an Elo K-factor of 43 (eta = K * ln(10) / 400).
    /// </summary>
    public const double DefaultLearningRate = 0.25;

    /// <summary>
    /// Probability that player A beats player B under the Bradley–Terry model.
    /// </summary>
    /// <param name="ratingA">Relative win rate of player A, in (0, 1).</param>
    /// <param name="ratingB">Relative win rate of player B, in (0, 1).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A rating is not a finite number strictly between 0 and 1.
    /// </exception>
    public static double WinProbability(double ratingA, double ratingB)
    {
        ValidateRating(ratingA, nameof(ratingA));
        ValidateRating(ratingB, nameof(ratingB));
        return WinProbabilityUnchecked(ratingA, ratingB);
    }

    /// <summary>
    /// Applies one match result and returns both players' new relative win
    /// rates. The update is a pure function of its arguments.
    /// </summary>
    /// <param name="ratingA">Current relative win rate of player A, in (0, 1).</param>
    /// <param name="ratingB">Current relative win rate of player B, in (0, 1).</param>
    /// <param name="aWon">
    /// <c>true</c> if player A won, <c>false</c> if player B won. Draws are not
    /// supported.
    /// </param>
    /// <param name="learningRate">
    /// Non-negative, finite step size in log-odds units. Defaults to
    /// <see cref="DefaultLearningRate"/>. Use a decaying value for convergence
    /// to the global Bradley–Terry maximum-likelihood solution.
    /// </param>
    /// <returns>The two players' new relative win rates.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A rating is not a finite number strictly between 0 and 1, or the learning
    /// rate is negative or not finite.
    /// </exception>
    public static (double RatingA, double RatingB) Update(
        double ratingA,
        double ratingB,
        bool aWon,
        double learningRate = DefaultLearningRate)
    {
        ValidateRating(ratingA, nameof(ratingA));
        ValidateRating(ratingB, nameof(ratingB));
        if (!double.IsFinite(learningRate) || learningRate < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(learningRate),
                learningRate,
                "Learning rate must be a finite number greater than or equal to zero.");
        }

        double score = aWon ? 1d : 0d;
        double delta = learningRate * (score - WinProbabilityUnchecked(ratingA, ratingB));

        // The log-odds change of A is the negative of B's: the pair's total
        // log-odds are conserved, so no global reference drift is introduced.
        return (
            ClampRating(Logistic(Logit(ratingA) + delta)),
            ClampRating(Logistic(Logit(ratingB) - delta)));
    }

    /// <summary>Guards the documented (0, 1) range against floating-point rounding.</summary>
    private static double ClampRating(double rating) =>
        Math.Clamp(rating, double.Epsilon, Math.BitDecrement(1d));

    private static double WinProbabilityUnchecked(double ratingA, double ratingB)
    {
        double oddsA = ratingA * (1d - ratingB);
        double oddsB = ratingB * (1d - ratingA);
        return oddsA / (oddsA + oddsB);
    }

    private static double Logit(double rating) => Math.Log(rating) - Math.Log(1d - rating);

    private static double Logistic(double x) => x >= 0d
        ? 1d / (1d + Math.Exp(-x))
        : Math.Exp(x) / (1d + Math.Exp(x));

    private static void ValidateRating(double value, string paramName)
    {
        if (!double.IsFinite(value) || value <= 0d || value >= 1d)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                "Rating must be a finite number strictly between 0 and 1.");
        }
    }
}
