namespace BlackjackStreakSimulator.Engine;

// Shared setup and per-round mechanics for SimulationRunner (loops this to
// completion) and SingleRoundRunner (calls it once per click, or loops it
// the same way for "finish automatically"). Never runs a simulation to
// completion itself — it's the pieces both runners are built on top of.
public static class SimulationLoop
{
    // Drones exist only to consume cards realistically at the table — their
    // own outcome is never measured, so they get no configuration surface.
    // A fixed low bet against a bankroll scaled well past the tracked seat's
    // own goal keeps them out of bust territory for any goal a real user
    // would set, without needing per-round refresh logic.
    private const decimal DroneBaseBet = 5m;

    public static List<Seat> BuildSeats(SimulationConfig config)
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

    // Plays one round in place and reports whether the tracked seat's run
    // has now ended (bust or goal reached). `shoe` is `ref` because a
    // reshuffle needs to replace the object itself, not just mutate it.
    // `dealerHand` is `out`, not `ref` - the caller has nothing to hand in
    // (there's no dealer hand yet for this round), the method just creates
    // one and hands it back; SimulationRunner doesn't care what's in it and
    // can discard it, SingleRoundRunner keeps it to show the UI.
    public static bool PlayOneRound(Seat trackedSeat, SimulationConfig config, ref Shoe shoe, List<Seat> seats, ref int handsPlayed, out Hand dealerHand)
    {
        if (shoe.NeedsReshuffle)
        {
            shoe = new Shoe(config.DecksInShoe);
        }

        dealerHand = new Hand();
        RoundEngine.PlayRound(dealerHand, shoe, seats);
        handsPlayed++;

        return trackedSeat.Bankroll <= 0 || trackedSeat.Bankroll >= config.BankrollGoal;
    }

    public static SimulationResult BuildResult(Seat trackedSeat, SimulationConfig config, int handsPlayed)
    {
        return new SimulationResult
        {
            ReachedGoal = trackedSeat.Bankroll >= config.BankrollGoal,
            HandsPlayed = handsPlayed,
            FinalBankroll = trackedSeat.Bankroll,
            StreakLengthFrequency = new Dictionary<int, int>(trackedSeat.StreakLengthFrequency),
            MaxDrawdown = trackedSeat.MaxDrawdown,
            LowestBankroll = trackedSeat.LowestBankroll
        };
    }

    // Which 10-point bucket a run's lowest-bankroll-as-a-percentage-of-
    // initial falls into, for BatchSimulationResult.LowestBankrollBucketFrequency.
    // Buckets are the percentage floor: 0, 10, 20, ... 90 - a run whose
    // lowest point was, say, 73% of its starting bankroll falls in bucket
    // 70. A run that never dipped below its starting bankroll at all (100%)
    // falls in the top bucket (90) alongside anything from 90-100%, rather
    // than needing an eleventh bucket just for that edge case.
    public static int GetLowestBankrollBucket(decimal lowestBankroll, decimal initialBankroll)
    {
        decimal percentage = lowestBankroll / initialBankroll * 100m;
        int bucket = (int)(percentage / 10m) * 10;
        return Math.Clamp(bucket, 0, 90);
    }
}
