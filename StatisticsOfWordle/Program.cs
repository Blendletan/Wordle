using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using WordleEngine;
using Engine = WordleEngine.WordleEngine;
namespace StatisticsOfWordle
{
    internal class Program
    {
        const string dictionaryFilePath = "official_wordle_all.txt";
        const string commonFilePath = "official_wordle_common.txt";
        const string outputFilePath = "outputLarge.txt";
        const int maxGuesses = 6;
        static void Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--stats")
            {
                ShowStatistics(args[1]);
                return;
            }
            if (args.Length != 0 && !(args.Length == 2 && args[0] == "--game"))
            {
                Console.WriteLine("Usage: [--game answer | --stats file]");
                return;
            }
            try
            {
                using var reader = new StreamReader(dictionaryFilePath);
                using var readerCommon = new StreamReader(commonFilePath);
                var timer = Stopwatch.StartNew();
                var engine = new Engine(reader, readerCommon);
                var answers = engine.GetAnswerDictionary();
                if (answers.Count == 0)
                    throw new InvalidOperationException("The answer dictionary is empty.");
                Console.WriteLine($"Initialized engine in {timer.Elapsed.TotalMilliseconds:F1} ms.");
                // The first guess is identical for every game in this experiment.
                timer.Restart();
                string openingGuess = engine.NextGuess().BestGuess;
                Console.WriteLine($"Opening guess: {openingGuess} ({timer.Elapsed.TotalMilliseconds:F1} ms)");
                if (args.Length == 2)
                {
                    string answer = args[1].ToLowerInvariant();
                    if (!answers.Contains(answer))
                    {
                        Console.WriteLine($"'{answer}' is not in the answer dictionary.");
                        return;
                    }
                    var result = SimulateWordleGame(engine, new PuzzleGiver(answer), openingGuess);
                    PrintGame(result);
                    return;
                }
                RunSimulation(engine, answers, openingGuess, outputFilePath);
                ShowStatistics(outputFilePath);
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
        static SimulationGameInfo SimulateWordleGame(
            Engine engine, PuzzleGiver giver, string openingGuess)
        {
            engine.Reset();
            var result = new SimulationGameInfo { TrueAnswer = giver.TrueAnswer };
            for (int turn = 1; turn <= maxGuesses; turn++)
            {
                string guess = turn == 1 ? openingGuess : engine.NextGuess().BestGuess;
                byte outcome = giver.CheckAnswer(guess);
                engine.UpdateInfo(guess, outcome);
                int remaining = engine.NumberOfRemainingWords();
                if (remaining == 0)
                    throw new InvalidOperationException($"No answers remain for '{giver.TrueAnswer}' after '{guess}'.");
                result.Guesses.Add(guess);
                result.Outcomes.Add(outcome);
                result.WordsRemaining.Add(remaining);
                if (WordleOutcome.Win(outcome))
                {
                    if (remaining != 1)
                        throw new InvalidOperationException("Winning feedback did not leave exactly one answer.");
                    result.GameWon = true;
                    break;
                }
            }
            return result;
        }
        static void RunSimulation(Engine engine, List<string> answers,
            string openingGuess, string outputPath)
        {
            var timer = Stopwatch.StartNew();
            using var writer = new StreamWriter(outputPath);
            for (int i = 0; i < answers.Count; i++)
            {
                var result = SimulateWordleGame(engine, new PuzzleGiver(answers[i]), openingGuess);
                writer.WriteLine(JsonSerializer.Serialize(result));
                if ((i + 1) % 100 == 0 || i + 1 == answers.Count)
                    Console.WriteLine($"Completed {i + 1}/{answers.Count} games ({timer.Elapsed.TotalSeconds:F1} s)");
            }
            Console.WriteLine($"Simulation time: {timer.Elapsed.TotalSeconds:F2} s");
        }
        static void PrintGame(SimulationGameInfo game)
        {
            Console.WriteLine($"Answer: {game.TrueAnswer}");
            for (int i = 0; i < game.Guesses.Count; i++)
            {
                var outcomes = WordleOutcome.GetLetterOutcomes(game.Outcomes[i]);
                Console.WriteLine($"{i + 1}. {game.Guesses[i]}: {string.Join(" ", outcomes)} | {game.WordsRemaining[i]} remaining");
            }
            Console.WriteLine(game.GameWon ? $"Won in {game.Guesses.Count} guesses." : "Lost in six guesses.");
        }
        static void ShowStatistics(string dataFile)
        {
            var games = File.ReadLines(dataFile)
                .Select(line => JsonSerializer.Deserialize<SimulationGameInfo>(line)
                    ?? throw new InvalidOperationException("Unable to deserialize game."))
                .ToList();
            if (games.Count == 0)
            {
                Console.WriteLine("No games found.");
                return;
            }
            var wins = games.Where(g => g.GameWon).ToList();
            var losses = games.Where(g => !g.GameWon).ToList();
            Console.WriteLine($"Games: {games.Count}");
            Console.WriteLine($"Won: {wins.Count} ({100.0 * wins.Count / games.Count:F2}%)");
            Console.WriteLine($"Lost: {losses.Count} ({100.0 * losses.Count / games.Count:F2}%)");
            for (int turn = 1; turn <= maxGuesses; turn++)
            {
                int count = wins.Count(g => g.Guesses.Count == turn);
                Console.WriteLine($"{turn} guesses: {count} ({100.0 * count / games.Count:F2}%)");
            }
            if (wins.Count > 0)
                Console.WriteLine($"Mean guesses (wins only): {wins.Average(g => g.Guesses.Count):F4}");
            Console.WriteLine($"Mean guesses played (including losses): {games.Average(g => g.Guesses.Count):F4}");
            if (losses.Count > 0)
            {
                Console.WriteLine("Unsolved answers:");
                foreach (var game in losses)
                    Console.WriteLine($"{game.TrueAnswer}: {string.Join(", ", game.Guesses)}");
            }
            ShowGuessFrequencies(games, 2);
            ShowGuessFrequencies(games, 3);
        }
        static void ShowGuessFrequencies(List<SimulationGameInfo> games, int turn)
        {
            var reached = games.Where(g => g.Guesses.Count >= turn).ToList();
            Console.WriteLine($"\nMost frequent guesses on turn {turn} ({reached.Count} games):");
            foreach (var group in reached.GroupBy(g => g.Guesses[turn - 1])
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Key).Take(25))
            {
                Console.WriteLine($"{group.Key}: {group.Count()} ({100.0 * group.Count() / reached.Count:F2}%)");
            }
        }
    }
    // New JSON-lines schema, independent of the old, unavailable WordleGameInfo.
    public class SimulationGameInfo
    {
        public string TrueAnswer { get; set; } = "";
        public bool GameWon { get; set; }
        public List<string> Guesses { get; set; } = new List<string>();
        public List<byte> Outcomes { get; set; } = new List<byte>();
        public List<int> WordsRemaining { get; set; } = new List<int>();
    }
}
