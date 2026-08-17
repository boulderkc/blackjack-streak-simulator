namespace BlackjackStreakSimulator.Engine;

public class Card
{
    public CardRank Rank;
    public CardSuit Suit;

    // Ten, Jack, Queen, and King are distinct ranks (see CardRank) but share
    // the same blackjack point value.
    public int PointValue => Rank switch
    {
        CardRank.Ace => 11,
        CardRank.Jack or CardRank.Queen or CardRank.King => 10,
        _ => (int)Rank
    };
}
