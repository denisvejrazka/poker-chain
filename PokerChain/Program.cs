using System;
using static Poker.Program;

namespace Poker;

class Program
{
    static void Main(string[] args)
    {
        //inicialization
        Player player1 = new Player("Denis", 1000);
        Player player2 = new Player("Daniel", 2000);
        Player player3 = new Player("Ondřej", 7000);
        Player player4 = new Player("Petr", 2300);
        List<Player> players = new List<Player> { player1, player2, player3 };
        GameManager game = new GameManager(players);
        RequestHandler RoyalFlushHandler = new RoyalFlush(game);
        RequestHandler StraightFlushHandler = new StraightFlush(game);
        RequestHandler FourOfAKindHandler = new FourOfAKind(game);
        RequestHandler FullHouseHandler = new FullHouse(game);
        RequestHandler FlushHandler = new Flush(game);
        RequestHandler StraightHandler = new Straight(game);
        RequestHandler ThreeOfAKindHandler = new ThreeOfAKind(players, game);
        RequestHandler TwoPairHandler = new TwoPair(game);
        RequestHandler PairHandler = new Pair(game);
        RequestHandler HighCardHandler = new HighCard(game);

        //game loop
        Deck.DealTheCardsToPlayers(players);
        player1.isOnTurn = true;
        Deck.DealFlop();
        Deck.DealTurn();
        Deck.DealRiver();
        game.currentPlayer = player1;
        player1.currentBet = 1;

        //chain of responsibility
        RoyalFlushHandler.SetNext(StraightFlushHandler).SetNext(FourOfAKindHandler)
        .SetNext(FullHouseHandler).SetNext(FlushHandler).SetNext(StraightHandler)
        .SetNext(ThreeOfAKindHandler).SetNext(TwoPairHandler).SetNext(PairHandler)
        .SetNext(HighCardHandler);

        game.GameLoop();

        foreach (Player player in players)
        {
            RoyalFlushHandler.HandleRequest(Deck.PlayerCommunityCards(player));
        }
        Console.WriteLine(game.TypeOfGameEnd);
    }

    public abstract class RequestHandler
    {
        private RequestHandler next;

        public RequestHandler SetNext(RequestHandler next)
        {
            this.next = next;
            return next;
        }

        protected void PassNext(List<Card> CC)
        {
            if (next != null)
            {
                next.HandleRequest(CC);
            }
        }

        public abstract void HandleRequest(List<Card> CC);
    }

    public class RoyalFlush : RequestHandler
    {
        private GameManager game;

        public RoyalFlush(GameManager game)
        {
            this.game = game;
        }

        public override void HandleRequest(List<Card> CC)
        {
            List<Rank> ranks = new List<Rank>();
            List<Suit> suits = new List<Suit>();

            foreach (Card card in CC)
            {
                ranks.Add(card.GetRank());

            }

            if (ranks.Contains(Rank.Ace) && ranks.Contains(Rank.King) && ranks.Contains(Rank.Queen) && ranks.Contains(Rank.Jack) && ranks.Contains(Rank.Ten))
            {
                if (suits.All(s => s == Suit.Diamonds || s == Suit.Clubs || s == Suit.Hearts || s == Suit.Spades))
                {
                    game.SetGameEnd("Royal flush");
                }
            }
            PassNext(CC);
        }
    }

    public class StraightFlush : RequestHandler
    {
        private GameManager game;
        public StraightFlush(GameManager game)
        {
            this.game = game;
        }

        public override void HandleRequest(List<Card> CC)
        {
            List<Card> SortedCC = CC.OrderBy(c => c.GetRank()).ToList();
            int cntr = 0;

            for (int i = 0; i < CC.Count - 1; i++)
            {
                if ((SortedCC[i + 1].GetRank() == SortedCC[i].GetRank() + 1) && (SortedCC[i + 1].GetSuit() == SortedCC[i].GetSuit() + 1))
                {
                    cntr += 1;
                }
            }

            if (cntr >= 5)
            {
                game.SetGameEnd("Straight FLush");
                return;
            }
            PassNext(CC);
        }
    }

    public class FourOfAKind : RequestHandler
    {
        private GameManager game;

        public FourOfAKind(GameManager game)
        {
            this.game = game;
        }

        public override void HandleRequest(List<Card> CC)
        {
            foreach (var pair in game.CountCC(CC))
            {
                if (pair.Value == 4)
                {
                    game.SetGameEnd("Four of a kind");
                    return;
                }
                PassNext(CC);
            }
        }
    }

    public class FullHouse : RequestHandler
    {
        private GameManager game;

        public FullHouse(GameManager game)
        {
            this.game = game;
        }

        public override void HandleRequest(List<Card> CC)
        {
            if (game.CountCC(CC).ContainsValue(2) && game.CountCC(CC).ContainsValue(3))
            {
                game.SetGameEnd("Full House");
                return;
            }
            PassNext(CC);
        }
    }

