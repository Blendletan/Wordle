using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
namespace WordleEngine
{
    public class WordleEngine
    {
        Stopwatch sw;
        readonly string[] permissibleWords;
        bool[] wordIsPossible;
        int numberOfPossibleWords;
        int[,] wordComparisons;
        bool firstGuesmade;
        const string firstGuess = "tares";
        public WordleEngine(TextReader reader)
        {
            sw = new Stopwatch();
            permissibleWords = ReadDictionaryFile(reader).ToArray();
            int numberOfWords = permissibleWords.Length;
            wordIsPossible = new bool[numberOfWords];
            numberOfPossibleWords = numberOfWords;
            wordComparisons = new int[numberOfWords, numberOfWords];
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
                    byte comparison = WordleOutcome.Compare(firstWord, secondWord);
                    wordComparisons[i,j]= comparison;
                }
            }
        }
        public List<string> GetDictionary()
        {
            return new List<string>(permissibleWords);
        }
        public static List<string> ReadDictionaryFile(TextReader reader)
        {
            string? nextLine = reader.ReadLine();
            if (nextLine == null)
            {
                throw new Exception("Invalid Dictionary file");
            }
            var output = new List<string>();
            while (nextLine != null)
            {
                if (nextLine.Count() == GlobalConstants.WordSize)
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
            double? secondBestGuessValue = null;
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
                double expectedWordValue = 0;
                var maskProbabilities = GetProbabilites(i);
                for(int j=0;j<WordleOutcome.NumberOfMasks;j++)
                {
                    var maskProbability = maskProbabilities[j];
                    double probabilityOfGuess = maskProbability;
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
                    secondBestGuessValue = bestGuessValue;
                    bestGuess = permissibleWords[i];
                    bestGuessValue = expectedWordValue;
                }
                else if (secondBestGuess == null || expectedWordValue > secondBestGuessValue)
                {
                    secondBestGuess = permissibleWords[i];
                    secondBestGuessValue = expectedWordValue;
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
        public void UpdateInfo(string guess, byte guessOutcome)
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
            for (int i = 0; i < permissibleWords.Length; i++)
            {
                var word = permissibleWords[i];
                if (wordIsPossible[i] == false)
                {
                    continue;
                }
                var testOutcome = wordComparisons[guessedWordIndex, i];
                if (guessOutcome != testOutcome)
                {
                    wordIsPossible[i] = false;
                    numberOfPossibleWords--;
                }
            }
        }
        double[] GetProbabilites(int guessWordIndex)
        {
            var outputs = new double[WordleOutcome.NumberOfMasks];
            for (int i = 0; i < permissibleWords.Length; i++)
            {
                if (wordIsPossible[i] == false)
                {
                    continue;
                }
                var mask = wordComparisons[guessWordIndex, i];
                outputs[mask]++;
            }
            for(int i=0;i< WordleOutcome.NumberOfMasks; i++)
            {
                outputs[i] /= (double)numberOfPossibleWords;
            }
            return outputs;
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
}