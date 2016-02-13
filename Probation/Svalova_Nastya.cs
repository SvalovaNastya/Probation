using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Probation
{
    public enum CardColors
    {
        Red,
        Green,
        Blue,
        Yellow,
        White
    }

    public class Card
    {
        public int Rank { get; private set; }
        public CardColors Color { get; private set; }
        
        public Card(int rank, CardColors color)
        {
            Rank = rank;
            Color = color;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Card))
                throw new InvalidCastException();
            Card other = (Card) obj;
            return Color == other.Color && Rank == other.Rank;
        }

        public override int GetHashCode()
        {
            return Color.GetHashCode() ^ Rank.GetHashCode();
        }
    }

    public class CardInfo
    {
        public Card RealCard { get; private set; }
        public bool KnewRank { get; private set; }
        public bool KnewColor { get; private set; }
        public HashSet<int> NotRank { get; private set; } 
        public HashSet<CardColors> NotColor { get; private set; }

        public CardInfo(Card realCard)
        {
            RealCard = realCard;
            KnewRank = false;
            KnewColor = false;
            NotRank = new HashSet<int>();
            NotColor = new HashSet<CardColors>();
        }

        public bool TryAddInfoAboutCard(int? rank, CardColors? color, int? notRank, CardColors? notColor)
        {
            if (rank.HasValue)
                if (rank.Value != RealCard.Rank)
                    return false;
                else
                    KnewRank = true;
            if (color.HasValue)
                if (color.Value != RealCard.Color)
                    return false;
                else
                    KnewColor = true;
            if (notRank.HasValue)
                if (RealCard.Rank == notRank.Value)
                    return false;
                else
                    NotRank.Add(notRank.Value);
            if (notColor.HasValue)
                if (RealCard.Color == notColor.Value)
                    return false;
                else
                    NotColor.Add(notColor.Value);
            if (NotRank.Count == 4)
                KnewRank = true;
            if (NotColor.Count == 4)
                KnewColor = true;
            return true;
        }
    }

    public struct GameResults
    {
        public int TurnsCount;
        public int PlayedCardsCount;
        public int RiskedTurnsCount;

        public GameResults(int turnsCount, int playedCardsCount, int riskedTurnsCount)
        {
            TurnsCount = turnsCount;
            PlayedCardsCount = playedCardsCount;
            RiskedTurnsCount = riskedTurnsCount;
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
        private readonly Dictionary<char, CardColors> ColorAbbreviation = new Dictionary<char, CardColors>()
        {
            {'R', CardColors.Red},
            {'G', CardColors.Green},
            {'B', CardColors.Blue},
            {'Y', CardColors.Yellow},
            {'W', CardColors.White}
        };

        private Dictionary<CardColors, int> cardsOnTable;
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
                        if (gameIsOver)
                        {
                            yield return new GameResults(turnsCount, playedCards, riskedTurns);
                        }
                    }
                }
                currentPlayer = (currentPlayer + 1)%playersCount;
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
            return new Card(int.Parse(abbreviation[1].ToString()), ColorAbbreviation[abbreviation[0]]);
        }

        private void InitGameState()
        {
            gameIsOver = false;
            currentPlayer = -1;
            playersHands = new List<CardInfo>[playersCount];
            cardsOnTable = new Dictionary<CardColors, int>()
            {
                {CardColors.Blue, 0},
                {CardColors.Green, 0},
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
            return (currentPlayer + 1)%playersCount;
        }

        private void TellColor(string colorString, List<int> selectedCardsIndexes)
        {
            CardColors color;
            if(!Enum.TryParse(colorString, true, out color))
                throw new Exception("incorrect input format");
            var nextPlayersHand = playersHands[GetNextPlayer()];
            for (int i =0; i < nextPlayersHand.Count; i++)
            {
                if (selectedCardsIndexes.Contains(i))
                {
                    if (!nextPlayersHand[i].TryAddInfoAboutCard(null, color, null, null))
                        gameIsOver = true;
                }
                else
                {
                    if (!nextPlayersHand[i].TryAddInfoAboutCard(null, null, null, color))
                        gameIsOver = true;
                }
            }
        }

        private void TellRank(int rank, List<int> selectedCardsIndexes)
        {
            var nextPlayersHand = playersHands[GetNextPlayer()];
            for (int i = 0; i < nextPlayersHand.Count; i++)
            {
                if (selectedCardsIndexes.Contains(i))
                {
                    if (!nextPlayersHand[i].TryAddInfoAboutCard(rank, null, null, null))
                        gameIsOver = true;
                }
                else
                {
                    if (!nextPlayersHand[i].TryAddInfoAboutCard(null, null, rank, null))
                        gameIsOver = true;
                }
            }
        }

        private void PlayCard(int cardsIndex)
        {
            CardInfo card = playersHands[currentPlayer][cardsIndex];
            if (cardsOnTable[card.RealCard.Color] + 1 != card.RealCard.Rank)
            {
                gameIsOver = true;
                return;
            }
            if (CheckForRisked(card))
            {
                riskedTurns++;
            }
            cardsOnTable[card.RealCard.Color]++;
            DropCard(cardsIndex);
            if (card.RealCard.Rank == 5)
                CheckForAllCardsOnTable();
            playedCards++;
        }

        private bool CheckForRisked(CardInfo card)
        {
            if (card.KnewRank && card.RealCard.Rank == 1)
            {
                if (card.KnewColor || playedCards == 0)
                    return false;
            }
            if (card.KnewColor && card.KnewRank)
                return false;
            if (card.KnewRank)
            {
                return ColorAbbreviation.Values.Any(cardColor => !card.NotColor.Contains(cardColor) && cardsOnTable[cardColor] != card.RealCard.Rank - 1);
            }
            return true;
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

    public class Program
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
                Console.WriteLine("Turn: {0}, cards: {1}, with risk: {2}", gameResult.TurnsCount, gameResult.PlayedCardsCount, gameResult.RiskedTurnsCount);
            }
        }
    }
}