    public class Flush : RequestHandler
    {
        private GameManager game;

        public Flush(GameManager game)
        {
            this.game = game;
        }

        public override void HandleRequest(List<Card> CC)
        {
            if (CC.GroupBy(card => card.GetSuit()).Count(group => group.Count() >= 5) == 1)
            {
                game.SetGameEnd("Flush");
                return;
            }
            PassNext(CC);
        }
    }

    public class Straight : RequestHandler
    {
        private GameManager game;

        public Straight(GameManager game)
        {
            this.game = game;
        }

        public override void HandleRequest(List<Card> CC)
        {
            List<Card> SortedCC = CC.OrderBy(c => c.GetRank()).ToList();
            int cntr = 0;

            for (int i = 0; i < CC.Count - 1; i++)
            {
                if (SortedCC[i + 1].GetRank() == SortedCC[i].GetRank() + 1)
                {
                    cntr += 1;
                }
            }

            if (cntr >= 5)
            {
                game.SetGameEnd("Straight");
                return;
            }
            PassNext(CC);
        }
    }

    public class ThreeOfAKind : RequestHandler
    {
        private List<Player> players;
        private GameManager game;

        public ThreeOfAKind(List<Player> players, GameManager game)
        {
            this.players = players;
            this.game = game;
        }

        public override void HandleRequest(List<Card> CC)
        {
            foreach (var pair in game.CountCC(CC))
            {
                if (pair.Value == 3)
                {
                    game.SetGameEnd("Three of a kind");
                    return;
                }
                PassNext(CC);
            }
        }
    }

    public class TwoPair : RequestHandler
    {
        private GameManager game;

        public TwoPair(GameManager game)
        {
            this.game = game;
        }

        public override void HandleRequest(List<Card> CC)
        {
            if (game.CountCC(CC).Count(kvp => kvp.Value == 2) == 2)
            {
                game.SetGameEnd("Two pair");
                return;
            }
            PassNext(CC);
        }
    }

    //funguje
    public class Pair : RequestHandler
    {
        private GameManager game;

        public Pair(GameManager game)
        {
            this.game = game;
        }

        public override void HandleRequest(List<Card> CC)
        {
            foreach (var pair in game.CountCC(CC))
            {
                if (pair.Value == 2)
                {
                    game.SetGameEnd("Pair");
                    return;
                }
                PassNext(CC);
            }
        }
    }

    public class HighCard : RequestHandler
    {
        private GameManager game;

        public HighCard(GameManager game)
        {
            this.game = game;
        }

        public override void HandleRequest(List<Card> CC)
        {
            game.SetGameEnd("High card");
        }
    }

    public class Player
    {
        public string playerName;
        public int playerCash;
        public bool isWinner;
        public List<Card> hand;
        public bool isOnTurn;
        public int currentBet;
        public bool didFold;
        public bool didAllIn;
        public List<int> bets;
        public string choice;
        public bool didCheck;

        public Player(string name, int cash)
        {
            playerName = name;
            playerCash = cash;
            hand = new List<Card>();
            bets = new List<int>();
            currentBet = 0;
            didFold = false;
            didAllIn = false;
            didCheck = false;
            choice = "";
            isWinner = false;
        }

        public void ShowHand()
        {
            Console.WriteLine("");
            foreach (Card card in hand)
            {
                Console.WriteLine($"|{card}| ");
            }
            Console.WriteLine("");
        }
    }

    public enum Suit { Hearts, Diamonds, Clubs, Spades }

    public enum Rank { Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }

    public static class Deck
    {
        public static Random random = new Random();
        public static int deckSize = 52;
        public static List<Card> deck = new List<Card>();
        public static List<Card> flop = new List<Card>();
        public static Card turn;
        public static Card river;

