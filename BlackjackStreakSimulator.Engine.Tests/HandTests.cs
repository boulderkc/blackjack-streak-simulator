namespace BlackjackStreakSimulator.Engine.Tests;

public class HandTests
{
    [Fact]
    public void Value_AceAndKing_ReturnsTwentyOne()
    {
        // Arrange
        Hand aceAndKing = new Hand();
        aceAndKing.AddCard(new Card() {Rank = CardRank.Ace, Suit = CardSuit.Clubs});
        aceAndKing.AddCard(new Card() {Rank = CardRank.King, Suit = CardSuit.Clubs});

        // Act, assert
        Assert.Equal(21, aceAndKing.Value);

    }

    [Fact]
    public void Value_TwoAcesAndNine_ReturnsTwentyOneAndIsSoft()
    {
        // Arrange
        Hand hand = new Hand();
        hand.AddCard(new Card { Rank = CardRank.Ace, Suit = CardSuit.Clubs });
        hand.AddCard(new Card { Rank = CardRank.Ace, Suit = CardSuit.Hearts });
        hand.AddCard(new Card { Rank = CardRank.Nine, Suit = CardSuit.Spades });

        // Act, assert — one Ace demotes from 11 to 1, the other stays soft
        Assert.Equal(21, hand.Value);
        Assert.True(hand.IsSoft);
    }

    [Fact]
    public void Value_TenAndSeven_ReturnsHardSeventeen()
    {
        // Arrange
        Hand hand = new Hand();
        hand.AddCard(new Card { Rank = CardRank.Ten, Suit = CardSuit.Diamonds });
        hand.AddCard(new Card { Rank = CardRank.Seven, Suit = CardSuit.Spades });

        // Act, assert — no Aces, so nothing to demote
        Assert.Equal(17, hand.Value);
        Assert.False(hand.IsSoft);
    }

    [Fact]
    public void Value_ThreeAces_DemotesTwoAcesAndStaysSoft()
    {
        // Arrange
        Hand hand = new Hand();
        hand.AddCard(new Card { Rank = CardRank.Ace, Suit = CardSuit.Clubs });
        hand.AddCard(new Card { Rank = CardRank.Ace, Suit = CardSuit.Hearts });
        hand.AddCard(new Card { Rank = CardRank.Ace, Suit = CardSuit.Spades });

        // Act, assert — 11+11+11=33; two Aces demote to 1 each (33-10-10=13); one still soft
        Assert.Equal(13, hand.Value);
        Assert.True(hand.IsSoft);
    }

    [Fact]
    public void IsBusted_TenNineFive_ReturnsTrue()
    {
        // Arrange
        Hand hand = new Hand();
        hand.AddCard(new Card { Rank = CardRank.Ten, Suit = CardSuit.Clubs });
        hand.AddCard(new Card { Rank = CardRank.Nine, Suit = CardSuit.Hearts });
        hand.AddCard(new Card { Rank = CardRank.Five, Suit = CardSuit.Spades });

        // Act, assert
        Assert.True(hand.IsBusted);
    }

    [Fact]
    public void IsBusted_TenAndSeven_ReturnsFalse()
    {
        // Arrange
        Hand hand = new Hand();
        hand.AddCard(new Card { Rank = CardRank.Ten, Suit = CardSuit.Clubs });
        hand.AddCard(new Card { Rank = CardRank.Seven, Suit = CardSuit.Hearts });

        // Act, assert
        Assert.False(hand.IsBusted);
    }

    [Fact]
    public void IsBlackjack_AceAndKingNotFromSplit_ReturnsTrue()
    {
        // Arrange
        Hand hand = new Hand { IsFromSplit = false };
        hand.AddCard(new Card { Rank = CardRank.Ace, Suit = CardSuit.Clubs });
        hand.AddCard(new Card { Rank = CardRank.King, Suit = CardSuit.Hearts });

        // Act, assert
        Assert.True(hand.IsBlackjack);
    }

    [Fact]
    public void IsBlackjack_AceAndKingFromSplit_ReturnsFalse()
    {
        // Arrange — same cards as above, but this hand came from a split
        Hand hand = new Hand { IsFromSplit = true };
        hand.AddCard(new Card { Rank = CardRank.Ace, Suit = CardSuit.Clubs });
        hand.AddCard(new Card { Rank = CardRank.King, Suit = CardSuit.Hearts });

        // Act, assert
        Assert.False(hand.IsBlackjack);
    }

    [Fact]
    public void IsBlackjack_ThreeCardTwentyOne_ReturnsFalse()
    {
        // Arrange — 21 reached via three cards doesn't count as a natural blackjack
        Hand hand = new Hand();
        hand.AddCard(new Card { Rank = CardRank.Seven, Suit = CardSuit.Clubs });
        hand.AddCard(new Card { Rank = CardRank.Seven, Suit = CardSuit.Hearts });
        hand.AddCard(new Card { Rank = CardRank.Seven, Suit = CardSuit.Spades });

        // Act, assert
        Assert.Equal(21, hand.Value);
        Assert.False(hand.IsBlackjack);
    }

    [Theory]
    [InlineData(CardRank.Jack, CardRank.Jack, true)]
    [InlineData(CardRank.Two, CardRank.Two, true)]
    [InlineData(CardRank.King, CardRank.Jack, false)]
    [InlineData(CardRank.Five, CardRank.Six, false)]
    public void CanSplit_RankPair_ReturnsExpected(CardRank rank1, CardRank rank2, bool expectedCanSplit)
    {
        // Arrange
        Hand hand = new Hand();
        hand.AddCard(new Card { Rank = rank1, Suit = CardSuit.Clubs });
        hand.AddCard(new Card { Rank = rank2, Suit = CardSuit.Hearts });

        // Act, assert
        Assert.Equal(expectedCanSplit, hand.CanSplit);
    }

    [Fact]
    public void CanSplit_ThreeCards_ReturnsFalse()
    {
        // Arrange — first two cards match rank, but a third card (e.g. from a hit)
        // means this is no longer a splittable two-card hand
        Hand hand = new Hand();
        hand.AddCard(new Card { Rank = CardRank.Two, Suit = CardSuit.Clubs });
        hand.AddCard(new Card { Rank = CardRank.Two, Suit = CardSuit.Hearts });
        hand.AddCard(new Card { Rank = CardRank.Five, Suit = CardSuit.Spades });

        // Act, assert
        Assert.False(hand.CanSplit);
    }

    [Fact]
    public void AddCard_AddsCardToHand()
    {
        // Arrange
        Hand hand = new Hand();
        Card card = new Card { Rank = CardRank.Five, Suit = CardSuit.Spades };

        // Act
        hand.AddCard(card);

        // Assert
        Assert.Single(hand.Cards);
        Assert.Contains(card, hand.Cards);
    }
}
