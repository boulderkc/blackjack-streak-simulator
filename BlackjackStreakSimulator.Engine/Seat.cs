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
    // has concluded so far (either broken by a loss, or hitting the
    // configured max and auto-resetting). Flat mode never populates this -
    // streakComplete is never true and StreakCount never leaves 0.
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

        // Using profit rather than win/loss because a seat might hold more than one hand after a split.
        if (netProfit > 0)
        {
            StreakCount++;
        }
        else if (netProfit < 0)
        {
            // Only a real streak (length 1+) counts as "concluded" - a loss
            // while already at 0 isn't breaking anything.
            if (StreakCount > 0)
            {
                RecordStreakConclusion(StreakCount);
            }
            StreakCount = 0;
        }
        // else push does not effect streakcount
    }

    private void RecordStreakConclusion(int length)
    {
        StreakLengthFrequency[length] = StreakLengthFrequency.GetValueOrDefault(length) + 1;
    }
}
