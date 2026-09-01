namespace BlackjackStreakSimulator.Engine.Tests;

public class SimulationLoopTests
{
    // ---- BuildSeats ----

    [Fact]
    public void BuildSeats_ReturnsConfiguredSeatCount()
    {
        SimulationConfig config = new SimulationConfig { SeatCount = 3 };

        List<Seat> seats = SimulationLoop.BuildSeats(config);

        Assert.Equal(3, seats.Count);
    }

    [Fact]
    public void BuildSeats_TrackedSeatUsesStreakStrategy_WhenConfigured()
    {
        SimulationConfig config = new SimulationConfig { BettingMode = BettingMode.Streak };

        List<Seat> seats = SimulationLoop.BuildSeats(config);

        Assert.IsType<StreakBettingStrategy>(seats[0].BettingStrategy);
    }

    [Fact]
    public void BuildSeats_TrackedSeatUsesFlatStrategy_WhenConfigured()
    {
        SimulationConfig config = new SimulationConfig { BettingMode = BettingMode.Flat };

        List<Seat> seats = SimulationLoop.BuildSeats(config);

        Assert.IsType<FlatBettingStrategy>(seats[0].BettingStrategy);
    }

    [Fact]
    public void BuildSeats_DroneSeatsAlwaysUseFlatStrategy()
    {
        SimulationConfig config = new SimulationConfig { SeatCount = 5, BettingMode = BettingMode.Streak };

        List<Seat> seats = SimulationLoop.BuildSeats(config);

        for (int i = 1; i < seats.Count; i++)
        {
            Assert.IsType<FlatBettingStrategy>(seats[i].BettingStrategy);
        }
    }

    [Fact]
    public void BuildSeats_DroneBankroll_UsesOneMillionFloorForSmallGoals()
    {
        SimulationConfig config = new SimulationConfig { SeatCount = 2, BankrollGoal = 1000m };

        List<Seat> seats = SimulationLoop.BuildSeats(config);

        Assert.Equal(1_000_000m, seats[1].Bankroll);
    }

    [Fact]
    public void BuildSeats_DroneBankroll_ScalesWithGoal_WhenGoalIsLarge()
    {
        SimulationConfig config = new SimulationConfig { SeatCount = 2, BankrollGoal = 50_000m };

        List<Seat> seats = SimulationLoop.BuildSeats(config);

        Assert.Equal(5_000_000m, seats[1].Bankroll); // goal * 100, above the 1,000,000 floor
    }

    [Fact]
    public void BuildSeats_TrackedSeatUsesConfiguredBankrollAndBaseBet()
    {
        SimulationConfig config = new SimulationConfig { InitialBankroll = 777m, BaseBet = 33m };

        List<Seat> seats = SimulationLoop.BuildSeats(config);
        seats[0].NewRound(); // fresh seat, streak 0 -> bet should be exactly BaseBet

        Assert.Equal(777m, seats[0].Bankroll);
        Assert.Equal(33m, seats[0].Hands[0].Bet);
    }

    // ---- PlayOneRound ----

    [Fact]
    public void PlayOneRound_IncrementsHandsPlayedByOne()
    {
        SimulationConfig config = new SimulationConfig();
        Shoe shoe = new Shoe(config.DecksInShoe);
        Seat trackedSeat = new Seat(1000m, 10m, new FlatBettingStrategy());
        List<Seat> seats = new List<Seat> { trackedSeat };
        int handsPlayed = 5;

        SimulationLoop.PlayOneRound(trackedSeat, config, ref shoe, seats, ref handsPlayed, out _);

        Assert.Equal(6, handsPlayed);
    }

    [Fact]
    public void PlayOneRound_DealsAtLeastTwoCardsToTheDealer()
    {
        SimulationConfig config = new SimulationConfig();
        Shoe shoe = new Shoe(config.DecksInShoe);
        Seat trackedSeat = new Seat(1000m, 10m, new FlatBettingStrategy());
        List<Seat> seats = new List<Seat> { trackedSeat };
        int handsPlayed = 0;

        SimulationLoop.PlayOneRound(trackedSeat, config, ref shoe, seats, ref handsPlayed, out Hand dealerHand);

        Assert.True(dealerHand.Cards.Count >= 2);
    }

    [Fact]
    public void PlayOneRound_BankrollAlreadyPastGoal_ReturnsTrue()
    {
        // A goal this low relative to the bankroll is reached before the
        // round even starts, regardless of that round's outcome.
        SimulationConfig config = new SimulationConfig { BankrollGoal = 1m };
        Shoe shoe = new Shoe(config.DecksInShoe);
        Seat trackedSeat = new Seat(5000m, 10m, new FlatBettingStrategy());
        List<Seat> seats = new List<Seat> { trackedSeat };
        int handsPlayed = 0;

        bool finished = SimulationLoop.PlayOneRound(trackedSeat, config, ref shoe, seats, ref handsPlayed, out _);

        Assert.True(finished);
    }

