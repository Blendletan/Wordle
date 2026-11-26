using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
namespace WordleEngine
{
    public class WordleGameInfo
    {
        public List<string> Guesses { get; set; }
        public List<LetterMatchOutcome[]> Results { get; set; }
        public List<int> PossibilitiesRemaining { get; set; }
        public string TrueAnswer { get; set; }
        public bool GameWon { get; set; }
        public WordleGameInfo(string answer)
        {
            Guesses = new List<string>();
            Results = new List<LetterMatchOutcome[]>();
            PossibilitiesRemaining = new List<int>();
            TrueAnswer = answer;
            GameWon = false;
        }
        public void Update(string guess, byte result,int possibilities)
        {
            Guesses.Add(guess);
            Results.Add(WordleOutcome.GetLetterOutcomes(result));
            PossibilitiesRemaining.Add(possibilities);
            if (WordleOutcome.Win(result))
            {
                GameWon = true;
            }
        }
    }
}
