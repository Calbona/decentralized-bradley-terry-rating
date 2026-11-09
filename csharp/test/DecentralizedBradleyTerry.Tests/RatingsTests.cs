using System;
using System.Collections.Generic;
using Xunit;

namespace DecentralizedBradleyTerry.Tests;

public class RatingsTests
{
    [Fact]
    public void ExportsDocumentedConstants()
    {
        Assert.Equal(0.5, Ratings.InitialRating);
        Assert.Equal(0.25, Ratings.DefaultLearningRate);
    }

    [Fact]
    public void TwoNeutralPlayersAreEven()
    {
        Assert.Equal(0.5, Ratings.WinProbability(0.5, 0.5), 15);
    }

    [Fact]
    public void PredictionMatchesBradleyTerryStrengthFormula()
    {
        // r = 0.5 -> gamma = 1, r = 0.75 -> gamma = 3, so P(A beats B) = 1 / 4.
        Assert.Equal(0.25, Ratings.WinProbability(0.5, 0.75), 15);
        Assert.Equal(0.75, Ratings.WinProbability(0.75, 0.5), 15);
    }

    [Theory]
    [InlineData(0.01, 0.99)]
    [InlineData(0.2, 0.8)]
    [InlineData(0.4, 0.6)]
    [InlineData(0.1, 0.5)]
    public void PredictionIsSymmetricAndWithinUnitInterval(double a, double b)
    {
        double p = Ratings.WinProbability(a, b);
        Assert.True(p > 0d && p < 1d);
        Assert.Equal(1d, p + Ratings.WinProbability(b, a), 12);
    }

    [Fact]
    public void PredictionIsIncreasingInOwnRating()
    {
        Assert.True(Ratings.WinProbability(0.6, 0.4) > Ratings.WinProbability(0.5, 0.4));
        Assert.True(Ratings.WinProbability(0.5, 0.4) > Ratings.WinProbability(0.4, 0.4));
    }

    [Theory]
    [InlineData(0.5, 0.5, 0.05)]
    [InlineData(0.5, 0.5, 0.25)]
    [InlineData(0.5, 0.5, 1.0)]
    [InlineData(0.2, 0.9, 0.25)]
    [InlineData(0.9, 0.2, 0.25)]
    [InlineData(0.4, 0.4, 0.1)]
    public void WinnerRisesAndLoserFalls(double a, double b, double eta)
    {
        var (aAfterWin, bAfterWin) = Ratings.Update(a, b, aWon: true, learningRate: eta);
        Assert.True(aAfterWin > a);
        Assert.True(bAfterWin < b);

        var (aAfterLoss, bAfterLoss) = Ratings.Update(a, b, aWon: false, learningRate: eta);
        Assert.True(aAfterLoss < a);
        Assert.True(bAfterLoss > b);
    }

    [Theory]
    [InlineData(1e-9, 0.999999999)]
    [InlineData(0.999999999, 1e-9)]
    [InlineData(0.5, 0.999999999999)]
    public void UpdatedRatingsStayStrictlyInsideUnitInterval(double a, double b)
    {
        foreach (bool aWon in new[] { true, false })
        {
            var (ra, rb) = Ratings.Update(a, b, aWon, learningRate: 1d);
            Assert.True(ra > 0d && ra < 1d);
            Assert.True(rb > 0d && rb < 1d);
        }
    }

    [Fact]
    public void ExtremeValuesKeepOutputsStrictlyInsideUnitInterval()
    {
        var huge = Ratings.Update(0.5, 0.5, aWon: true, learningRate: double.MaxValue);
        Assert.True(huge.RatingA > 0d && huge.RatingA < 1d);
        Assert.True(huge.RatingB > 0d && huge.RatingB < 1d);

        var nearEdges = Ratings.Update(
            Math.BitDecrement(1d),
            double.Epsilon,
            aWon: false,
            learningRate: 1d);
        Assert.True(nearEdges.RatingA > 0d && nearEdges.RatingA < 1d);
        Assert.True(nearEdges.RatingB > 0d && nearEdges.RatingB < 1d);
    }

    [Theory]
    [InlineData(0.3, 0.8, true, 0.7)]
    [InlineData(0.3, 0.8, false, 0.7)]
    [InlineData(0.55, 0.45, true, 0.7)]
    [InlineData(0.05, 0.95, false, 0.7)]
    public void PairLogOddsAreConserved(double a, double b, bool aWon, double eta)
    {
        var (ra, rb) = Ratings.Update(a, b, aWon, eta);
        Assert.Equal(Logit(a) + Logit(b), Logit(ra) + Logit(rb), 12);
    }

    [Fact]
    public void SwappingPlayersAndResultMirrorsTheUpdate()
    {
        var ab = Ratings.Update(0.3, 0.8, aWon: true, learningRate: 0.4);
        var ba = Ratings.Update(0.8, 0.3, aWon: false, learningRate: 0.4);
        Assert.Equal(ab.RatingA, ba.RatingB, 12);
        Assert.Equal(ab.RatingB, ba.RatingA, 12);
    }

    [Fact]
    public void ZeroLearningRateLeavesRatingsUnchanged()
    {
        var (ra, rb) = Ratings.Update(0.3, 0.8, aWon: true, learningRate: 0d);
        Assert.Equal(0.3, ra, 12);
        Assert.Equal(0.8, rb, 12);
    }

    [Fact]
    public void UpdateIsPureAndDeterministic()
    {
        var first = Ratings.Update(0.42, 0.37, aWon: true);
        var second = Ratings.Update(0.42, 0.37, aWon: true);
        Assert.Equal(first, second);
    }

