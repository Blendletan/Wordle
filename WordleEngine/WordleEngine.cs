using System.Diagnostics;
namespace WordleEngine
{
    public class WordleEngine
    {
        Stopwatch sw;
        readonly string[] permissibleWords;
        List<int>? possibleWordIndices;
        readonly int numberOfCommonWords;
        byte[,] wordComparisons;
        double[] entropyTable;
        public WordleEngine(TextReader dictionaryReader,TextReader commonReader)
        {
            possibleWordIndices = null;
            sw = new Stopwatch();
            var commonWords = ReadDictionaryFile(commonReader);
            numberOfCommonWords = commonWords.Count;
            var rawPermissibleWords = ReadDictionaryFile(dictionaryReader);
            int numberOfWords = rawPermissibleWords.Count;
            var commonSet = commonWords.ToHashSet();
            var permissibleSet = rawPermissibleWords.ToHashSet();
            if (commonSet.Count != commonWords.Count ||
                permissibleSet.Count != rawPermissibleWords.Count ||
                !commonSet.IsSubsetOf(permissibleSet))
            {
                throw new Exception("Unexpected dictionary");
            }
            permissibleWords = commonWords.Concat(rawPermissibleWords.Where(w => !commonSet.Contains(w))).ToArray();
            wordComparisons = new byte[numberOfWords, numberOfCommonWords];
            entropyTable = new double[numberOfCommonWords + 1];
            entropyTable[0] = 0;
            for (int i = 0; i < numberOfCommonWords; i++)
            {
                entropyTable[i + 1] = (i+1)*Math.Log(i + 1);
            }
            Parallel.For(0, numberOfWords, i =>
            {
                string guess = permissibleWords[i];
                if (i < numberOfCommonWords)
                {
                    wordComparisons[i, i] = 242;
                    for (int j = i + 1; j < numberOfCommonWords; j++)
                    {
                        var (ab, ba) = WordleOutcome.CompareBoth(guess, permissibleWords[j]);
                        wordComparisons[i, j] = ab;
                        wordComparisons[j, i] = ba;
                    }
                }
                else
                {
                    for (int j = 0; j < numberOfCommonWords; j++)
                    {
                        wordComparisons[i, j] = WordleOutcome.Compare(
                            guess, permissibleWords[j]);
                    }
                }
            });
        }
        private bool WordIsPossible(int wordIndex)
        {
            if (wordIndex >= numberOfCommonWords)
            {
                return false;
            }
            if (possibleWordIndices==null)
            {
                return true;
            }
            return possibleWordIndices.BinarySearch(wordIndex) >= 0;
        }
        public List<string> GetAnswerDictionary()
        {
            return permissibleWords.Take(numberOfCommonWords).ToList();
        }
        public int NumberOfRemainingWords()
        {
            return possibleWordIndices?.Count ?? numberOfCommonWords;
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
                if (nextLine.Length == GlobalConstants.WordSize)
                {
                    output.Add(nextLine.ToLowerInvariant());
                }
                nextLine = reader.ReadLine();
            }
            return output;
        }
        public void Reset()
        {
            possibleWordIndices = null;
        }
        public bool WordAdmissible(string input)
        {
            return permissibleWords.Contains(input);
        }
        public WordleGuessInformation NextGuess()
        {
            WordleGuessInformation output;
            int numberOfPossibleWords = numberOfCommonWords;
            if (possibleWordIndices != null)
            {
                numberOfPossibleWords = possibleWordIndices.Count;
            }
            sw.Restart();
            int? bestGuessIndex = null;
            double? bestGuessValue = null;
            int? secondBestGuessIndex = null;
            double? secondBestGuessValue = null;
            if (possibleWordIndices != null)
            {
                if (possibleWordIndices.Any()==false)
                {
                    throw new InvalidOperationException("No possible answers remain.");
                }
                if (possibleWordIndices.Count == 1)
                {
                    sw.Stop();
                    string outputWord = permissibleWords[possibleWordIndices.First()];
                    output = new WordleGuessInformation(outputWord, null, sw.Elapsed.TotalMilliseconds, numberOfPossibleWords);
                    return output;
                }
            }
            Span<int> maskCounts = stackalloc int[WordleOutcome.NumberOfMasks];
            for (int i = 0; i < permissibleWords.Length; i++)
            {
                double score = 0;
                GetMaskCounts(i,maskCounts);
                for(int j=0;j<WordleOutcome.NumberOfMasks;j++)
                {
                    var maskCount = maskCounts[j];
                    double entropy = entropyTable[maskCount];
                    score += entropy;
                }
                if (IsBetterGuess(i,score,bestGuessIndex,bestGuessValue))
                {
                    secondBestGuessIndex = bestGuessIndex;
                    secondBestGuessValue = bestGuessValue;
                    bestGuessIndex = i;
                    bestGuessValue = score;
                }
                else if (IsBetterGuess(i, score, secondBestGuessIndex, secondBestGuessValue))
                {
                    secondBestGuessIndex = i;
                    secondBestGuessValue = score;
                }
            }
            if (bestGuessIndex == null)
            {
                throw new Exception("Couldn't make a guess");
            }
            sw.Stop();
            string bestGuess = permissibleWords[bestGuessIndex.Value];
            string? secondBestGuess = null;
            if (secondBestGuessIndex != null)
            {
                secondBestGuess = permissibleWords[secondBestGuessIndex.Value];
            }
            output = new WordleGuessInformation(bestGuess, secondBestGuess, sw.Elapsed.TotalMilliseconds, numberOfPossibleWords);
            return output;
        }
        bool IsBetterGuess(int candidate, double candidateScore, int? incumbent, double? incumbentScore)
        {
            if (incumbent == null)
            {
                return true;
            }
            if (candidateScore < incumbentScore)
            {
                return true;
            }
            if (candidateScore > incumbentScore)
            {
                return false;
            }
            return WordIsPossible(incumbent.Value) == false && WordIsPossible(candidate);
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
            if (possibleWordIndices==null)
            {
                possibleWordIndices = new List<int>();
                for (int i = 0; i < numberOfCommonWords; i++)
                {
                    var testOutcome = wordComparisons[guessedWordIndex, i];
                    if (guessOutcome == testOutcome)
                    {
                        possibleWordIndices.Add(i);
                    }
                }
            }
            else
            {
                possibleWordIndices.RemoveAll(i => wordComparisons[guessedWordIndex, i] != guessOutcome);
            }
        }
        void GetMaskCounts(int guessWordIndex, Span<int> counts)
        {
            counts.Clear();
            if (possibleWordIndices == null)
            {
                for (int i = 0; i < numberOfCommonWords; i++)
                {
                    counts[wordComparisons[guessWordIndex, i]]++;
                }
            }
            else
            {
                foreach (int i in possibleWordIndices)
                {
                    counts[wordComparisons[guessWordIndex, i]]++;
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
}