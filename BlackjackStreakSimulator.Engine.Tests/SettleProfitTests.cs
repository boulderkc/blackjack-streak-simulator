namespace BlackjackStreakSimulator.Engine.Tests;

public class SettleProfitTests
{
    // Suit never matters for settlement, so every hand here is built from
    // Clubs — only Rank (and therefore Value/IsBlackjack) is relevant.
    private static Hand CreateHand(decimal bet, params CardRank[] ranks)
    {
        Hand hand = new Hand { Bet = bet };
        foreach (CardRank rank in ranks)
        {
            hand.AddCard(new Card { Rank = rank, Suit = CardSuit.Clubs });
        }
        return hand;
    }

    [Fact]
    public void SettleProfit_BothBlackjack_ReturnsZero()
    {
        Hand player = CreateHand(10, CardRank.Ace, CardRank.King);
        Hand dealer = CreateHand(0, CardRank.Ace, CardRank.King);

        Assert.Equal(0m, player.SettleProfit(dealer));
    }

    [Fact]
    public void SettleProfit_PlayerBlackjackOnly_ReturnsOneAndHalfTimesBet()
    {
        Hand player = CreateHand(10, CardRank.Ace, CardRank.King);
        Hand dealer = CreateHand(0, CardRank.Ten, CardRank.Seven);

        Assert.Equal(15m, player.SettleProfit(dealer));
    }

    [Fact]
    public void SettleProfit_DealerBlackjackOnly_ReturnsNegativeBet()
    {
        Hand player = CreateHand(10, CardRank.Ten, CardRank.Seven);
        Hand dealer = CreateHand(0, CardRank.Ace, CardRank.King);

        Assert.Equal(-10m, player.SettleProfit(dealer));
    }

    [Fact]
    public void SettleProfit_DealerBlackjackVsPlayerNonBlackjackTwentyOne_ReturnsNegativeBet()
    {
        // Regression case: a player reaching 21 via three cards is NOT a
        // natural blackjack, so the dealer's natural still wins outright —
        // this must not fall through to a push just because both are 21.
        Hand player = CreateHand(10, CardRank.Seven, CardRank.Seven, CardRank.Seven);
        Hand dealer = CreateHand(0, CardRank.Ace, CardRank.King);

        Assert.Equal(-10m, player.SettleProfit(dealer));
    }

    [Fact]
    public void SettleProfit_PlayerBusted_ReturnsNegativeBet()
    {
        Hand player = CreateHand(10, CardRank.Ten, CardRank.Nine, CardRank.Five);
        Hand dealer = CreateHand(0, CardRank.Ten, CardRank.Seven);

        Assert.Equal(-10m, player.SettleProfit(dealer));
    }

    [Fact]
    public void SettleProfit_DealerBustedPlayerNot_ReturnsBet()
    {
        Hand player = CreateHand(10, CardRank.Ten, CardRank.Seven);
        Hand dealer = CreateHand(0, CardRank.Ten, CardRank.Nine, CardRank.Five);

        Assert.Equal(10m, player.SettleProfit(dealer));
    }

    [Fact]
    public void SettleProfit_PlayerValueHigher_ReturnsBet()
    {
        Hand player = CreateHand(10, CardRank.Ten, CardRank.Nine);
        Hand dealer = CreateHand(0, CardRank.Ten, CardRank.Seven);

        Assert.Equal(10m, player.SettleProfit(dealer));
    }

    [Fact]
    public void SettleProfit_PlayerValueLower_ReturnsNegativeBet()
    {
        Hand player = CreateHand(10, CardRank.Ten, CardRank.Seven);
        Hand dealer = CreateHand(0, CardRank.Ten, CardRank.Nine);

        Assert.Equal(-10m, player.SettleProfit(dealer));
    }

    [Fact]
    public void SettleProfit_EqualValues_ReturnsZero()
    {
        Hand player = CreateHand(10, CardRank.Ten, CardRank.Seven);
        Hand dealer = CreateHand(0, CardRank.Nine, CardRank.Eight);

        Assert.Equal(0m, player.SettleProfit(dealer));
    }

    [Fact]
    public void SettleProfit_BlackjackWithFractionalPayout_ReturnsExactDecimalValue()
    {
        // The original motivation for Bet being decimal, not int: a $5
        // blackjack bet pays exactly $7.50 at 3:2, not a rounded value.
        Hand player = CreateHand(5, CardRank.Ace, CardRank.King);
        Hand dealer = CreateHand(0, CardRank.Ten, CardRank.Seven);

        Assert.Equal(7.5m, player.SettleProfit(dealer));
    }
}
