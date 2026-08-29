namespace BlackjackStreakSimulator.Web.Components.Pages;

using BlackjackStreakSimulator.Engine;
using BlackjackStreakSimulator.Web.Services;
using Microsoft.AspNetCore.Components;

public partial class ManualSimulation
{
    [Inject] public SimulationConfigState ConfigState { get; set; }
    [Inject] public ManualSimulationState RunState { get; set; }

    public void RunSim()
    {
        RunState.CurrentRun = new SingleRoundRunner(ConfigState.Current);
        RunState.CurrentRun.PlayNextRound();
    }

    public void NextRound()
    {
        RunState.CurrentRun.PlayNextRound();
    }

    // Read-only re-derivation of a hand's outcome for display - safe to call
    // repeatedly, same reasoning as calling SettleProfit again in the
    // ScratchWatchRounds script: it doesn't mutate anything.
    public string GetResultText(Hand hand)
    {
        if (RunState.CurrentRun?.LastDealerHand is null)
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

        decimal profit = hand.SettleProfit(RunState.CurrentRun.LastDealerHand);

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
        if (RunState.CurrentRun?.LastDealerHand is null)
        {
            return string.Empty;
        }

        if (RunState.CurrentRun.LastDealerHand.IsBusted)
        {
            return "Bust";
        }

        if (RunState.CurrentRun.LastDealerHand.IsBlackjack)
        {
            return "Blackjack!";
        }

        return "Stands";
    }

    public decimal GetBankrollDelta()
    {
        if (RunState.CurrentRun?.LastDealerHand is null)
        {
            return 0;
        }

        decimal bankrollDelta = 0;
        foreach (Hand hand in RunState.CurrentRun?.Seats?[0].Hands)
        {
            bankrollDelta += hand.SettleProfit(RunState.CurrentRun?.LastDealerHand);
        }

        return bankrollDelta;
    }

    public void FinishInBatchMode()
    {
        if (RunState.CurrentRun is null)
        {
            return;
        }

        SimulationResult? result = RunState.CurrentRun.FinishAutomatically(); 
        RunState.CurrentRun = null;  
    }
}
