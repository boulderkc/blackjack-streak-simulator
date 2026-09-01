namespace BlackjackStreakSimulator.Engine.Tests;

public class SeatTests
{
    // Dealer always stands on a plain 17 - not busted, not blackjack, so
    // every player hand's outcome is driven purely by its own value.
    private static Hand CreateDealerHand()
        => CreateHand(0, CardRank.Ten, CardRank.Seven);

    private static Hand CreateWinningHand(decimal bet)
        => CreateHand(bet, CardRank.Ten, CardRank.Nine); // 19 beats dealer's 17

    private static Hand CreateLosingHand(decimal bet)
        => CreateHand(bet, CardRank.Ten, CardRank.Nine, CardRank.Five); // busts (24)

    private static Hand CreatePushHand(decimal bet)
        => CreateHand(bet, CardRank.Ten, CardRank.Seven); // ties dealer's 17

    private static Hand CreateHand(decimal bet, params CardRank[] ranks)
    {
        Hand hand = new Hand { Bet = bet };
        foreach (CardRank rank in ranks)
        {
            hand.Cards.Add(new Card { Rank = rank, Suit = CardSuit.Clubs });
        }
        return hand;
    }

    private static Seat CreateSeat(decimal initialBankroll = 1000m, IBettingStrategy? strategy = null)
        => new Seat(initialBankroll, baseBet: 10m, strategy ?? new FlatBettingStrategy());

    [Fact]
    public void ApplyRoundResult_Win_IncrementsStreakCountAndDoesNotTally()
    {
        Seat seat = CreateSeat();
        seat.Hands = new List<Hand> { CreateWinningHand(10m) };

        seat.ApplyRoundResult(CreateDealerHand());

        Assert.Equal(1, seat.StreakCount);
        Assert.Empty(seat.StreakLengthFrequency);
    }

    [Fact]
    public void ApplyRoundResult_LossAfterStreak_TalliesFrequencyAndResets()
    {
        Seat seat = CreateSeat();
        Hand dealerHand = CreateDealerHand();

        // Build a streak of 3 wins, then break it with a loss.
        seat.Hands = new List<Hand> { CreateWinningHand(10m) };
        seat.ApplyRoundResult(dealerHand);
        seat.Hands = new List<Hand> { CreateWinningHand(10m) };
        seat.ApplyRoundResult(dealerHand);
        seat.Hands = new List<Hand> { CreateWinningHand(10m) };
        seat.ApplyRoundResult(dealerHand);

        seat.Hands = new List<Hand> { CreateLosingHand(10m) };
        seat.ApplyRoundResult(dealerHand);

        Assert.Equal(0, seat.StreakCount);
        Assert.Equal(1, seat.StreakLengthFrequency[3]);
    }

    [Fact]
    public void ApplyRoundResult_LossAtZeroStreak_TalliesNothing()
    {
        // A loss that isn't breaking an active streak shouldn't be recorded
        // as a "concluded streak" of length 0 - there's nothing to tally.
        Seat seat = CreateSeat();
        seat.Hands = new List<Hand> { CreateLosingHand(10m) };

        seat.ApplyRoundResult(CreateDealerHand());

        Assert.Equal(0, seat.StreakCount);
        Assert.Empty(seat.StreakLengthFrequency);
    }

    [Fact]
    public void ApplyRoundResult_Push_LeavesStreakCountAndFrequencyUnchanged()
    {
        Seat seat = CreateSeat();
        Hand dealerHand = CreateDealerHand();

        seat.Hands = new List<Hand> { CreateWinningHand(10m) };
        seat.ApplyRoundResult(dealerHand);
        seat.Hands = new List<Hand> { CreateWinningHand(10m) };
        seat.ApplyRoundResult(dealerHand);

        seat.Hands = new List<Hand> { CreatePushHand(10m) };
        seat.ApplyRoundResult(dealerHand);

        Assert.Equal(2, seat.StreakCount);
        Assert.Empty(seat.StreakLengthFrequency);
    }

