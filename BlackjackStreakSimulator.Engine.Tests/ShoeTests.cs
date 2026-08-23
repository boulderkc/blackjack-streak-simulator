namespace BlackjackStreakSimulator.Engine.Tests;

public class ShoeTests
{
    [Fact]
    public void NeedsReshuffle_NewShoe_ReturnsFalse()
    {
        // Arrange
        Shoe shoe = new Shoe(6);

        // Act, assert
        Assert.False(shoe.NeedsReshuffle);
    }

    [Fact]
    public void NeedsReshuffle_AfterDrawingPastPenetrationThreshold_ReturnsTrue()
    {
        // Arrange — one deck is 52 cards; 75% penetration is 39, so 40 draws crosses it
        Shoe shoe = new Shoe(deckCount: 1);

        // Act
        for (int i = 0; i < 40; i++)
        {
            shoe.DrawCard();
        }

        // Assert
        Assert.True(shoe.NeedsReshuffle);
    }

    [Fact]
    public void DrawCard_ReturnsACard()
    {
        // Arrange
        Shoe shoe = new Shoe(6);

        // Act
        Card card = shoe.DrawCard();

        // Assert
        Assert.NotNull(card);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void DrawCard_DrawingFarMoreThanShoeSize_DoesNotThrow(int deckCount)
    {
        // Deliberately draw well past what a shoe of this size holds (deckCount * 52)
        // to exercise the self-healing rebuild in DrawCard, so a bulk simulation
        // can never crash mid-run because a round used more cards than expected.

        // Arrange
        Shoe shoe = new Shoe(deckCount);

        // Act
        Exception? exception = Record.Exception(() =>
        {
            for (int i = 0; i < (deckCount * 52) + 20; i++)
            {
                shoe.DrawCard();
            }
        });

        // Assert
        Assert.Null(exception);
    }
}
