namespace BlackjackStreakSimulator.Engine;

// The engine-provided session object a Blazor Scoped/per-circuit service
// wraps for manual step-through mode (see docs/DECISIONS.md). Unlike
// everything else in the engine, this genuinely needs to be instanced: it
// holds a Shoe/Seats/hands-played that persist across many separate calls —
// one per "Next Round" click — rather than living and dying within one
// method call the way SimulationRunner's local variables do.
public class SingleRoundRunner
{
    private readonly SimulationConfig config;
    private readonly Seat trackedSeat;
    private Shoe shoe;
    private int handsPlayed;

    public List<Seat> Seats { get; }
    public int HandsPlayed => handsPlayed;
    public Hand? LastDealerHand { get; private set; }
    public bool IsFinished { get; private set; }
    public SimulationResult? Result { get; private set; }

    public SingleRoundRunner(SimulationConfig config)
    {
        this.config = config;
        shoe = new Shoe(config.DecksInShoe);
        Seats = SimulationLoop.BuildSeats(config);
        trackedSeat = Seats[0];
    }

    public void PlayNextRound()
    {
        if (IsFinished)
        {
            return;
        }

        IsFinished = SimulationLoop.PlayOneRound(trackedSeat, config, ref shoe, Seats, ref handsPlayed, out Hand dealerHand);
        LastDealerHand = dealerHand;

        if (IsFinished)
        {
            Result = SimulationLoop.BuildResult(trackedSeat, config, handsPlayed);
        }
    }

    // "Finish Simulation Automatically" — just PlayNextRound in a loop,
    // starting from wherever this session currently stands.
    public void FinishAutomatically()
    {
        while (!IsFinished)
        {
            PlayNextRound();
        }
    }
}
