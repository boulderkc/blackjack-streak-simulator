namespace BlackjackStreakSimulator.Web.Components.Pages;

using BlackjackStreakSimulator.Engine;

public partial class ManualSimulation
{
    public SingleRoundRunner currentRun;
    public Card TestCard()
    {
        Card testCard = new Card();
        testCard.Rank = CardRank.Eight;
        testCard.Suit = CardSuit.Clubs;
        return testCard;
    }

    public Card TestCard2()
    {
        Card testCard = new Card();
        testCard.Rank = CardRank.King;
        testCard.Suit = CardSuit.Hearts;
        return testCard;
    }

    public void RunTest()
    {
        SimulationConfig config = new SimulationConfig(); // default constructor values
        currentRun = new SingleRoundRunner(config);

        currentRun.PlayNextRound();
    }

    public void NextRound()
    {
        currentRun.PlayNextRound();
    }

    // Read-only re-derivation of a hand's outcome for display - safe to call
    // repeatedly, same reasoning as calling SettleProfit again in the
    // ScratchWatchRounds script: it doesn't mutate anything.
    public string GetResultText(Hand hand)
    {
        if (currentRun?.LastDealerHand is null)
        {
            return string.Empty;
        }

        if (hand.IsBusted)
        {
            return "Bust";
        }

        if (hand.IsBlackjack)
        {
            return "Blackjack!";
        }

        decimal profit = hand.SettleProfit(currentRun.LastDealerHand);

        if (profit > 0)
        {
            return $"Win ({profit:C})";
        }

        if (profit < 0)
        {
            return $"Lose {profit:C}";
        }

        return "Push";
    }
}
