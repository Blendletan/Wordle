namespace StatisticsOfWordle
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using WordleEngine;
    internal class Program
    {
        //place a dictionary file named words.txt in the same folder as the executable
        const string filePath = "words.txt";
        static void Main(string[] args)
        {
            WordleEngine engine;
            PuzzleGiver giver;
            int dictionarySize = 0;
            try
            {
                using (var reader = new StreamReader(filePath))
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
            TextWriter writer = new StreamWriter("output2.txt");
            for (int i = 0; i < 2; i++)
            {
                writer.WriteLine($"Beginning game {i+1}");
                var outcome = SimulateWordleGame(engine, giver);
                var options = new JsonSerializerOptions();
                options.IncludeFields = true;
                string output = JsonSerializer.Serialize<WordleGameInfo>(outcome,options);
                writer.WriteLine(output);
                writer.WriteLine();
            }
            writer.Close();
            StreamReader testReader = new StreamReader("output2.txt");
            testReader.ReadLine();
            string serialized = testReader.ReadLine();
            var result = ReadInfo(serialized);
            Console.WriteLine(result.guesses.Count);
            testReader.Close();
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
            output.Update(guess, outcome);
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
                output.Update(guess, outcome);
                if (outcome.Win())
                {
                    engine.Reset();
                    giver.Reset(engine.GetDictionary());
                    return output;
                }
            }
            return output;
        }
    }
}
