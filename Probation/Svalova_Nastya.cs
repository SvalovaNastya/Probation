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
        public readonly int Rank;
        public readonly CardColors Color;
        private static readonly Dictionary<char, CardColors> ColorAbbreviation = new Dictionary<char, CardColors>()
        {
            {'R', CardColors.Red},
            {'G', CardColors.Green},
            {'B', CardColors.Blue},
            {'Y', CardColors.Yellow},
            {'W', CardColors.White}
        };
        
        public Card(int rank, CardColors color)
        {
            Rank = rank;
            Color = color;
        }

        public static Card SelectAbbreviationToCard(string abbreviation)
        {
            return new Card(int.Parse(abbreviation[1].ToString()), ColorAbbreviation[abbreviation[0]]);
        }
    }

    public class CardInfo
    {
        public readonly Card RealCard;
        public bool KnewRank { get; private set; }
        public bool KnewColor { get; private set; }
        public HashSet<int> NotThisRank { get; private set; } 
        public HashSet<CardColors> NotThisColor { get; private set; }

        public CardInfo(Card realCard)
        {
            RealCard = realCard;
            KnewRank = false;
            KnewColor = false;
            NotThisRank = new HashSet<int>();
            NotThisColor = new HashSet<CardColors>();
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
                    NotThisRank.Add(notRank.Value);
            if (notColor.HasValue)
                if (RealCard.Color == notColor.Value)
                    return false;
                else
                    NotThisColor.Add(notColor.Value);
            if (NotThisRank.Count == 4)
                KnewRank = true;
            if (NotThisColor.Count == 4)
                KnewColor = true;
            return true;
        }
    }

    public struct GameResult
    {
        public readonly int TurnsCount;
        public readonly int PlayedCardsCount;
        public readonly int RiskedTurnsCount;

        public GameResult(int turnsCount, int playedCardsCount, int riskedTurnsCount)
        {
            TurnsCount = turnsCount;
            PlayedCardsCount = playedCardsCount;
            RiskedTurnsCount = riskedTurnsCount;
        }
    }

    public class PlayerHand
    {
        private readonly List<CardInfo> cards;

        public PlayerHand()
        {
            cards = new List<CardInfo>();
        }

        public void AddCard(CardInfo card)
        {
            cards.Add(card);
        }

        public void AddCards(CardInfo[] cards)
        {
            foreach (var card in cards)
            {
                AddCard(card);
            }
        }

        public void DropCard(int cardIndex)
        {
            cards.RemoveAt(cardIndex);
        }

        public CardInfo GetCard(int cardIndex)
        {
            var card = cards[cardIndex];
            return card;
        }

        public bool TryTellRank(int rank, List<int> selectedCardsIndexes)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (selectedCardsIndexes.Contains(i))
                {
                    if (!cards[i].TryAddInfoAboutCard(rank, null, null, null))
                        return false;
                }
                else
                {
                    if (!cards[i].TryAddInfoAboutCard(null, null, rank, null))
                        return false;
                }
            }
            return true;
        }

        public bool TryTellColor(CardColors color, List<int> selectedCardsIndexes)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (selectedCardsIndexes.Contains(i))
                {
                    if (!cards[i].TryAddInfoAboutCard(null, color, null, null))
                        return false;
                }
                else
                {
                    if (!cards[i].TryAddInfoAboutCard(null, null, null, color))
                        return false;
                }
            }
            return true;
        }
    }

    public class GameProcessor
    {
        private const int playersCount = 2;

        private Dictionary<CardColors, int> cardsOnTable;
        private Queue<Card> deck; 
        private int currentPlayer;
        private PlayerHand[] playersHands;
        private int turnsCount;
        private int riskedTurns;
        private bool gameIsOver;
        private int playedCards;

        public IEnumerable<GameResult> RunGames(IEnumerable<Action> actions)
        {
            foreach (var action in actions)
            {
                var gameWasOver = gameIsOver;
                action();
                if (gameIsOver && !gameWasOver)
                    yield return new GameResult(turnsCount, playedCards, riskedTurns);
                currentPlayer = (currentPlayer + 1) % playersCount;
                turnsCount++;
            }
        }

        public void StartNewGame(List<string> initalCards)
        {
            InitGameState();
            for (int i = 0; i < playersCount; i++)
            {
                playersHands[i].AddCards(initalCards
                    .Skip(i*5)
                    .Take(5)
                    .Select(x => new CardInfo(Card.SelectAbbreviationToCard(x)))
                    .ToArray());
            }
            deck = new Queue<Card>(initalCards.Skip(5*playersCount).Select(Card.SelectAbbreviationToCard));
        }

        private void InitGameState()
        {
            gameIsOver = false;
            currentPlayer = -1;
            playersHands = new PlayerHand[playersCount];
            for (int i = 0; i < playersCount; i++)
            {
                playersHands[i] = new PlayerHand();
            }
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

        public void TellColor(string colorString, List<int> selectedCardsIndexes)
        {
            if (gameIsOver)
                return;
            CardColors color;
            if(!Enum.TryParse(colorString, true, out color))
                throw new Exception("incorrect input format");
            var nextPlayersHand = playersHands[GetNextPlayer()];
            if (!nextPlayersHand.TryTellColor(color, selectedCardsIndexes))
                gameIsOver = true;
        }

        public void TellRank(int rank, List<int> selectedCardsIndexes)
        {
            if (gameIsOver)
                return;
            var nextPlayersHand = playersHands[GetNextPlayer()];
            if (!nextPlayersHand.TryTellRank(rank, selectedCardsIndexes))
                gameIsOver = true;
        }

        public void DropCard(int cardsIndex)
        {
            if (gameIsOver)
                return;
            playersHands[currentPlayer].DropCard(cardsIndex);
            playersHands[currentPlayer].AddCard(new CardInfo(deck.Dequeue()));
            if (deck.Count == 0)
                gameIsOver = true;
        }

        public void PlayCard(int cardsIndex)
        {
            if (gameIsOver)
                return;
            CardInfo card = playersHands[currentPlayer].GetCard(cardsIndex);
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
                return cardsOnTable.Keys.Where(cardColor => cardsOnTable[cardColor] != card.RealCard.Rank - 1).Any(cardColor => !card.NotThisColor.Contains(cardColor));
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
    }

    public class GameCommandsParser
    {
        private readonly Regex StartNewGameRegex = new Regex(@"Start new game with deck (.*)");
        private readonly Regex TellColorRegex = new Regex(@"Tell color (\w*) for cards (.*)");
        private readonly Regex TellRankRegex = new Regex(@"Tell rank (\d) for cards (.*)");
        private readonly Regex PlayCardRegex = new Regex(@"Play card (\d)");
        private readonly Regex DropCardRegex = new Regex(@"Drop card (\d)");
        private readonly Dictionary<Regex, Action<Match>> InputDataProсessing;

        public GameCommandsParser(GameProcessor processor)
        {
            InputDataProсessing = new Dictionary<Regex, Action<Match>>();
            InputDataProсessing[StartNewGameRegex] = match => processor.StartNewGame(match.Groups[1].Value.Split(' ').ToList());
            InputDataProсessing[TellColorRegex] = match => processor.TellColor(match.Groups[1].Value, match.Groups[2].Value.Split(' ').Select(int.Parse).ToList());
            InputDataProсessing[TellRankRegex] = match => processor.TellRank(int.Parse(match.Groups[1].Value), match.Groups[2].Value.Split(' ').Select(int.Parse).ToList());
            InputDataProсessing[PlayCardRegex] = match => processor.PlayCard(int.Parse(match.Groups[1].Value));
            InputDataProсessing[DropCardRegex] = match => processor.DropCard(int.Parse(match.Groups[1].Value));
        }

        public Action ParseCommand(string commandLine)
        {
            foreach (var regex in InputDataProсessing.Keys)
            {
                if (regex.IsMatch(commandLine))
                {
                    var currentRegex = regex;
                    return () => InputDataProсessing[currentRegex](currentRegex.Match(commandLine));
                }
            }
            throw new Exception("Error input line");
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
            var arbiter = new GameProcessor();
            var commandsParser = new GameCommandsParser(arbiter);
            foreach (var gameResult in arbiter.RunGames(ReadAllLines().Select(commandsParser.ParseCommand)))
            {
                Console.WriteLine("Turn: {0}, cards: {1}, with risk: {2}", gameResult.TurnsCount, gameResult.PlayedCardsCount, gameResult.RiskedTurnsCount);
            }
        }
    }
}
