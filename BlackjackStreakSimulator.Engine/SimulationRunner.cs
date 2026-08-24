namespace BlackjackStreakSimulator.Engine;

// No state of its own — runs one full simulation start to finish from a
// given config and returns a result. Same "stateless orchestrator" shape as
// DealerPlay/BasicStrategy/RoundEngine.
public static class SimulationRunner
{
    public static SimulationResult RunSimulation(SimulationConfig config)
    {
        Shoe shoe = new Shoe(config.DecksInShoe);
        List<Seat> seats = SimulationLoop.BuildSeats(config);
        Seat trackedSeat = seats[0];
        int handsPlayed = 0;

        bool finished;
        do
        {
            finished = SimulationLoop.PlayOneRound(trackedSeat, config, ref shoe, seats, ref handsPlayed, out _);
        } while (!finished);

        return SimulationLoop.BuildResult(trackedSeat, config, handsPlayed);
    }
}
