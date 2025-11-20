using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace WordleEngine
{
    public class WordleGameInfo
    {
        public List<string> guesses;
        public List<WordleMask> results;
        public string trueAnswer;
        public bool gameWon;
        public WordleGameInfo(string answer)
        {
            guesses = new List<string>();
            results = new List<WordleMask>();
            trueAnswer = answer;
            gameWon = false;
        }
        public void Update(string guess, WordleMask result)
        {
            guesses.Add(guess);
            results.Add(result);
            if (result.Win())
            {
                gameWon = true;
            }
        }
    }
}
