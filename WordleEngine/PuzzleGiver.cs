using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordleEngine
{
    public class PuzzleGiver
    {
        public string TrueAnswer { get; private set; }
        public PuzzleGiver(List<string> words)
        {
            TrueAnswer = GenerateAnswer(words);
        }
        static string GenerateAnswer(List<string> possibleWords)
        {
            var rng = new Random();
            int index = rng.Next(possibleWords.Count);
            return possibleWords[index];
        }
        public WordleMask CheckAnswer(string guess)
        {
            return WordleEngine.Compare(guess, TrueAnswer);
        }
        public void Reset(List<string> words)
        {
            TrueAnswer = GenerateAnswer(words);
        }
    }
}
