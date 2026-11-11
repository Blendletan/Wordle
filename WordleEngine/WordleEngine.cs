using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
namespace WordleEngine
{
    public class WordleEngine
    {
        Stopwatch sw;
        public const int wordSize = 5;
        readonly string[] permissibleWords;
        bool[] wordIsPossible;
        int numberOfPossibleWords;
        WordleMask[,] wordComparisons;
        //const string firstGuess = "tares";
        public WordleEngine(TextReader reader)
        {
            sw = new Stopwatch();
            permissibleWords = ReadFile(reader).ToArray();
            int numberOfWords = permissibleWords.Length;
            wordIsPossible = new bool[numberOfWords];
            numberOfPossibleWords = numberOfWords;
            wordComparisons = new WordleMask[numberOfWords, numberOfWords];
            for (int i = 0; i < numberOfWords; i++)
            {
                wordIsPossible[i] = true;
            }
            for (int i = 0; i < numberOfWords; i++)
            {
                for (int j = 0; j < numberOfWords; j++)
                {
                    string firstWord = permissibleWords[i];
                    string secondWord = permissibleWords[j];
                    WordleMask comparison = Compare(firstWord, secondWord);
                    wordComparisons[i, j] = comparison;
                }
            }
        }
        public List<string> GetDictionary()
        {
            return new List<string>(permissibleWords);
        }
        public static List<string> ReadFile(TextReader reader)
        {
            string? nextLine = reader.ReadLine();
            if (nextLine == null)
            {
                throw new Exception("Invalid Dictionary file");
            }
            var output = new List<string>();
            while (nextLine != null)
            {
                if (nextLine.Count() == wordSize)
                {
                    output.Add(nextLine.ToLower());
                }
                nextLine = reader.ReadLine();
            }
            return output;
        }
        public void Reset()
        {
            int numberOfWords = permissibleWords.Length;
            numberOfPossibleWords = numberOfWords;
            for (int i = 0; i < numberOfWords; i++)
            {
                wordIsPossible[i] = true;
            }
        }
        public bool WordAdmissible(string input)
        {
            if (permissibleWords.Contains(input))
            {
                return true;
            }
            return false;
        }
        public WordleGuessInformation NextGuess()
        {
            WordleGuessInformation output;
            sw.Restart();
            string? bestGuess = null;
            double? bestGuessValue = null;
            string? secondBestGuess = null;
            if (numberOfPossibleWords == 1)
            {
                for (int i = 0; i < permissibleWords.Length; i++)
                {
                    if (wordIsPossible[i])
                    {
                        sw.Stop();
                        output = new WordleGuessInformation(permissibleWords[i], secondBestGuess, sw.ElapsedMilliseconds, numberOfPossibleWords);
                        return output;
                    }
                }
            }
            for (int i = 0; i < permissibleWords.Length; i++)
            {
                if (wordIsPossible[i] == false)
                {
                    continue;
                }
                double expectedWordValue = 0;
                var maskProbabilities = GetProbabilites(i);
                foreach (var maskProbability in maskProbabilities)
                {
                    double probabilityOfGuess = maskProbability.Value;
                    double numberOfWordsRuledOut = (1 - probabilityOfGuess) * numberOfPossibleWords;
                    expectedWordValue += probabilityOfGuess * numberOfWordsRuledOut;
                }
                if (bestGuess == null && expectedWordValue > 0)
                {
                    bestGuess = permissibleWords[i];
                    bestGuessValue = expectedWordValue;
                }
                else if (expectedWordValue > bestGuessValue)
                {
                    secondBestGuess = bestGuess;
                    bestGuess = permissibleWords[i];
                    bestGuessValue = expectedWordValue;
                }
                else if (secondBestGuess == null)
                {
                    secondBestGuess = permissibleWords[i];
                }
            }
            if (bestGuess == null)
            {
                throw new Exception("Couldn't make a guess");
            }
            sw.Stop();
            output = new WordleGuessInformation(bestGuess, secondBestGuess, sw.ElapsedMilliseconds, numberOfPossibleWords);
            return output;
        }
        public void UpdateInfo(string guess, WordleMask guessOutcome)
        {
            int guessedWordIndex = -1;
            for (int i=0;i<permissibleWords.Length;i++)
            {
                if (permissibleWords[i]==guess)
                {
                    guessedWordIndex = i;
                    break;
                }
            }
            if (guessedWordIndex == -1)
            {
                throw new Exception("Guessed word isn't in dictionary");
            }
            var toRemove = new List<string>();
            for (int i = 0; i < permissibleWords.Length; i++)
            {
                var word = permissibleWords[i];
                if (wordIsPossible[i] == false)
                {
                    continue;
                }
                var testOutcome = wordComparisons[guessedWordIndex, i];
                if (guessOutcome.GetHashCode() != testOutcome.GetHashCode())
                {
                    wordIsPossible[i] = false;
                    numberOfPossibleWords--;
                }
            }
        }
        Dictionary<int, double> GetProbabilites(int guessWordIndex)
        {
            Dictionary<int, double> outputs = new Dictionary<int, double>();
            for (int i = 0; i < permissibleWords.Length; i++)
            {
                if (wordIsPossible[i] == false)
                {
                    continue;
                }
                var mask = wordComparisons[guessWordIndex, i];
                if (outputs.ContainsKey(mask.GetHashCode()) == false)
                {
                    outputs.Add(mask.GetHashCode(), 0);
                }
                outputs[mask.GetHashCode()]++;
            }
            foreach(var v in outputs)
            {
                outputs[v.Key] /= (double)numberOfPossibleWords;
            }
            return outputs;
        }
        public static WordleMask Compare(string guess, string trueAnswer)
        {
            var outcomes = new List<LetterMatchOutcome>();
            for (int i = 0; i < wordSize; i++)
            {
                char nextGuessChar = guess[i];
                if (trueAnswer.Contains(nextGuessChar) == false)
                {
                    outcomes.Add(LetterMatchOutcome.Grey);
                    continue;
                }
                if (trueAnswer[i] == nextGuessChar)
                {
                    outcomes.Add(LetterMatchOutcome.Green);
                    continue;
                }
                outcomes.Add(LetterMatchOutcome.Yellow);
            }
            TrimRepeatedLetters(guess, trueAnswer, outcomes);
            return new WordleMask(outcomes);
        }
        static void TrimRepeatedLetters(string guess, string trueAnswer, List<LetterMatchOutcome> outcomes)
        {
            var guessHistogram = GenerateLetterHistogram(guess);
            var answerHistogram = GenerateLetterHistogram(trueAnswer);
            foreach (char c in answerHistogram.Keys)
            {
                if (guessHistogram.ContainsKey(c) == false)
                {
                    continue;
                }
                if (guessHistogram[c] > answerHistogram[c])
                {
                    int numberOfYellowsToTrim = guessHistogram[c] - answerHistogram[c];
                    for (int i = 0; i < numberOfYellowsToTrim; i++)
                    {
                        TrimExtraYellow(guess, c, outcomes);
                    }
                }
            }
        }
        static Dictionary<char, int> GenerateLetterHistogram(string input)
        {
            var output = new Dictionary<char, int>();
            foreach (char c in input)
            {
                if (output.ContainsKey(c) == false)
                {
                    output.Add(c, 0);
                }
                output[c]++;
            }
            return output;
        }
        static void TrimExtraYellow(string guess, char repeatedChar, List<LetterMatchOutcome> outcomes)
        {
            for (int i = wordSize - 1; i >= 0; i--)
            {
                if (guess[i] == repeatedChar && outcomes[i] == LetterMatchOutcome.Yellow)
                {
                    outcomes[i] = LetterMatchOutcome.Grey;
                    return;
                }
            }
        }
    }
    public class WordleGuessInformation
    {
        public readonly string BestGuess;
        public readonly string SecondBestGuess;
        public readonly double ElapsedMilliseconds;
        public readonly int NumberOfPossibilites;
        public WordleGuessInformation(string guess, string? backupGuess, double time, int number)
        {
            BestGuess = guess;
            if (backupGuess == null)
            {
                SecondBestGuess = guess;
            }
            else
            {
                SecondBestGuess = backupGuess;
            }
            ElapsedMilliseconds = time;
            NumberOfPossibilites = number;
        }
    }
    public enum LetterMatchOutcome
    {
        Grey,
        Yellow,
        Green
    }
    public class WordleMask
    {
        public readonly LetterMatchOutcome[] mask;
        public WordleMask(List<LetterMatchOutcome> input)
        {
            mask = input.ToArray();
        }
        public bool Win()
        {
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i] != LetterMatchOutcome.Green)
                {
                    return false;
                }
            }
            return true;
        }
        public override int GetHashCode()
        {
            int output = 0;
            for (int i = 0; i < mask.Length; i++)
            {
                int nextDigit = (int)mask[i];
                int placeMultiplier = (int)Math.Pow(3, i);
                output += nextDigit * placeMultiplier;
            }
            return output;
        }
    }
}