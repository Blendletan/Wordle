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
        public List<int[]> Results { get; set; }
        public List<int> PossibilitiesRemaining { get; set; }
        public string TrueAnswer { get; set; }
        public bool GameWon { get; set; }
        public WordleGameInfo(string answer)
        {
            Guesses = new List<string>();
            Results = new List<int[]>();
            PossibilitiesRemaining = new List<int>();
            TrueAnswer = answer;
            GameWon = false;
        }
        public void Update(string guess, WordleMask result,int possibilities)
        {
            Guesses.Add(guess);
            Results.Add(result.mask.Select(x=>(int)x).ToArray());
            PossibilitiesRemaining.Add(possibilities);
            if (result.Win())
            {
                GameWon = true;
            }
        }
        [JsonConstructor]
        public WordleGameInfo(List<string> guesses, List<int[]> results, List<int> possibilitiesRemaining, string trueAnswer, bool gameWon)
        {
            Guesses = guesses;
            Results = results;
            PossibilitiesRemaining = possibilitiesRemaining;
            TrueAnswer = trueAnswer;
            GameWon = gameWon;
        }
    }
}
