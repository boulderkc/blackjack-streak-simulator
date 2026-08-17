namespace BlackjackStreakSimulator.Engine;

public class Shoe
{
    private const double PenetrationThreshold = 0.75;

    private readonly int deckCount;
    private List<Card> cards = new();
    private int currentShoePosition = 0;

    // default to 6 decks — matches typical Vegas shoe sizes, and leaves enough
    // reserve past the penetration threshold for a heavy multi-split round.
    public Shoe(int deckCount = 6)
    {
        this.deckCount = deckCount;
        BuildAndShuffle();
    }

    // True once enough of the shoe has been dealt that a fresh shoe should be
    // built before the next round starts. The caller decides when to act on
    // this (e.g. between rounds) — Shoe only reports its own state.
    public bool NeedsReshuffle => currentShoePosition >= cards.Count * PenetrationThreshold;

    public Card DrawCard()
    {
        if (currentShoePosition >= cards.Count)
        {
            // Safety net only: NeedsReshuffle checked between rounds should make
            // this rare-to-never in practice, but a bulk simulation running
            // tens of thousands of hands must never crash mid-round over it.
            BuildAndShuffle();
        }

        Card next = cards[currentShoePosition];
        currentShoePosition++;
        return next;
    }

    private void BuildAndShuffle()
    {
        cards = new List<Card>();
        for (int n = 0; n < deckCount; n++)
        {
            foreach (CardSuit suit in Enum.GetValues<CardSuit>())
            {
                foreach (CardRank rank in Enum.GetValues<CardRank>())
                {
                    cards.Add(new Card { Rank = rank, Suit = suit });
                }
            }
        }

        Card[] shoeArray = cards.ToArray();
        Random.Shared.Shuffle(shoeArray);
        cards = shoeArray.ToList();

        currentShoePosition = 0;
    }
}
