namespace BlackjackStreakSimulator.Engine;

public class Seat
{
    public decimal Bankroll { get; private set; }
    public int StreakCount { get; private set; }
    public List<Hand> Hands;
    public IBettingStrategy BettingStrategy;
    private readonly decimal baseBet;

    // Largest peak-to-trough decline in Bankroll seen so far, and the
    // running peak used to compute it - see MaxDrawdown.
    private decimal peakBankroll;
    public decimal MaxDrawdown { get; private set; }

    // Lowest Bankroll has ever been, including the starting value - a run
    // that never dipped below where it started reports its own initial
    // bankroll here, not some lower "worst case" that never happened.
    public decimal LowestBankroll { get; private set; }

    // Key = streak length, value = how many times a streak of that length
    // has concluded so far - broken by a loss (0 when there was no active
    // streak to break), or hit the configured max and auto-reset. This
    // tracks every loss, so it's a complete account of streak-ending
    // hands, not just ones that broke an active streak. Stays empty for
    // flat-mode seats - see ApplyRoundResult - since the bet never changes
    // regardless of streak length, this data can't describe anything
    // flat betting actually does.
    public Dictionary<int, int> StreakLengthFrequency { get; } = [];

    public Seat(decimal initialBankroll, decimal baseBet, IBettingStrategy bettingStrategy)
    {

        Hands = new List<Hand>();
        Bankroll = initialBankroll;
        peakBankroll = initialBankroll;
        LowestBankroll = initialBankroll;
        StreakCount = 0;
        this.baseBet = baseBet;
        BettingStrategy = bettingStrategy;
    }

    // Pure preview of what NewRound() would bet next, without triggering any
    // of its side effects (streak-conclusion recording, StreakCount reset) -
    // GetNextBet itself is stateless, so this is safe to call speculatively
    // before deciding whether the round can even be played at all.
    public bool CanAffordNextBet()
    {
        (decimal bet, _) = BettingStrategy.GetNextBet(baseBet, StreakCount);
        return bet <= Bankroll;
    }

    public void NewRound()
    {
        var (bet, streakComplete) = BettingStrategy.GetNextBet(baseBet, StreakCount);
        if (streakComplete)
        {
            // Hitting the configured max is itself a streak conclusion (the
            // classic Paroli "cash out") - tally it before resetting.
            RecordStreakConclusion(StreakCount);
            StreakCount = 0;
        }
        Hands = new List<Hand> { new Hand { Bet = bet } };
    }

    public void ApplyRoundResult(Hand dealerHand)
    {
        decimal netProfit = 0;
        foreach (Hand hand in Hands)
        {
            netProfit += hand.SettleProfit(dealerHand);
        }

        Bankroll += netProfit;

        if (Bankroll > peakBankroll)
        {
            peakBankroll = Bankroll;
        }
        MaxDrawdown = Math.Max(MaxDrawdown, peakBankroll - Bankroll);

        LowestBankroll = Math.Min(LowestBankroll, Bankroll);

        // Streak tracking is meaningless for flat betting - the bet never
        // changes regardless of streak length, so there's nothing for this
        // data to describe. Skipped here at the source, rather than
        // filtered out downstream, so a flat-mode seat never accumulates
        // the long tail of near-empty buckets an uncapped winning streak
        // can produce (nothing resets it early the way hitting a
        // configured max would in streak mode).
        if (BettingStrategy is not FlatBettingStrategy)
        {
            // Using profit rather than win/loss because a seat might hold more than one hand after a split.
            if (netProfit > 0)
            {
                StreakCount++;
            }
            else if (netProfit < 0)
            {
                // Tally every loss, keyed by the streak length it broke - 0
                // when there was no active streak to break at all. This makes
                // the table a complete account of every loss, not just ones
                // that ended an active streak.
                RecordStreakConclusion(StreakCount);
                StreakCount = 0;
            }
            // else push does not effect streakcount
        }
    }

    private void RecordStreakConclusion(int length)
    {
        StreakLengthFrequency[length] = StreakLengthFrequency.GetValueOrDefault(length) + 1;
    }
}
