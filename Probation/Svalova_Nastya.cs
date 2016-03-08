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

        // Возможно не в этой задаче, но в игре в целом конструктор все же понадобится
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
        public bool RankIsKnown { get; private set; }
        public bool ColorIsKnown { get; private set; }
        public HashSet<int> NotThisRanks { get; set; } 
        public HashSet<CardColors> NotThisColors { get; private set; }

        public CardInfo(Card realCard)
        {
            RealCard = realCard;
            RankIsKnown = false;
            ColorIsKnown = false;
            NotThisRanks = new HashSet<int>();
            NotThisColors = new HashSet<CardColors>();
        }

        public bool TryAddInfoAboutRank(int? rank, int? notRank)
        {
            if (rank.HasValue)
                if (rank.Value != RealCard.Rank)
                    return false;
                else
                    RankIsKnown = true;
            if (notRank.HasValue)
                if (RealCard.Rank == notRank.Value)
                    return false;
                else
                    NotThisRanks.Add(notRank.Value);
            if (NotThisRanks.Count == 4)
                RankIsKnown = true;
            return true;
        }

        public bool TryAddInfoAboutColor(CardColors? color, CardColors? notColor)
        {
            if (color.HasValue)
                if (color.Value != RealCard.Color)
                    return false;
                else
                    ColorIsKnown = true;
            if (notColor.HasValue)
                if (RealCard.Color == notColor.Value)
                    return false;
                else
                    NotThisColors.Add(notColor.Value);
            if (NotThisColors.Count == 4)
                ColorIsKnown = true;
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

        public void AddCards(IEnumerable<CardInfo> addendCards)
        {
            foreach (var card in addendCards)
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
            for (var i = 0; i < cards.Count; i++)
            {
                if (selectedCardsIndexes.Contains(i))
                {
                    if (!cards[i].TryAddInfoAboutRank(rank, null))
                        return false;
                }
                else
                {
                    if (!cards[i].TryAddInfoAboutRank(null, rank))
                        return false;
                }
            }
            return true;
        }

        public bool TryTellColor(CardColors color, List<int> selectedCardsIndexes)
        {
            for (var i = 0; i < cards.Count; i++)
            {
                if (selectedCardsIndexes.Contains(i))
                {
                    if (!cards[i].TryAddInfoAboutColor(color, null))
                        return false;
                }
                else
                {
                    if (!cards[i].TryAddInfoAboutColor(null, color))
                        return false;
                }
            }
            return true;
        }
    }

    public class GameProcessor
    {
        private const int PlayersCount = 2;

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
                currentPlayer = (currentPlayer + 1) % PlayersCount;
                turnsCount++;
            }
        }

        public void StartNewGame(List<Card> initalCards)
        {
            InitGameState();
            for (var i = 0; i < PlayersCount; i++)
            {
                playersHands[i].AddCards(initalCards
                    .Skip(i*5)
                    .Take(5)
                    .Select(card => new CardInfo(card)));
            }
            deck = new Queue<Card>(initalCards.Skip(5*PlayersCount));
        }

        private void InitGameState()
        {
            gameIsOver = false;
            currentPlayer = -1;
            playersHands = new PlayerHand[PlayersCount];
            for (var i = 0; i < PlayersCount; i++)
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
            return (currentPlayer + 1)%PlayersCount;
        }

        public void TellColor(CardColors color, List<int> selectedCardsIndexes)
        {
            if (gameIsOver)
                return;
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
            var card = playersHands[currentPlayer].GetCard(cardsIndex);
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
            if (card.RankIsKnown && card.RealCard.Rank == 1)
            {
                if (card.ColorIsKnown || playedCards == 0)
                    return false;
            }
            if (card.ColorIsKnown && card.RankIsKnown)
                return false;
            if (card.RankIsKnown)
            {
                return cardsOnTable.Keys
                    .Where(cardColor => cardsOnTable[cardColor] != card.RealCard.Rank - 1)
                    .Any(cardColor => !card.NotThisColors.Contains(cardColor));
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
        private readonly Regex startNewGameRegex = new Regex(@"Start new game with deck (.*)");
        private readonly Regex tellColorRegex = new Regex(@"Tell color (\w*) for cards (.*)");
        private readonly Regex tellRankRegex = new Regex(@"Tell rank (\d) for cards (.*)");
        private readonly Regex playCardRegex = new Regex(@"Play card (\d)");
        private readonly Regex dropCardRegex = new Regex(@"Drop card (\d)");
        private readonly Dictionary<Regex, Action<Match>> inputDataProсessing;

        public GameCommandsParser(GameProcessor processor)
        {
            inputDataProсessing = new Dictionary<Regex, Action<Match>>();
            inputDataProсessing[startNewGameRegex] = match =>
            {
                processor.StartNewGame(
                    match.Groups[1].Value.Split(' ')
                        .Select(Card.SelectAbbreviationToCard)
                        .ToList());
            };
            inputDataProсessing[tellColorRegex] = match =>
            {
                processor.TellColor(ParseColorString(match.Groups[1].Value),
                    match.Groups[2].Value.Split(' ').Select(int.Parse).ToList());
            };
            inputDataProсessing[tellRankRegex] = match =>
            {
                processor.TellRank(int.Parse(match.Groups[1].Value),
                    match.Groups[2].Value.Split(' ').Select(int.Parse).ToList());
            };
            inputDataProсessing[playCardRegex] = match => { processor.PlayCard(int.Parse(match.Groups[1].Value)); };
            inputDataProсessing[dropCardRegex] = match => { processor.DropCard(int.Parse(match.Groups[1].Value)); };
        }

        public Action ParseCommand(string commandLine)
        {
            foreach (var regex in inputDataProсessing.Keys)
            {
                if (regex.IsMatch(commandLine))
                {
                    var currentRegex = regex;
                    return () => inputDataProсessing[currentRegex](currentRegex.Match(commandLine));
                }
            }
            throw new Exception("Error input line");
        }

        public CardColors ParseColorString(string colorString)
        {
            CardColors color;
            if (!Enum.TryParse(colorString, true, out color))
                throw new Exception("incorrect input format");
            return color;
        }
    }

    public static class Program
    {
        private static IEnumerable<string> ReadAllLines()
        {
            var line = Console.ReadLine();
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
