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
        public static (byte normalOutput, byte reversedOutput) CompareBoth(string guess, string answer)
        {
            int greenPositions = 0;
            int outputA = 0;
            int weightA = 1;
            // Match greens.
            for (int i = 0; i < GlobalConstants.WordSize;i++, weightA *= 3)
            {
                if (guess[i] == answer[i])
                {
                    greenPositions |= 1 << i;
                    outputA += 2 * weightA;
                }
            }
            int outputB = outputA;
            int usedB = greenPositions;
            // Match yellows.
            weightA = 1;
            for (int i = 0; i < GlobalConstants.WordSize;i++, weightA *= 3)
            {
                if ((greenPositions & (1 << i)) != 0)
                {
                    continue;
                }
                int weightB = 1;
                for (int j = 0; j < GlobalConstants.WordSize;j++, weightB *= 3)
                {
                    if ((usedB & (1 << j)) != 0)
                    {
                        continue;
                    }
                    if (guess[i] == answer[j])
                    {
                        usedB |= 1 << j;
                        outputA += weightA;
                        outputB += weightB;
                        break;
                    }
                }
            }
            return ((byte)outputA, (byte)outputB);
        }
        public static byte Compare(string guess, string answer)
        {
            int greenPositions = 0;
            int usedAnswerPositions = 0;
            int mask = 0;
            int weight = 1;
            // Match all greens first.
            for (int i = 0; i < GlobalConstants.WordSize; i++)
            {
                if (guess[i] == answer[i])
                {
                    greenPositions |= 1 << i;
                    usedAnswerPositions |= 1 << i;
                    mask += 2 * weight;
                }
                weight *= 3;
            }
            // Match yellows against remaining answer letters.
            weight = 1;
            for (int i = 0; i < GlobalConstants.WordSize; i++)
            {
                if ((greenPositions & (1 << i)) == 0)
                {
                    for (int j = 0; j < GlobalConstants.WordSize; j++)
                    {
                        if ((usedAnswerPositions & (1 << j)) != 0)
                            continue;
                        if (guess[i] == answer[j])
                        {
                            usedAnswerPositions |= 1 << j;
                            mask += weight;
                            break;
                        }
                    }
                }
                weight *= 3;
            }
            return (byte)mask;
        }
        public static bool Win(byte input)
        {
            return input == winningMask;
        }
    }
}