    [Fact]
    public void PlayOneRound_HealthyBankrollFarFromGoal_ReturnsFalse()
    {
        SimulationConfig config = new SimulationConfig { BankrollGoal = 1_000_000m };
        Shoe shoe = new Shoe(config.DecksInShoe);
        Seat trackedSeat = new Seat(1000m, 10m, new FlatBettingStrategy());
        List<Seat> seats = new List<Seat> { trackedSeat };
        int handsPlayed = 0;

        bool finished = SimulationLoop.PlayOneRound(trackedSeat, config, ref shoe, seats, ref handsPlayed, out _);

        Assert.False(finished);
    }

    [Fact]
    public void PlayOneRound_ReshufflesWhenShoeNeedsIt()
    {
        SimulationConfig config = new SimulationConfig { DecksInShoe = 1, BankrollGoal = 1_000_000m };
        Shoe shoe = new Shoe(config.DecksInShoe);
        for (int i = 0; i < 40; i++) // 75% of a 1-deck (52 card) shoe is 39
        {
            shoe.DrawCard();
        }
        Shoe shoeBeforeRound = shoe;

        Seat trackedSeat = new Seat(1000m, 10m, new FlatBettingStrategy());
        List<Seat> seats = new List<Seat> { trackedSeat };
        int handsPlayed = 0;

        SimulationLoop.PlayOneRound(trackedSeat, config, ref shoe, seats, ref handsPlayed, out _);

        // `shoe` should now point at a different object - the reshuffle
        // replaced it, which only works because the parameter is `ref`.
        Assert.False(ReferenceEquals(shoeBeforeRound, shoe));
    }

    // ---- BuildResult ----

    [Fact]
    public void BuildResult_BankrollAtOrAboveGoal_ReachedGoalIsTrue()
    {
        Seat trackedSeat = new Seat(5000m, 10m, new FlatBettingStrategy());
        SimulationConfig config = new SimulationConfig { BankrollGoal = 5000m };

        SimulationResult result = SimulationLoop.BuildResult(trackedSeat, config, handsPlayed: 42);

        Assert.True(result.ReachedGoal);
        Assert.Equal(42, result.HandsPlayed);
        Assert.Equal(5000m, result.FinalBankroll);
    }

    [Fact]
    public void BuildResult_BankrollBelowGoal_ReachedGoalIsFalse()
    {
        Seat trackedSeat = new Seat(0m, 10m, new FlatBettingStrategy());
        SimulationConfig config = new SimulationConfig { BankrollGoal = 5000m };

        SimulationResult result = SimulationLoop.BuildResult(trackedSeat, config, handsPlayed: 7);

        Assert.False(result.ReachedGoal);
    }

    // ---- GetLowestBankrollBucket ----

    [Theory]
    [InlineData(1000, 1000, 90)]  // never dropped below the start (100%) - top bucket, no 11th special case
    [InlineData(950, 1000, 90)]   // 95% - top bucket
    [InlineData(900, 1000, 90)]   // exactly on the 90 boundary
    [InlineData(730, 1000, 70)]   // 73% - floors down to its bucket, not rounds
    [InlineData(500, 1000, 50)]   // exactly on the 50 boundary
    [InlineData(100, 1000, 10)]   // 10% - exactly on the 10 boundary
    [InlineData(50, 1000, 0)]     // barely survived - bottom bucket
    [InlineData(0, 1000, 0)]      // as low as a reached-goal run's trough can get without busting
    public void GetLowestBankrollBucket_ReturnsExpectedBucketFloor(decimal lowestBankroll, decimal initialBankroll, int expectedBucket)
    {
        int bucket = SimulationLoop.GetLowestBankrollBucket(lowestBankroll, initialBankroll);

        Assert.Equal(expectedBucket, bucket);
    }

    [Fact]
    public void GetLowestBankrollBucket_AboveInitialBankroll_ClampsToTopBucket()
    {
        // Shouldn't happen in practice (LowestBankroll can never exceed
        // InitialBankroll), but the clamp should stay safe if it ever does.
        int bucket = SimulationLoop.GetLowestBankrollBucket(1500m, 1000m);

        Assert.Equal(90, bucket);
    }

    [Fact]
    public void GetLowestBankrollBucket_Negative_ClampsToBottomBucket()
    {
        int bucket = SimulationLoop.GetLowestBankrollBucket(-50m, 1000m);

        Assert.Equal(0, bucket);
    }
}
