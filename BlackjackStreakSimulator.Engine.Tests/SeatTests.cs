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
    public void ApplyRoundResult_LossAtZeroStreak_TalliesZeroLengthBucket()
    {
        // A loss that isn't breaking an active streak still gets recorded,
        // under bucket 0 - "the streak length was 0 when this loss
        // happened" - so the table accounts for every loss, not just ones
        // that broke an active streak.
        Seat seat = CreateSeat();
        seat.Hands = new List<Hand> { CreateLosingHand(10m) };

        seat.ApplyRoundResult(CreateDealerHand());

        Assert.Equal(0, seat.StreakCount);
        Assert.Equal(1, seat.StreakLengthFrequency[0]);
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
    public void CanAffordNextBet_BankrollCoversBaseBet_ReturnsTrue()
    {
        Seat seat = CreateSeat(initialBankroll: 100m); // base bet is 10m

        Assert.True(seat.CanAffordNextBet());
    }

    [Fact]
    public void CanAffordNextBet_BankrollBelowBaseBet_ReturnsFalse()
    {
        Seat seat = CreateSeat(initialBankroll: 5m); // base bet is 10m

        Assert.False(seat.CanAffordNextBet());
    }

    [Fact]
    public void CanAffordNextBet_BankrollExactlyEqualsNextBet_ReturnsTrue()
    {
        Seat seat = CreateSeat(initialBankroll: 10m); // base bet is 10m - exactly enough

        Assert.True(seat.CanAffordNextBet());
    }

    [Fact]
    public void CanAffordNextBet_StreakDoublesBetBeyondRemainingBankroll_ReturnsFalse()
    {
        // Two wins on a 10 base bet puts StreakCount at 2, so the next bet
        // doubles twice to 40 - more than the 30 the seat actually has, even
        // though it could easily cover a plain 10 base bet. This is the
        // exact scenario that used to drive Bankroll negative.
        Seat seat = CreateSeat(initialBankroll: 10m, strategy: new StreakBettingStrategy(maxStreakLength: 4));
        Hand dealerHand = CreateDealerHand();
        seat.Hands = new List<Hand> { CreateWinningHand(10m) };
        seat.ApplyRoundResult(dealerHand);
        seat.Hands = new List<Hand> { CreateWinningHand(10m) };
        seat.ApplyRoundResult(dealerHand);

        Assert.Equal(30m, seat.Bankroll); // sanity check on the setup itself
        Assert.False(seat.CanAffordNextBet());
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