    [Fact]
    public void LargerLearningRateMovesRatingsFurther()
    {
        var small = Ratings.Update(0.5, 0.5, aWon: true, learningRate: 0.1);
        var large = Ratings.Update(0.5, 0.5, aWon: true, learningRate: 0.4);
        Assert.True(large.RatingA > small.RatingA);
        Assert.True(large.RatingB < small.RatingB);
    }

    [Fact]
    public void UpsetMovesRatingsMoreThanExpectedWin()
    {
        const double favourite = 0.8;
        const double underdog = 0.2;
        var expectedWin = Ratings.Update(favourite, underdog, aWon: true, learningRate: 0.25);
        var upset = Ratings.Update(favourite, underdog, aWon: false, learningRate: 0.25);

        Assert.True(
            Math.Abs(Logit(upset.RatingA) - Logit(favourite)) >
            Math.Abs(Logit(expectedWin.RatingA) - Logit(favourite)));
    }

    [Fact]
    public void OneWinBetweenEqualsMovesEachLogOddsByHalfTheLearningRate()
    {
        const double eta = 0.25;
        var (ra, rb) = Ratings.Update(0.5, 0.5, aWon: true, learningRate: eta);

        Assert.Equal(eta / 2d, Logit(ra), 12);
        Assert.Equal(-eta / 2d, Logit(rb), 12);
        Assert.Equal(1d / (1d + Math.Exp(-eta)), Ratings.WinProbability(ra, rb), 12);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void RejectsRatingsOutsideUnitInterval(double bad)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Ratings.WinProbability(bad, 0.5));
        Assert.Throws<ArgumentOutOfRangeException>(() => Ratings.WinProbability(0.5, bad));
        Assert.Throws<ArgumentOutOfRangeException>(() => Ratings.Update(bad, 0.5, aWon: true));
        Assert.Throws<ArgumentOutOfRangeException>(() => Ratings.Update(0.5, bad, aWon: true));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void RejectsNegativeOrNonFiniteLearningRate(double bad)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Ratings.Update(0.5, 0.5, aWon: true, learningRate: bad));
    }

    [Fact]
    public void OnlineUpdatesConvergeToGlobalBradleyTerryMle()
    {
        // Deterministic three-player round robin generated from known strengths.
        double[] strengths = { 1d, 2d, 4d };
        const int gamesPerPair = 200;
        var random = new Random(12345);
        var games = new List<(int A, int B, bool AWon)>();

        for (int a = 0; a < strengths.Length; a++)
        {
            for (int b = a + 1; b < strengths.Length; b++)
            {
                double p = strengths[a] / (strengths[a] + strengths[b]);
                for (int k = 0; k < gamesPerPair; k++)
                {
                    games.Add((a, b, random.NextDouble() < p));
                }
            }
        }

        double[] mle = MaximumLikelihoodRatings(strengths.Length, games);

        // Decentralized online updates with a decaying learning rate.
        var ratings = new double[strengths.Length];
        Array.Fill(ratings, Ratings.InitialRating);
        int step = 0;
        for (int epoch = 0; epoch < 200; epoch++)
        {
            foreach (var game in games)
            {
                double eta = 5d / (step + 5);
                var (ra, rb) = Ratings.Update(ratings[game.A], ratings[game.B], game.AWon, eta);
                ratings[game.A] = ra;
                ratings[game.B] = rb;
                step++;
            }
        }

        for (int a = 0; a < strengths.Length; a++)
        {
            for (int b = a + 1; b < strengths.Length; b++)
            {
                double got = Ratings.WinProbability(ratings[a], ratings[b]);
                double want = Ratings.WinProbability(mle[a], mle[b]);
                Assert.True(
                    Math.Abs(got - want) < 0.005,
                    $"P({a} beats {b}): online {got:F5}, MLE {want:F5}");
            }
        }
    }

    /// <summary>
    /// Reference global Bradley–Terry MLE via the standard minorization–maximization
    /// iteration, normalized so the geometric mean strength is 1 (sum of log-odds 0).
    /// </summary>
    private static double[] MaximumLikelihoodRatings(
        int playerCount,
        IReadOnlyList<(int A, int B, bool AWon)> games)
    {
        var wins = new int[playerCount];
        var played = new int[playerCount, playerCount];

        foreach (var game in games)
        {
            played[game.A, game.B]++;
            played[game.B, game.A]++;
            if (game.AWon)
            {
                wins[game.A]++;
            }
            else
            {
                wins[game.B]++;
            }
        }

        var strength = new double[playerCount];
        Array.Fill(strength, 1d);

        for (int iteration = 0; iteration < 5000; iteration++)
        {
            var next = (double[])strength.Clone();
            for (int i = 0; i < playerCount; i++)
            {
                double denominator = 0d;
                for (int j = 0; j < playerCount; j++)
                {
                    if (j != i)
                    {
                        denominator += played[i, j] / (strength[i] + strength[j]);
                    }
                }

                if (wins[i] == 0 || denominator == 0d)
                {
                    throw new InvalidOperationException(
                        "MLE reference requires at least one win per player.");
                }

                next[i] = wins[i] / denominator;
            }

            double logMean = 0d;
            for (int i = 0; i < playerCount; i++)
            {
                logMean += Math.Log(next[i]);
            }

            logMean /= playerCount;
            for (int i = 0; i < playerCount; i++)
            {
                strength[i] = Math.Exp(Math.Log(next[i]) - logMean);
            }
        }

        var ratings = new double[playerCount];
        for (int i = 0; i < playerCount; i++)
        {
            ratings[i] = strength[i] / (1d + strength[i]);
        }

        return ratings;
    }

    private static double Logit(double rating) => Math.Log(rating) - Math.Log(1d - rating);
}
