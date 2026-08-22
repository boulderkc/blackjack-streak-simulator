namespace BlackjackStreakSimulator.Engine.Tests;

public class RoundEngineTests
{
    private static List<Seat> CreateSeats()
    {
        return new List<Seat>
        {
            new Seat(initialBankroll: 1000, baseBet: 10, new StreakBettingStrategy(maxStreakLength: 4)),
            new Seat(initialBankroll: 1000, baseBet: 10, new FlatBettingStrategy()),
            new Seat(initialBankroll: 1000, baseBet: 10, new FlatBettingStrategy()),
            new Seat(initialBankroll: 1000, baseBet: 10, new FlatBettingStrategy()),
            new Seat(initialBankroll: 1000, baseBet: 10, new FlatBettingStrategy())
        };
    }

    [Fact]
    public void PlayRound_ManyRounds_NeverThrows()
    {
        // Small deck deliberately stresses Shoe's self-healing rebuild across
        // many rounds of hitting/splitting, same as ShoeTests does directly.
        Shoe shoe = new Shoe(deckCount: 1);
        List<Seat> seats = CreateSeats();

        Exception? exception = Record.Exception(() =>
        {
            for (int i = 0; i < 500; i++)
            {
                RoundEngine.PlayRound(new Hand(), shoe, seats);
            }
        });

        Assert.Null(exception);
    }

    [Fact]
    public void PlayRound_ManyRounds_EverySeatEndsWithBetweenOneAndFourHands()
    {
        Shoe shoe = new Shoe(deckCount: 2);
        List<Seat> seats = CreateSeats();

        for (int i = 0; i < 300; i++)
        {
            RoundEngine.PlayRound(new Hand(), shoe, seats);

            foreach (Seat seat in seats)
            {
                Assert.InRange(seat.Hands.Count, 1, 4);
            }
        }
    }

    [Fact]
    public void PlayRound_ManyRounds_MultiHandSeatsAreAllFlaggedFromSplit()
    {
        Shoe shoe = new Shoe(deckCount: 2);
        List<Seat> seats = CreateSeats();

        for (int i = 0; i < 300; i++)
        {
            RoundEngine.PlayRound(new Hand(), shoe, seats);

            foreach (Seat seat in seats)
            {
                if (seat.Hands.Count > 1)
                {
                    Assert.All(seat.Hands, hand => Assert.True(hand.IsFromSplit));
                }
            }
        }
    }

    [Fact]
    public void PlayRound_ManyRounds_DealerBlackjackMeansNoSeatEverActed()
    {
        // If the dealer peeks and finds blackjack, every seat's turn is
        // skipped entirely - each seat should still be holding exactly its
        // original one two-card hand.
        Shoe shoe = new Shoe(deckCount: 2);
        List<Seat> seats = CreateSeats();
        bool observedDealerBlackjack = false;

        for (int i = 0; i < 500; i++)
        {
            Hand dealerHand = new Hand();
            RoundEngine.PlayRound(dealerHand, shoe, seats);

            if (dealerHand.IsBlackjack)
            {
                observedDealerBlackjack = true;
                foreach (Seat seat in seats)
                {
                    Assert.Single(seat.Hands);
                    Assert.Equal(2, seat.Hands[0].Cards.Count);
                }
            }
        }

        // Not a hard requirement of correctness, but with 500 rounds this
        // should show up - if it never does, the peek path isn't actually
        // being exercised by this test at all.
        Assert.True(observedDealerBlackjack);
    }

    [Fact]
    public void PlayRound_ManyRounds_DealerHandAlwaysEndsInAValidStoppingState()
    {
        Shoe shoe = new Shoe(deckCount: 2);
        List<Seat> seats = CreateSeats();

        for (int i = 0; i < 300; i++)
        {
            Hand dealerHand = new Hand();
            RoundEngine.PlayRound(dealerHand, shoe, seats);

            Assert.True(dealerHand.IsBusted || dealerHand.Value >= 17);
            Assert.False(dealerHand.Value == 17 && dealerHand.IsSoft);
        }
    }
}