        static Deck()
        {
            deckSize = 52;
            deck = new List<Card>(deckSize);

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    Card card = new Card(rank, suit);
                    deck.Add(card);
                }
            }
        }

        public static void Shuffle()
        {
            random = new Random();

            while (deckSize > 1)
            {
                deckSize--;
                int k = random.Next(deckSize);
                Card value = deck[k];
                deck[k] = deck[deckSize];
                deck[deckSize] = value;
            }
        }

        public static bool DealTheCardsToPlayers(List<Player> players)
        {
            Shuffle();

            foreach (Player player in players)
            {
                Card card1 = deck[0];
                deck.RemoveAt(0);
                Card card2 = deck[0];
                deck.RemoveAt(0);
                player.hand.Add(card1);
                player.hand.Add(card2);
            }

            return true;
        }

        public static void DealFlop()
        {
            int randomIndex = random.Next(deck.Count);

            for (int i = 0; i < 3; i++)
            {
                Card randomCard = deck[randomIndex];
                flop.Add(randomCard);
                deck.RemoveAt(randomIndex);
            }
        }

        public static void DealTurn()
        {
            int randomIndex = random.Next(deck.Count);
            Card randomCard = deck[randomIndex];
            turn = randomCard;
            deck.RemoveAt(randomIndex);
        }

        public static void DealRiver()
        {
            int randomIndex = random.Next(deck.Count);
            Card randomCard = deck[randomIndex];
            river = randomCard;
            deck.RemoveAt(randomIndex);
        }

        public static List<Card> PlayerCommunityCards(Player player)
        {
            List<Card> CommunityCards = new List<Card>();
            CommunityCards.Add(Deck.turn);
            CommunityCards.Add(Deck.river);
            CommunityCards.AddRange(Deck.flop);
            foreach (Card card in player.hand)
            {
                CommunityCards.Add(card);
            }
            return CommunityCards;
        }
    }


    public class Card
    {
        public Suit Suit { get; }
        public Rank Rank { get; }

        public Card(Rank rank, Suit suit)
        {
            Suit = suit;
            Rank = rank;
        }

        public override string ToString() => $"{(int)Rank} of {Suit}";

        public Rank GetRank() => Rank;

        public Suit GetSuit() => Suit;
    }


    public class GameManager
    {
        public Player currentPlayer;
        public List<Player> players;
        public int playersChoice;
        public int currentRoundBet;
        public int pot;
        public int numberOfPLayers;
        public int gameRound;
        public bool isGameOver;
        public Player playerToSkip;
        public bool check;
        public string TypeOfGameEnd { get; set; }


        public GameManager(List<Player> listOfPlayers)
        {
            players = listOfPlayers;
            numberOfPLayers = players.Count;
            pot = 0;
            gameRound = 0;
            isGameOver = false;
            currentRoundBet = 0;
            check = false;
            TypeOfGameEnd = "";
        }

        public Dictionary<Rank, int> CountCC(List<Card> CC)
        {
            Dictionary<Rank, int> CCDict = new Dictionary<Rank, int>();

            foreach (Card ComunityCard in CC)
            {
                if (CCDict.ContainsKey(ComunityCard.GetRank()))
                {
                    CCDict[ComunityCard.GetRank()] += 1;
                }
                else
                {
                    CCDict.Add(ComunityCard.GetRank(), 1);
                }
            }
            return CCDict;
        }

        public void SetGameEnd(string type)
        {
            TypeOfGameEnd = type;
        }

        public void PrintFlop()
        {
            Console.WriteLine("");
            Console.WriteLine($"  Pot value: {pot}");
            Console.WriteLine("");
            string flopString = "Flop cards: ";

            foreach (Card card in Deck.flop)
            {
                flopString += card.ToString() + "    ";
            }

            Console.WriteLine(flopString);
        }

        public void PrintTurn()
        {
            Console.WriteLine("");
            Console.WriteLine($"  Pot value: {pot}");
            Console.WriteLine("");
            string flopString = "Flop cards: ";

            foreach (Card card in Deck.flop)
            {
                flopString += card + "    ";
            }

            Console.WriteLine(flopString);
            Console.WriteLine($"Turn cards: {Deck.turn}");
        }

        public void PrintRiver()
        {
            Console.WriteLine("");
            Console.WriteLine($"  Pot value: {pot}");
            Console.WriteLine("");
            string flopString = "Flop cards: ";

            foreach (Card card in Deck.flop)
            {
                flopString += card.ToString() + "    ";
            }

            Console.WriteLine(flopString);
            Console.WriteLine($"Turn card: {Deck.turn}");
            Console.WriteLine($"River card: {Deck.river}");
        }

        public void SwitchPlayer()
        {
            int currentIndex = players.FindIndex(player => player.isOnTurn);
            players[currentIndex].isOnTurn = false;

            do
            {
                currentIndex = (currentIndex + 1) % numberOfPLayers;
            } while (players[currentIndex] == playerToSkip);

            players[currentIndex].isOnTurn = true;
            currentPlayer = players[currentIndex];
        }


        public void ProcessPlayersInput()
        {
            bool validInput = false;

            while (!validInput)
            {
                Console.WriteLine("0| check");
                Console.WriteLine("1| call");
                Console.WriteLine("2| raise");
                Console.WriteLine("3| fold");
                Console.WriteLine("4| all in");
                string input = Console.ReadLine();

                playersChoice = Convert.ToInt16(input);

                switch (playersChoice)
                {
                    case 0:
                        validInput = Check(currentPlayer);
                        currentPlayer.choice = "check";
                        break;
                    case 1:
                        validInput = Call(currentPlayer);
                        currentPlayer.choice = "call";
                        break;
                    case 2:
                        validInput = Raise(currentPlayer);
                        currentPlayer.choice = "raise";
                        break;
                    case 3:
                        validInput = true;
                        Fold(currentPlayer);
                        currentPlayer.choice = "fold";
                        break;
                    case 4:
                        validInput = AllIn(currentPlayer);
                        currentPlayer.choice = "allin";
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please choose again.");
                        break;
                }

                if (validInput && (playersChoice == 1 || playersChoice == 2)) 
                {
                    Console.WriteLine($"Player {currentPlayer.playerName} bet {currentPlayer.currentBet}");
                }
            }
        }

        public bool Raise(Player player)
        {
            Console.WriteLine("How much would you like to bet?");
            string bet = Console.ReadLine();
            player.currentBet = Convert.ToInt32(bet);

            if (player.currentBet < currentRoundBet)
            {
                Console.WriteLine("Bet again!");

                return false;
            }
            else if (player.playerCash >= player.currentBet)
            {
                currentRoundBet = player.currentBet;
                return true;
            }
            else
            {
                Console.WriteLine("Not enough cash");
                return false;
            }
        }


        public bool Call(Player player)
        {
            if (player.playerCash >= currentRoundBet)
            {
                player.currentBet = currentRoundBet;
                return true;
            }
            else
            {
                Console.WriteLine("Not enough cash");
                return false;
            }
        }

        public void Fold(Player player)
        {
            player.didFold = true;
            playerToSkip = player;
        }

        public bool Check(Player player)
        { 
            player.didCheck = true;
            return true;
        }

        public bool AllIn(Player player)
        {
            bool didSomeoneAllIn = players.Any(p => p.didAllIn);
            List<Player> allInners = new List<Player>();
            bool isAllInnersEmpty = !allInners.Any();

            if (isAllInnersEmpty || player.playerCash < currentRoundBet)
            {
                player.currentBet = player.playerCash;
                currentRoundBet = player.currentBet;
                player.didAllIn = true;
                return true;
            }
            else
            {
                int maxBetFromAllInners = allInners.Max(aI => aI.currentBet);
                player.currentBet = maxBetFromAllInners;
                currentRoundBet = player.currentBet;
                player.didAllIn = true;
                return true;
            }
        }

        public int UpdateGamePot()
        {
            int finalBet = 0;
            foreach (Player player in players)
            {
                player.playerCash -= player.currentBet;
                player.bets.Add(player.currentBet);
                finalBet += player.currentBet;
            }
            if (check)
            {
                return pot -= 0;
            }
            return pot += finalBet;
        }

        public bool DidAllPlayersAllIn() => players.All(player => player.didAllIn);

        public bool FindWinner()
        {
            List<Player> foldedPlayers = new List<Player>();
            foreach (Player player in players)
            {
                if (player.didFold && players.Count(player => player.didFold) > 1)
                {
                    foldedPlayers.Add(player);
                }
            }

            List<Player> differenceList = players.Except(foldedPlayers).ToList();

            foreach (Player p in differenceList)
            {
                if (differenceList.Count == 1)
                {
                    p.isWinner = true;
                    Console.WriteLine($"{p.playerName} won {p.currentBet + pot}!");
                    return true;
                }
            }
            return false;
        }

        public void RoundCounter()
        {
            if (!GameRoundController())
            {
                gameRound += 1;
            }
            else if (DidAllPlayersAllIn())
            {
                gameRound += 3;
                isGameOver = true;
            }
        }

        public void RefreshStats()
        {
            foreach (Player player in players)
            {
                players[0].currentBet = 1;
                player.currentBet = 0;
                player.didAllIn = false;
                player.didCheck = false;
            }
            currentRoundBet = 0;
            check = false;
        }

        public bool GameRoundController()
        {
            if (players.Where(player => (player.choice != "fold" || player.choice != "check")).All(player => player.currentBet == currentRoundBet))
            {
                return false;
            }
            else if (players.Where(player => player.choice != "fold").All(player => player.didCheck))
            {
                check = true;
                return false;
            }
            else if (players.Where(player => player.choice != "fold").All(player => (player.didAllIn || player.currentBet == currentRoundBet)))
            {
                return false;
            }
            return true;
        }

        public void GameLoop()
        {
            do
            {
                Console.WriteLine(currentPlayer.playerName, currentPlayer.playerCash);
                currentPlayer.ShowHand();
                ProcessPlayersInput();
                SwitchPlayer();

                if (!GameRoundController())
                {
                    UpdateGamePot();
                    RoundCounter();
                    switch (gameRound)
                    {
                        case 1:
                            PrintFlop();
                            break;
                        case 2:
                            PrintTurn();
                            break;
                        case 3:
                            PrintRiver();
                            break;
                    }
                    RefreshStats();
                }
            }
            while (gameRound != 3);
        }
    }
}