using System;
using System.Linq;
using System.Text;

namespace BaseDecoder
{
    internal class Assignment
    {
        public static string AutoDecoder(string input) // Assign each number of the identified base to a character (in order of which the character appears)
        {
            StringBuilder result = new(input);
            List<char> usedChars = [];
            int counter = 0;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == ' ') { continue; }

                if (!usedChars.Contains(c))
                {
                    usedChars.Add(c);
                    counter++;
                }
                result[i] = Convert.ToChar(usedChars.IndexOf(c).ToString());
                //Console.Write($"\n{c} -> {Convert.ToChar(counter.ToString())} : {result}"); // DEBUG INFO
            }
            return result.ToString();
        }

        public static List<string> GetAllCombinations(string input) // Get all possible combinations of numbers of the identified base
        {
            List<char> uniqueChars = input.Where(c => c != ' ').Distinct().ToList();
            int uniqueCount = uniqueChars.Count;
            IEnumerable<List<int>> permutations = GetPermutations(Enumerable.Range(0, uniqueCount), uniqueCount);

            List<string> results = [];
            foreach (var permutation in permutations)
            {
                Dictionary<char, int> charToNumberMap = [];
                for (int i = 0; i < uniqueCount; i++)
                {
                    charToNumberMap[uniqueChars[i]] = permutation[i];
                }

                string mappedString = new(input.Select(c => c == ' ' ? ' ' : charToNumberMap[c].ToString()[0]).ToArray());
                results.Add(mappedString);
            }
            return results;
        }

        public static int BaseIdentifier(string input)
        {
            return input.Replace(" ", "").Distinct().Count();
        }

        private static IEnumerable<List<T>> GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) { return list.Select(t => new List<T> { t }); }
            return GetPermutations(list, length - 1).SelectMany(t => list.Where(e => !t.Contains(e)), (t1, t2) => t1.Concat([t2]).ToList());
        }
    }
}