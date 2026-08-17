namespace BlackjackStreakSimulator.Engine;

// Each rank has a distinct underlying value so rank *identity* (needed for
// same-rank-only splitting) can be told apart at runtime — Ten, Jack, Queen,
// and King all score the same in blackjack (10), but they are not the same
// rank. Blackjack point value is computed separately; see Hand.PointValue.
public enum CardRank
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14
}
