using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
namespace WordleEngine
{
    public class WordleEngine
    {
        Stopwatch sw;
        public const int wordSize = 5;
        readonly List<string> permissibleWords;
        List<string> possibleWords;
        const string firstGuess = "tares";
        public WordleEngine(TextReader reader)
        {
            sw = new Stopwatch();
            possibleWords = new List<string>();
            permissibleWords = new List<string>();
            string? nextLine = reader.ReadLine();
            if (nextLine == null)
            {
                throw new Exception("Invalid Dictionary file");
            }
            while (nextLine != null)
            {
                if (nextLine.Count() == wordSize)
                {
                    permissibleWords.Add(nextLine.ToLower());
                    possibleWords.Add(nextLine.ToLower());
                }
                nextLine = reader.ReadLine();
            }
        }
        public void Reset()
        {
            possibleWords = new List<string>(permissibleWords);
        }
        public bool WordAdmissible(string input)
        {
            if (permissibleWords.Contains(input))
            {
                return true;
            }
            return false;
        }
        public string FirstGuess()
        {
            return firstGuess;
        }
        public WordleGuessInformation NextGuess()
        {
            WordleGuessInformation output;
            sw.Restart();
            string? bestGuess = null;
            double? bestGuessValue = null;
            string? secondBestGuess = null;
            if (possibleWords.Count == 1)
            {
                sw.Stop();
                output = new WordleGuessInformation(possibleWords[0], null, sw.ElapsedMilliseconds, 1);
                return output;
            }
            foreach (var word in permissibleWords)
            {
                double expectedWordValue = 0;
                var maskProbabilities = GetProbabilites(word);
                foreach (var maskProbability in maskProbabilities)
                {
                    double probabilityOfGuess = maskProbability.Value;
                    double numberOfWordsRuledOut = (1 - probabilityOfGuess) * possibleWords.Count;
                    expectedWordValue += probabilityOfGuess * numberOfWordsRuledOut;
                }
                if (bestGuess == null)
                {
                    bestGuess = word;
                    bestGuessValue = expectedWordValue;
                }
                else if (expectedWordValue > bestGuessValue)
                {
                    secondBestGuess = bestGuess;
                    bestGuess = word;
                    bestGuessValue = expectedWordValue;
                }
            }
            if (bestGuess == null)
            {
                throw new Exception("Couldn't make a guess");
            }
            sw.Stop();
            output = new WordleGuessInformation(bestGuess, secondBestGuess, sw.ElapsedMilliseconds, possibleWords.Count);
            return output;
        }
        public void UpdateInfo(string guessedWord, WordleMask guessOutcome)
        {
            var toRemove = new List<string>();
            foreach (var word in possibleWords)
            {
                WordleMask testOutcome = Compare(guessedWord, word);
                if (guessOutcome.GetHashCode() != testOutcome.GetHashCode())
                {
                    toRemove.Add(word);
                }
            }
            foreach (var word in toRemove)
            {
                possibleWords.Remove(word);
            }
        }
        Dictionary<int,double> GetProbabilites(string guess)
        {
            Dictionary<int, double> outputs = new Dictionary<int, double>();
            foreach(var answer in possibleWords)
            {
                WordleMask mask = Compare(guess, answer);

                if (outputs.ContainsKey(mask.GetHashCode()) == false)
                {
                    outputs.Add(mask.GetHashCode(), 0);
                }
                outputs[mask.GetHashCode()]++;
                outputs[mask.GetHashCode()] /= possibleWords.Count;
            }
            return outputs;
        }

        static WordleMask Compare(string guess, string trueAnswer)
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
            foreach (char c in trueAnswer)
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
                if (guess[i] == repeatedChar && outcomes[i]==LetterMatchOutcome.Yellow)
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