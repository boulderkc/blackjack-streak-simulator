namespace BlackjackStreakSimulator.Engine;

public class SimulationConfig
{
    public decimal InitialBankroll {get; set;}
    public decimal BaseBet {get; set;}
    public int DecksInShoe {get; set;}
    public int MaxStreakCount {get; set;}
    public int SeatCount {get; set;}
    public decimal BankrollGoal {get; set;}
    public BettingMode BettingMode {get; set;}

    public SimulationConfig()
    {
        InitialBankroll = 1000m;
        BankrollGoal = 5000m;
        BaseBet = 10m;
        DecksInShoe = 6;
        MaxStreakCount = 5;
        SeatCount = 5;
        BettingMode = BettingMode.Streak;
    }
}


