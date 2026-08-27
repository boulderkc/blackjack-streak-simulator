namespace BlackjackStreakSimulator.Web.Components.Pages;

using BlackjackStreakSimulator.Engine;

public partial class ManualSimulation
{
    public SingleRoundRunner currentRun;

    public void RunSim()
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

    // Dealer equivalent of GetResultText - no bet/profit involved, just the
    // hand's own final status.
    public string GetDealerResultText()
    {
        if (currentRun?.LastDealerHand is null)
        {
            return string.Empty;
        }

        if (currentRun.LastDealerHand.IsBusted)
        {
            return "Bust";
        }

        if (currentRun.LastDealerHand.IsBlackjack)
        {
            return "Blackjack!";
        }

        return "Stands";
    }

    public decimal GetBankrollDelta()
    {
        if (currentRun?.LastDealerHand is null)
        {
            return 0;
        }

        decimal bankrollDelta = 0;
        foreach (Hand hand in currentRun?.Seats?[0].Hands)
        {
            bankrollDelta += hand.SettleProfit(currentRun?.LastDealerHand);
        }

        return bankrollDelta;
    }

    public void FinishInBatchMode()
    {
        
    }
}
