namespace BlackjackStreakSimulator.Engine.Tests;

public class BasicStrategyTests
{
    // Suit never matters — only Rank (and therefore Value/IsSoft/CanSplit) does.
    private static Hand CreateHand(params CardRank[] ranks)
    {
        Hand hand = new Hand();
        foreach (CardRank rank in ranks)
        {
            hand.AddCard(new Card { Rank = rank, Suit = CardSuit.Clubs });
        }
        return hand;
    }

    private static Card DealerCard(CardRank rank) => new Card { Rank = rank, Suit = CardSuit.Clubs };

    // ---- Hard totals ----

    [Fact]
    public void HardTotal_EightOrLess_ReturnsHit()
    {
        Hand hand = CreateHand(CardRank.Five, CardRank.Three); // hard 8

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Six), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(PlayerAction.Hit, action);
    }

    [Theory]
    [InlineData(CardRank.Five, PlayerAction.Double)] // dealer 3-6
    [InlineData(CardRank.Two, PlayerAction.Hit)]     // outside that range
    public void HardTotal_Nine_ReturnsExpectedAction(CardRank dealerRank, PlayerAction expected)
    {
        Hand hand = CreateHand(CardRank.Six, CardRank.Three); // hard 9

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(dealerRank), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(expected, action);
    }

    [Theory]
    [InlineData(CardRank.Nine, PlayerAction.Double) ] // dealer <=9
    [InlineData(CardRank.Ten, PlayerAction.Hit)]      // dealer 10/A
    public void HardTotal_Ten_ReturnsExpectedAction(CardRank dealerRank, PlayerAction expected)
    {
        Hand hand = CreateHand(CardRank.Six, CardRank.Four); // hard 10

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(dealerRank), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(expected, action);
    }

    [Fact]
    public void HardTotal_Eleven_DoublesEvenVsDealerAce()
    {
        // H17: 11 doubles against everything, including a dealer Ace.
        Hand hand = CreateHand(CardRank.Six, CardRank.Five); // hard 11

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Ace), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(PlayerAction.Double, action);
    }

    [Fact]
    public void HardTotal_Eleven_HitsWhenCannotDouble()
    {
        // Three cards -> can't double even though the total qualifies.
        Hand hand = CreateHand(CardRank.Two, CardRank.Four, CardRank.Five); // hard 11

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Six), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(PlayerAction.Hit, action);
    }

    [Theory]
    [InlineData(CardRank.Five, PlayerAction.Stand)] // dealer 4-6
    [InlineData(CardRank.Two, PlayerAction.Hit)]    // outside that range
    public void HardTotal_Twelve_ReturnsExpectedAction(CardRank dealerRank, PlayerAction expected)
    {
        Hand hand = CreateHand(CardRank.Ten, CardRank.Two); // hard 12

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(dealerRank), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(expected, action);
    }

    [Theory]
    [InlineData(CardRank.Four, PlayerAction.Stand)] // dealer 2-6
    [InlineData(CardRank.Nine, PlayerAction.Hit)]   // outside that range
    public void HardTotal_ThirteenToSixteen_ReturnsExpectedAction(CardRank dealerRank, PlayerAction expected)
    {
        Hand hand = CreateHand(CardRank.Ten, CardRank.Four); // hard 14

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(dealerRank), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(expected, action);
    }

    [Fact]
    public void HardTotal_SeventeenOrMore_ReturnsStand()
    {
        Hand hand = CreateHand(CardRank.Ten, CardRank.Seven); // hard 17

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Two), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(PlayerAction.Stand, action);
    }

    // ---- Soft totals ----

    [Theory]
    [InlineData(CardRank.Five, PlayerAction.Double)] // dealer 5-6
    [InlineData(CardRank.Two, PlayerAction.Hit)]
    public void SoftTotal_ThirteenOrFourteen_ReturnsExpectedAction(CardRank dealerRank, PlayerAction expected)
    {
        Hand hand = CreateHand(CardRank.Ace, CardRank.Two); // soft 13

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(dealerRank), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(expected, action);
    }

    [Theory]
    [InlineData(CardRank.Four, PlayerAction.Double)] // dealer 4-6
    [InlineData(CardRank.Two, PlayerAction.Hit)]
    public void SoftTotal_FifteenOrSixteen_ReturnsExpectedAction(CardRank dealerRank, PlayerAction expected)
    {
        Hand hand = CreateHand(CardRank.Ace, CardRank.Four); // soft 15

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(dealerRank), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(expected, action);
    }

    [Theory]
    [InlineData(CardRank.Three, PlayerAction.Double)] // dealer 3-6
    [InlineData(CardRank.Two, PlayerAction.Hit)]
    public void SoftTotal_Seventeen_ReturnsExpectedAction(CardRank dealerRank, PlayerAction expected)
    {
        Hand hand = CreateHand(CardRank.Ace, CardRank.Six); // soft 17

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(dealerRank), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(expected, action);
    }

    [Theory]
    [InlineData(CardRank.Four, PlayerAction.Double)] // dealer 2-6
    [InlineData(CardRank.Eight, PlayerAction.Stand)] // dealer 7-8
    [InlineData(CardRank.Nine, PlayerAction.Hit)]    // dealer 9-A
    public void SoftTotal_Eighteen_ReturnsExpectedAction(CardRank dealerRank, PlayerAction expected)
    {
        Hand hand = CreateHand(CardRank.Ace, CardRank.Seven); // soft 18

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(dealerRank), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(expected, action);
    }

    [Fact]
    public void SoftTotal_Eighteen_CannotDouble_StandsInsteadOfHit()
    {
        // The subtle one: soft 18 vs a dealer 2-6, without the option to
        // double, stands - it does NOT fall back to Hit like the lower
        // soft totals do.
        Hand hand = CreateHand(CardRank.Ace, CardRank.Three, CardRank.Four); // soft 18, 3 cards

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Four), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(PlayerAction.Stand, action);
    }

    [Theory]
    [InlineData(CardRank.Six, PlayerAction.Double)]
    [InlineData(CardRank.Two, PlayerAction.Stand)]
    public void SoftTotal_Nineteen_ReturnsExpectedAction(CardRank dealerRank, PlayerAction expected)
    {
        Hand hand = CreateHand(CardRank.Ace, CardRank.Eight); // soft 19

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(dealerRank), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(expected, action);
    }

    [Fact]
    public void SoftTotal_TwentyOrMore_ReturnsStand()
    {
        Hand hand = CreateHand(CardRank.Ace, CardRank.Nine); // soft 20

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Ten), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(PlayerAction.Stand, action);
    }

    // ---- Pairs ----

    [Fact]
    public void Pair_Aces_AlwaysSplits()
    {
        Hand hand = CreateHand(CardRank.Ace, CardRank.Ace);

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Ten), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(PlayerAction.Split, action);
    }

    [Fact]
    public void Pair_Eights_SplitsEvenVsDealerAce()
    {
        Hand hand = CreateHand(CardRank.Eight, CardRank.Eight);

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Ace), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(PlayerAction.Split, action);
    }

    [Theory]
    [InlineData(CardRank.Six, PlayerAction.Split)]
    [InlineData(CardRank.Seven, PlayerAction.Stand)] // 7, 10, and Ace all stand instead
    public void Pair_Nines_ReturnsExpectedAction(CardRank dealerRank, PlayerAction expected)
    {
        Hand hand = CreateHand(CardRank.Nine, CardRank.Nine);

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(dealerRank), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(expected, action);
    }

    [Fact]
    public void Pair_Fives_PlaysAsHardTenAndDoubles()
    {
        // Never split - treated as a hard 10 instead.
        Hand hand = CreateHand(CardRank.Five, CardRank.Five);

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Six), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(PlayerAction.Double, action);
    }

    [Fact]
    public void Pair_TenValueRanks_NeverSplit()
    {
        Hand hand = CreateHand(CardRank.King, CardRank.King);

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Two), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(PlayerAction.Stand, action);
    }

    [Theory]
    [InlineData(CardRank.Seven, PlayerAction.Split)]
    [InlineData(CardRank.Eight, PlayerAction.Hit)]
    public void Pair_LowPair_ReturnsExpectedAction(CardRank dealerRank, PlayerAction expected)
    {
        Hand hand = CreateHand(CardRank.Two, CardRank.Two);

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(dealerRank), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(expected, action);
    }

    [Fact]
    public void Pair_Sixes_HitsAtDealerSevenEvenThoughSevensSplitThere()
    {
        // Sixes split only vs 2-6 - a stricter cutoff than 2s/3s/7s (2-7),
        // easy to mix up since they look like the same shape of rule.
        Hand hand = CreateHand(CardRank.Six, CardRank.Six);

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Seven), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(PlayerAction.Hit, action);
    }

    [Theory]
    [InlineData(CardRank.Five, PlayerAction.Split)]
    [InlineData(CardRank.Four, PlayerAction.Hit)]
    public void Pair_Fours_SplitsOnlyVsFiveOrSix(CardRank dealerRank, PlayerAction expected)
    {
        Hand hand = CreateHand(CardRank.Four, CardRank.Four);

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(dealerRank), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(expected, action);
    }

    // ---- Guards ----

    [Fact]
    public void SplitAces_AlreadyHasOneCard_AlwaysStands()
    {
        Hand hand = CreateHand(CardRank.Ace, CardRank.Five);
        hand.IsSplitAces = true;

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Six), seatHandCount: 1, remainingBankroll: 1000m);

        Assert.Equal(PlayerAction.Stand, action);
    }

    [Fact]
    public void SeatAtMaxHands_DoesNotOfferSplit()
    {
        // A pair that would normally always split, but the seat already
        // has the maximum 4 hands for the round - falls through to hard
        // total logic (16 vs 9 = Hit) instead of Split.
        Hand hand = CreateHand(CardRank.Eight, CardRank.Eight);

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Nine), seatHandCount: 4, remainingBankroll: 1000m);

        Assert.Equal(PlayerAction.Hit, action);
    }

    [Fact]
    public void CannotAffordSplit_PlaysAsNaturalTotalInstead()
    {
        // Would normally always split, but there isn't enough remaining
        // bankroll to cover a second hand at this bet size.
        Hand hand = CreateHand(CardRank.Eight, CardRank.Eight);
        hand.Bet = 10m;

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Nine), seatHandCount: 1, remainingBankroll: 5m);

        Assert.Equal(PlayerAction.Hit, action); // hard 16 vs 9
    }

    [Fact]
    public void CanAffordSplit_SplitsNormally()
    {
        // Same hand/dealer as above, with just enough funds - confirms the
        // gate really is the bankroll check, and that it's inclusive (>=).
        Hand hand = CreateHand(CardRank.Eight, CardRank.Eight);
        hand.Bet = 10m;

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Nine), seatHandCount: 1, remainingBankroll: 10m);

        Assert.Equal(PlayerAction.Split, action);
    }

    [Fact]
    public void CannotAffordDouble_HitsInstead()
    {
        Hand hand = CreateHand(CardRank.Six, CardRank.Five); // hard 11
        hand.Bet = 10m;

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Six), seatHandCount: 1, remainingBankroll: 5m);

        Assert.Equal(PlayerAction.Hit, action); // would otherwise double
    }

    [Fact]
    public void CanAffordDouble_DoublesNormally()
    {
        Hand hand = CreateHand(CardRank.Six, CardRank.Five); // hard 11
        hand.Bet = 10m;

        PlayerAction action = BasicStrategy.GetAction(hand, DealerCard(CardRank.Six), seatHandCount: 1, remainingBankroll: 10m);

        Assert.Equal(PlayerAction.Double, action);
    }
}
