namespace Wordle
{
    using WordleEngine;
    internal class Program
    {
        //place a dictionary file named words.txt in the same folder as the executable
        const string filePath = "words.txt";
        static void Main(string[] args)
        {
            WordleEngine engine;
            using (var reader = new StreamReader(filePath))
            {
                engine = new WordleEngine(reader);
            }
            while (true)
            {
                PlayWordle(engine);
                engine.Reset();
            }
        }
        static void PlayWordle(WordleEngine engine)
        {
            Console.WriteLine($"First guess should be {engine.FirstGuess()}");
            for(int i = 0; i < 5; i++)
            {
                var nextResult = GetNextResult(engine);
                engine.UpdateInfo(nextResult.Item1, nextResult.Item2);
                var guessInfo = engine.NextGuess();
                WriteGuess(guessInfo);
                if (guessInfo.NumberOfPossibilites == 1)
                {
                    break;
                }
            }
            Console.WriteLine("Wordle finished, press ENTER to play again");
            Console.ReadLine();
        }
        static void WriteGuess(WordleGuessInformation information)
        {
            Console.WriteLine($"Time to make guess was {information.ElapsedMilliseconds}");
            Console.WriteLine($"You should guess {information.BestGuess}");
            Console.WriteLine($"Second best guess should be {information.SecondBestGuess}");
            Console.WriteLine($"Number of possibilities is {information.NumberOfPossibilites}");
        }
        static bool VerifyWord(string input, WordleEngine engine)
        {
            return engine.WordAdmissible(input);
        }
        static LetterMatchOutcome? VerifyOutcome(string? input)
        {
            if (input == null)
            {
                return null;
            }
            if (input.ToLower() == "yellow")
            {
                return LetterMatchOutcome.Yellow;
            }
            if (input.ToLower() == "green")
            {
                return LetterMatchOutcome.Green;
            }
            if (input.ToLower() == "grey")
            {
                return LetterMatchOutcome.Grey;
            }
            return null;
        }
        static LetterMatchOutcome GetNextLetterOutcome(int letterNumber)
        {
            LetterMatchOutcome? attemptedOutcome = null;
            while (true)
            {
                Console.WriteLine($"Did letter number {letterNumber + 1} match?  Respond with yellow, green, or grey");
                string? input = Console.ReadLine()?.ToLower();
                attemptedOutcome = VerifyOutcome(input);
                if (attemptedOutcome != null)
                {
                    break;
                }
            }
            return attemptedOutcome.Value;
        }
        static (string, WordleMask) GetNextResult(WordleEngine engine)
        {
            string? attemptedInput = null;
            while (true)
            {
                Console.WriteLine("Please type in the word you guessed and press enter");
                attemptedInput = Console.ReadLine()?.ToLower();
                if (attemptedInput != null)
                {
                    if (VerifyWord(attemptedInput, engine))
                    {
                        break;
                    }
                }
            }
            var outcome = new List<LetterMatchOutcome>();
            for (int i = 0; i < WordleEngine.wordSize; i++)
            {
                outcome.Add(GetNextLetterOutcome(i));
            }
            return (attemptedInput, new WordleMask(outcome));
        }
    }
}