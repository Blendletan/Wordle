using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordleEngine
{
    public static class WordleOutcome
    {
        const int winningMask = 242;
        public const int NumberOfMasks = 243;
        public static byte GetMask(LetterMatchOutcome[] input)
        {
            int output = 0;
            byte placeMultiplier = 1;
            for (int i = 0; i < GlobalConstants.WordSize; i++)
            {
                var nextDigit = (int)input[i];
                output += nextDigit * placeMultiplier;
                placeMultiplier *= 3;
            }
            return (byte)output;
        }
        public static LetterMatchOutcome[] GetLetterOutcomes(byte input)
        {
            var output = new LetterMatchOutcome[GlobalConstants.WordSize];
            for (int i = 0; i < GlobalConstants.WordSize; i++)
            {
                int nextDigit = input % 3;
                var matchOutcome = (LetterMatchOutcome)nextDigit;
                output[i]=matchOutcome;
                input /= 3;
            }
            return output;
        }
        public static byte Compare(string guess,string trueAnswer)
        {
            //match green
            Span<bool> isUsed = stackalloc bool[GlobalConstants.WordSize];
            isUsed.Clear();
            int output = 0;
            int placeMultiplier = 1;
            for(int i=0;i<GlobalConstants.WordSize;i++, placeMultiplier *= 3)
            {
                if (guess[i] == trueAnswer[i])
                {
                    isUsed[i] = true;
                    output += (placeMultiplier * 2);
                }
            }
            placeMultiplier = 1;
            for (int i=0;i<GlobalConstants.WordSize;i++, placeMultiplier *= 3)
            {
                if (guess[i] == trueAnswer[i])
                {
                    continue;
                }
                for (int j=0;j<GlobalConstants.WordSize;j++)
                {
                    if (isUsed[j])
                    {
                        continue;
                    }
                    if (guess[i] == trueAnswer[j])
                    {
                        isUsed[j] = true;
                        output += placeMultiplier;
                        break;
                    }
                }
            }
            return (byte)output;
        }
        public static bool Win(byte input)
        {
            return input == winningMask;
        }
    }
}
