namespace BlackjackStreakSimulator.Engine;

public class Seat
{
    public decimal Bankroll { get; private set; }
    public int StreakCount { get; private set; }
    public List<Hand> Hands;
    public IBettingStrategy BettingStrategy;
    private readonly decimal baseBet;

    public Seat(decimal initialBankroll, decimal baseBet, IBettingStrategy bettingStrategy)
    {

        Hands = new List<Hand>();
        Bankroll = initialBankroll;
        StreakCount = 0;
        this.baseBet = baseBet;
        BettingStrategy = bettingStrategy;
    }

    public void NewRound()
    {
        var (bet, streakComplete) = BettingStrategy.GetNextBet(baseBet, StreakCount);
        if (streakComplete)
        {
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
        // Using profit rather than win/loss because a seat might hold more than one hand after a split. 
        if (netProfit > 0)
        {
            StreakCount++;
        }
        else if (netProfit < 0)
        {
            StreakCount = 0;
        }
        // else push does not effect streakcount
    }


}
