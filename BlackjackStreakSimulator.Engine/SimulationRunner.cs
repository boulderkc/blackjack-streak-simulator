namespace BlackjackStreakSimulator.Engine;

// No state of its own — runs one full simulation start to finish from a
// given config and returns a result. Same "stateless orchestrator" shape as
// DealerPlay/BasicStrategy/RoundEngine.
public static class SimulationRunner
{
    // Drones exist only to consume cards realistically at the table — their
    // own outcome is never measured, so they get no configuration surface.
    // A fixed low bet against a bankroll scaled well past the tracked seat's
    // own goal keeps them out of bust territory for any goal a real user
    // would set, without needing per-round refresh logic.
    private const decimal DroneBaseBet = 5m;

    public static SimulationResult RunSimulation(SimulationConfig config)
    {
        Shoe shoe = new Shoe(config.DecksInShoe);
        List<Seat> seats = BuildSeats(config);
        Seat trackedSeat = seats[0];
        int handsPlayed = 0;

        while (trackedSeat.Bankroll > 0 && trackedSeat.Bankroll < config.BankrollGoal)
        {
            if (shoe.NeedsReshuffle)
            {
                shoe = new Shoe(config.DecksInShoe);
            }

            RoundEngine.PlayRound(new Hand(), shoe, seats);
            handsPlayed++;
        }

        return new SimulationResult
        {
            ReachedGoal = trackedSeat.Bankroll >= config.BankrollGoal,
            HandsPlayed = handsPlayed,
            FinalBankroll = trackedSeat.Bankroll
        };
    }

    private static List<Seat> BuildSeats(SimulationConfig config)
    {
        decimal droneBankroll = Math.Max(1_000_000m, config.BankrollGoal * 100m);

        IBettingStrategy trackedStrategy = config.BettingMode == BettingMode.Streak
            ? new StreakBettingStrategy(config.MaxStreakCount)
            : new FlatBettingStrategy();

        List<Seat> seats = new List<Seat>
        {
            // seats[0] is the tracked player - the only one whose outcome
            // this run actually measures.
            new Seat(config.InitialBankroll, config.BaseBet, trackedStrategy)
        };

        for (int s = 1; s < config.SeatCount; s++)
        {
            seats.Add(new Seat(droneBankroll, DroneBaseBet, new FlatBettingStrategy()));
        }

        return seats;
    }
}
