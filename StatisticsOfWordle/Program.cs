namespace StatisticsOfWordle
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using WordleEngine;
    internal class Program
    {
        //place a dictionary file named words.txt in the same folder as the executable
        const string dictionaryPath = "words.txt";
        static void Main(string[] args)
        {
            ShowStatistics("output.txt");
            RunSimulation(5000, "outputLarge.txt");
        }
        static void ShowStatistics(string dataFile)
        {
            var reader = new StreamReader(dataFile);
            int[] results = new int[7];
            Dictionary<string, int> secondGuesses = new Dictionary<string, int>();
            string? nextLine = reader.ReadLine();
            while (true)
            {
                if (nextLine == null)
                {
                    break;
                }
                var gameInfo = ReadInfo(nextLine);
                int numberOfTurns = gameInfo.Guesses.Count;
                if (gameInfo.GameWon == false)
                {
                    numberOfTurns = 0;
                }
                results[numberOfTurns]++;
                if (gameInfo.Guesses.Count > 1)
                {
                    string secondGuess = gameInfo.Guesses[1];
                    if (secondGuesses.ContainsKey(secondGuess) == false)
                    {
                        secondGuesses.Add(secondGuess, 0);
                    }
                    secondGuesses[secondGuess]++;
                }
                nextLine = reader.ReadLine();
            }
            reader.Dispose();
            int numberOfLosses = results[0];
            int totalNumberOfGames = results.Sum();
            Console.WriteLine($"There were {numberOfLosses} games that the computer lost");
            for (int i = 1; i < 7; i++)
            {
                int numberOfGames = results[i];
                double percentage = 100.0 * (double)numberOfGames / (double)totalNumberOfGames;
                Console.WriteLine($"There were {numberOfGames} games that the computer won in {i} turns, or {percentage}%");
            }
            var myList = secondGuesses.ToList();
            myList.Sort((x, y) => x.Value.CompareTo(y.Value));
            myList.Reverse();
            for (int i = 1; i <= 10; i++)
            {
                var guess = myList[i - 1];
                Console.WriteLine($"Second guess number {i} was {guess.Key} with {guess.Value} appearances");
            }
        }
        static void CleanUpDataFile(string inputFilePath, string outputFilePath)
        {
            string? nextLine;
            StreamReader reader;
            StreamWriter writer = new StreamWriter(outputFilePath);
            try
            {
                reader = new StreamReader(inputFilePath);
            }
            catch
            {
                Console.WriteLine($"Cannot open file at {inputFilePath}");
                return;
            }
            while (true)
            {
                nextLine = reader.ReadLine();
                if (nextLine == null)
                {
                    break;
                }
                nextLine = reader.ReadLine();
                if (nextLine == null)
                {
                    break;
                }
                writer.WriteLine(nextLine);
                nextLine = reader.ReadLine();
                if (nextLine == null)
                {
                    break;
                }
            }
            reader.Dispose();
            writer.Dispose();
        }
        static void RunSimulation(int numberOfTrials, string outputFilePath)
        {
            WordleEngine engine;
            PuzzleGiver giver;
            int dictionarySize = 0;
            try
            {
                using (var reader = new StreamReader(dictionaryPath))
                {
                    engine = new WordleEngine(reader);
                    var words = engine.GetDictionary();
                    dictionarySize = words.Count;
                    giver = new PuzzleGiver(words);
                }
            }
            catch
            {
                Console.WriteLine("Cannot find dictionary file words.txt");
                Console.WriteLine("Please make sure a dictionary named words.txt is inside the same folder as this executable");
                return;
            }
            Console.WriteLine($"There are {dictionarySize} words in the dictionary");
            TextWriter writer = new StreamWriter(outputFilePath);
            for (int i = 0; i < numberOfTrials; i++)
            {
                Console.WriteLine($"Beginning game {i + 1}");
                var outcome = SimulateWordleGame(engine, giver);
                var options = new JsonSerializerOptions();
                string output = JsonSerializer.Serialize<WordleGameInfo>(outcome, options);
                writer.WriteLine(output);
            }
            writer.Close();
        }
        static WordleGameInfo ReadInfo(string serialized)
        {
            WordleGameInfo? output = JsonSerializer.Deserialize<WordleGameInfo>(serialized);
            if (output == null)
            {
                throw new Exception("Unable to deserialize JSON");
            }
            return output;
        }
        static WordleGameInfo SimulateWordleGame(WordleEngine engine, PuzzleGiver giver)
        {
            var output = new WordleGameInfo(giver.TrueAnswer);
            string guess = engine.FirstGuess();
            var outcome = giver.CheckAnswer(guess);
            int wordsRemaining = engine.NumberOfRemainingWords();
            output.Update(guess, outcome, wordsRemaining);
            if (outcome.Win())
            {
                engine.Reset();
                giver.Reset(engine.GetDictionary());
                return output;
            }
            for (int i = 0; i < 5; i++)
            {
                engine.UpdateInfo(guess, outcome);
                guess = engine.NextGuess().BestGuess;
                outcome = giver.CheckAnswer(guess);
                wordsRemaining = engine.NumberOfRemainingWords();
                output.Update(guess, outcome,wordsRemaining);
                if (outcome.Win())
                {
                    engine.Reset();
                    giver.Reset(engine.GetDictionary());
                    return output;
                }
            }
            engine.Reset();
            giver.Reset(engine.GetDictionary());
            return output;
        }
    }
}