    [Fact]
    public void NewRound_ReachingMaxStreakLength_TalliesFrequencyAndResets()
    {
        // The classic Paroli "cash out" - hitting the configured max streak
        // length is itself a streak conclusion, tallied the same as a
        // loss-triggered one, just not caused by a loss.
        Seat seat = CreateSeat(strategy: new StreakBettingStrategy(maxStreakLength: 2));
        Hand dealerHand = CreateDealerHand();

        seat.Hands = new List<Hand> { CreateWinningHand(10m) };
        seat.ApplyRoundResult(dealerHand);
        seat.Hands = new List<Hand> { CreateWinningHand(10m) };
        seat.ApplyRoundResult(dealerHand);
        Assert.Equal(2, seat.StreakCount); // now at the configured max

        seat.NewRound();

        Assert.Equal(0, seat.StreakCount);
        Assert.Equal(1, seat.StreakLengthFrequency[2]);
    }

    [Fact]
    public void ApplyRoundResult_OnlyWinning_MaxDrawdownStaysZero()
    {
        Seat seat = CreateSeat();
        Hand dealerHand = CreateDealerHand();

        seat.Hands = new List<Hand> { CreateWinningHand(10m) };
        seat.ApplyRoundResult(dealerHand);
        seat.Hands = new List<Hand> { CreateWinningHand(10m) };
        seat.ApplyRoundResult(dealerHand);

        Assert.Equal(0m, seat.MaxDrawdown);
    }

    [Fact]
    public void ApplyRoundResult_TracksWorstPeakToTroughDrop_NotJustFinalVsInitial()
    {
        // 1000 -> 1200 (peak) -> 1100 (100 drawdown so far) -> 1300 (new
        // peak) -> 1000 (300 drawdown - worse than the earlier 100, and
        // bigger than a naive "initial vs. final" or "peak vs. final"
        // comparison would suggest on its own).
        Seat seat = CreateSeat(initialBankroll: 1000m);
        Hand dealerHand = CreateDealerHand();

        seat.Hands = new List<Hand> { CreateWinningHand(200m) };
        seat.ApplyRoundResult(dealerHand); // 1200
        seat.Hands = new List<Hand> { CreateLosingHand(100m) };
        seat.ApplyRoundResult(dealerHand); // 1100, drawdown 100
        seat.Hands = new List<Hand> { CreateWinningHand(200m) };
        seat.ApplyRoundResult(dealerHand); // 1300, new peak
        seat.Hands = new List<Hand> { CreateLosingHand(300m) };
        seat.ApplyRoundResult(dealerHand); // 1000, drawdown 300

        Assert.Equal(1000m, seat.Bankroll);
        Assert.Equal(300m, seat.MaxDrawdown);
    }

    [Fact]
    public void ApplyRoundResult_OnlyWinning_LowestBankrollStaysAtInitial()
    {
        // A run that never dips below where it started should report its
        // own starting bankroll as the lowest point, not something lower
        // that never actually happened.
        Seat seat = CreateSeat(initialBankroll: 1000m);
        Hand dealerHand = CreateDealerHand();

        seat.Hands = new List<Hand> { CreateWinningHand(200m) };
        seat.ApplyRoundResult(dealerHand);

        Assert.Equal(1000m, seat.LowestBankroll);
    }

    [Fact]
    public void ApplyRoundResult_TracksWorstTrough_NotJustFinalValue()
    {
        // 1000 -> 700 (trough so far) -> 1200 (recovered well past start) -
        // LowestBankroll should still remember the 700 low point even
        // though the run ends up much higher than where it started.
        Seat seat = CreateSeat(initialBankroll: 1000m);
        Hand dealerHand = CreateDealerHand();

        seat.Hands = new List<Hand> { CreateLosingHand(300m) };
        seat.ApplyRoundResult(dealerHand); // 700
        seat.Hands = new List<Hand> { CreateWinningHand(500m) };
        seat.ApplyRoundResult(dealerHand); // 1200

        Assert.Equal(1200m, seat.Bankroll);
        Assert.Equal(700m, seat.LowestBankroll);
    }
}
