namespace BlackjackStreakSimulator.Engine.Tests;

public class DealerPlayTests
{
    // Suit never matters here — only Rank (and therefore Value/IsSoft) does.
    private static Hand CreateHand(params CardRank[] ranks)
    {
        Hand hand = new Hand();
        foreach (CardRank rank in ranks)
        {
            hand.AddCard(new Card { Rank = rank, Suit = CardSuit.Clubs });
        }
        return hand;
    }

    [Fact]
    public void Play_HardSeventeen_DoesNotHit()
    {
        // Arrange
        Hand dealerHand = CreateHand(CardRank.Ten, CardRank.Seven);
        Shoe shoe = new Shoe();

        // Act
        DealerPlay.Play(dealerHand, shoe);

        // Assert
        Assert.Equal(2, dealerHand.Cards.Count);
        Assert.Equal(17, dealerHand.Value);
    }

    [Fact]
    public void Play_HardEighteen_DoesNotHit()
    {
        // Arrange
        Hand dealerHand = CreateHand(CardRank.Ten, CardRank.Eight);
        Shoe shoe = new Shoe();

        // Act
        DealerPlay.Play(dealerHand, shoe);

        // Assert
        Assert.Equal(2, dealerHand.Cards.Count);
        Assert.Equal(18, dealerHand.Value);
    }

    [Fact]
    public void Play_SoftSeventeen_Hits()
    {
        // Arrange
        Hand dealerHand = CreateHand(CardRank.Ace, CardRank.Six);
        Shoe shoe = new Shoe();

        // Act
        DealerPlay.Play(dealerHand, shoe);

        // Assert — must draw at least one card, and can't stop on a soft 17 again
        Assert.True(dealerHand.Cards.Count > 2);
        Assert.True(dealerHand.IsBusted || dealerHand.Value >= 17);
        Assert.False(dealerHand.Value == 17 && dealerHand.IsSoft);
    }

    [Fact]
    public void Play_HardSixteen_HitsUntilSeventeenOrBust()
    {
        // Arrange
        Hand dealerHand = CreateHand(CardRank.Ten, CardRank.Six);
        Shoe shoe = new Shoe();

        // Act
        DealerPlay.Play(dealerHand, shoe);

        // Assert
        Assert.True(dealerHand.Cards.Count > 2);
        Assert.True(dealerHand.IsBusted || dealerHand.Value >= 17);
        Assert.False(dealerHand.Value == 17 && dealerHand.IsSoft);
    }
}
