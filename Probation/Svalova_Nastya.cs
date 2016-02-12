using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Probation
{
    public enum CardColors
    {
        Red,
        Greeen,
        Blue,
        Yellow,
        White
    }

    public class Card
    {

        public int? Rank { get; private set; }
        public CardColors? Color { get; private set; }
        
        public Card(int? rank, CardColors? color)
        {
            Rank = rank;
            Color = color;
        }



        public Card() : this(null, null){}

        public override bool Equals(object obj)
        {
            if (!(obj is Card))
                throw new InvalidCastException();
            Card other = (Card) obj;
            return this.Color == other.Color && this.Rank == other.Rank;
        }

        public override int GetHashCode()
        {
            return (int)Color.GetHashCode() ^ Rank.GetHashCode();
        }
    }

    public class CardInfo
    {
        public Card RealCard { get; private set; }

        public Card KnownCard { get; private set; }

        public CardInfo(Card realCard)
        {
            RealCard = realCard;
            KnownCard = new Card();
        }

        public bool TryAddInfoAboutCard(int? rank, CardColors? color)
        {
            if (rank != null && KnownCard.Rank != null)
                return false;
            int? newRank = rank ?? KnownCard.Rank;
            if (color != null && KnownCard.Color != null)
                return false;
            CardColors? newColor = color ?? KnownCard.Color;
            KnownCard = new Card(newRank, newColor);
            return true;
        }
    }

    public struct GameResults
    {
        public int turnsCount;
        public int playedCardsCount;
        public int riskedTurnsCount;

        public GameResults(int turnsCount, int playedCardsCount, int riskedTurnsCount)
        {
            this.turnsCount = turnsCount;
            this.playedCardsCount = playedCardsCount;
            this.riskedTurnsCount = riskedTurnsCount;
        }
    }

    public class GameArbiter
    {
        private readonly Regex StartNewGameRegex = new Regex(@"Start new game with deck (.*)");
        private readonly Regex TellColorRegex = new Regex(@"Tell color (\w*) for cards (.*)");
        private readonly Regex TellRankRegex = new Regex(@"Tell rank (\d) for cards (.*)");
        private readonly Regex PlayCardRegex = new Regex(@"Play card (\d)");
        private readonly Regex DropCardRegex = new Regex(@"Drop card (\d)");
        private readonly Dictionary<Regex, Action<Match>> InputDataProсessing;
        private const int playersCount = 2;
        private readonly Dictionary<char, CardColors?> ColorAbbreviation = new Dictionary<char, CardColors?>()
        {
            {'R', CardColors.Red},
            {'G', CardColors.Greeen},
            {'B', CardColors.Blue},
            {'Y', CardColors.Yellow},
            {'W', CardColors.White}
        };

        private Dictionary<CardColors?, int> cardsOnTable;
        private Queue<Card> deck; 
        private int currentPlayer;
        private List<CardInfo>[] playersHands;
        private int turnsCount;
        private int riskedTurns;
        private bool gameIsOver;
        private int playedCards;

        public GameArbiter()
        {
            InputDataProсessing = new Dictionary<Regex, Action<Match>>();
            InputDataProсessing[StartNewGameRegex] = match => StartNewGame(match.Groups[1].Value.Split(' ').ToList());
            InputDataProсessing[TellColorRegex] = match => TellColor(match.Groups[1].Value, match.Groups[2].Value.Split(' ').Select(int.Parse).ToList());
            InputDataProсessing[TellRankRegex] = match => TellRank(int.Parse(match.Groups[1].Value), match.Groups[2].Value.Split(' ').Select(int.Parse).ToList());
            InputDataProсessing[PlayCardRegex] = match => PlayCard(int.Parse(match.Groups[1].Value));
            InputDataProсessing[DropCardRegex] = match => DropCard(int.Parse(match.Groups[1].Value));
        }

        public IEnumerable<GameResults> RunGames(IEnumerable<string> inputLines)
        {
            foreach (var inputLine in inputLines)
            {
                foreach (var regex in InputDataProсessing.Keys)
                {
                    if (regex.IsMatch(inputLine))
                    {
                        if (gameIsOver && regex != StartNewGameRegex)
                            continue;
                        turnsCount++;
                        InputDataProсessing[regex](regex.Match(inputLine));
                    }
                    if (gameIsOver)
                    {
                        yield return new GameResults(turnsCount, playedCards, riskedTurns);
                    }
                }
            }
        }

        private void StartNewGame(List<string> initalCards)
        {
            InitGameState();
            for (int i = 0; i < playersCount; i++)
            {
                playersHands[i] = initalCards
                    .Skip(i*5)
                    .Take(5)
                    .Select(x => new CardInfo(SelectAbbreviationToCard(x)))
                    .ToList();
            }
            deck = new Queue<Card>(initalCards.Skip(5*playersCount).Select(SelectAbbreviationToCard));
        }

        private Card SelectAbbreviationToCard(string abbreviation)
        {
            return new Card(int.Parse(abbreviation[0].ToString()), ColorAbbreviation[abbreviation[1]]);
        }

        private void InitGameState()
        {
            currentPlayer = 0;
            playersHands = new List<CardInfo>[playersCount];
            cardsOnTable = new Dictionary<CardColors?, int>()
            {
                {CardColors.Blue, 0},
                {CardColors.Greeen, 0},
                {CardColors.Red, 0},
                {CardColors.White, 0},
                {CardColors.Yellow, 0}
            };
            turnsCount = 0;
            riskedTurns = 0;
            playedCards = 0;
        }

        private int GetNextPlayer()
        {
            return (playersCount + 1)%playersCount;
        }

        private void TellColor(string colorString, List<int> selectedCardsIndexes)
        {
            CardColors color;
            if(!CardColors.TryParse(colorString, true, out color))
                throw new Exception("incorrect input format");
            foreach (var selectedCardsIndex in selectedCardsIndexes)
            {
                var nextPlayersHand = playersHands[GetNextPlayer()];
                if (!nextPlayersHand[selectedCardsIndex].TryAddInfoAboutCard(null, color))
                    gameIsOver = true;
            }
        }

        private void TellRank(int rank, List<int> selectedCardsIndexes)
        {
            foreach (var selectedCardsIndex in selectedCardsIndexes)
            {
                var nextPlayerHand = playersHands[GetNextPlayer()];
                if (!nextPlayerHand[selectedCardsIndex].TryAddInfoAboutCard(rank, null))
                    gameIsOver = true;
            }
        }

        private void PlayCard(int cardsIndex)
        {
            var card = playersHands[currentPlayer][cardsIndex];
            if (cardsOnTable[card.RealCard.Color] + 1 != card.RealCard.Rank)
            {
                gameIsOver = true;
                return;
            }
            cardsOnTable[card.RealCard.Color]++;
            DropCard(cardsIndex);
            if (card.KnownCard.Color == null || card.KnownCard.Rank == null)
                riskedTurns++;
            if (card.RealCard.Rank == 5)
                CheckForAllCardsOnTable();
        }

        private void CheckForAllCardsOnTable()
        {
            var allCardsOnTable = true;
            foreach (var card in cardsOnTable)
            {
                if (card.Value != 5)
                    allCardsOnTable = false;
            }
            if (allCardsOnTable)
                gameIsOver = true;
        }

        private void DropCard(int cardsIndex)
        {
            playersHands[currentPlayer].RemoveAt(cardsIndex);
            playersHands[currentPlayer].Add(new CardInfo(deck.Dequeue()));
            if (deck.Count == 0)
                gameIsOver = true;
        }
    }

    public class Svalova_Nastya
    {
        public static IEnumerable<string> ReadAllLines()
        {
            string line = Console.ReadLine();
            while (line != null)
            {
                yield return line;
                line = Console.ReadLine();
            }
        }

        static void Main()
        {
            var arbiter = new GameArbiter();
            foreach (var gameResult in arbiter.RunGames(ReadAllLines()))
            {
                Console.WriteLine("Turn: {0}, cards: {1}, with risk: {2}", gameResult.turnsCount, gameResult.playedCardsCount, gameResult.riskedTurnsCount);
            }
        }
    }
}
