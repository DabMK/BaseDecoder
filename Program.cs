using System;
using System.Linq;
using System.Text;
using System.Collections;

namespace BaseDecoder
{
    internal class Program
    {
        readonly private static StringComparison o = StringComparison.OrdinalIgnoreCase;
        private static string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static int bruteforceMaxBase = 20;

        private static void Main(string[] args)
        {
            // Set Variables
            string data;
            string resultBase;
            string resultFromBase;
            int toBase = 0;
            int fromBase = 0;
            bool ascii = false;
            bool fromAscii = false;

            // CHECKS
            Console.Write(Environment.NewLine);
            if (args.Length < 1)
            {
                Error(true);
            }
            else if (args.Length < 3)
            {
                Error();
            }
            data = args[0]; // Set the data to encode
            if (args.Length > 4) // Set character map
            {
                string newChars = args[4];
                if (newChars.StartsWith("a", o) || newChars.StartsWith('+'))
                {
                    chars += newChars[1..];
                }
                else
                {
                    chars = newChars;
                }
            }

            if (args[2].Equals("ASCII", o)) // Check if output has to be in ASCII
            {
                ascii = true;
            }
            else if (!int.TryParse(args[2], out toBase) || toBase < 2)
            {
                Error();
            }
            if (ascii) { resultBase = "ASCII"; } else { resultBase = $"base {toBase}"; }
            if (args[1].Equals("auto", o)) // Try out the most probable combination of the most probable base
            {
                fromBase = Assignment.BaseIdentifier(data);
                Console.Write($"Base Identified: {fromBase}");
                if (fromBase > 10) // If the identified base is > 10, extracting it would be very complicated (TODO)
                {
                    Console.WriteLine("; this program cannot extract bases > 10");
                    Environment.Exit(1);
                }
                data = Assignment.AutoDecoder(data);
                Console.WriteLine($"Most Probable Combination: {data}\n");
            }
            else if (args[1].Equals("autoall", o)) // Try out every combination of the most probable base
            {
                fromBase = Assignment.BaseIdentifier(data);
                if (fromBase > 10) // If the identified base is > 10, extracting it would be very complicated (TODO)
                {
                    Console.WriteLine($"Base Identified: {fromBase}; this program cannot extract bases > 10");
                    Environment.Exit(1);
                }
                Console.WriteLine($"The program will try every single combination and output them sort them by entropy in case you chose ASCII");
                Console.WriteLine($"Conversion of all cases from identified base {fromBase} to {resultBase}:\n");

                List<string> combinations = Assignment.GetAllCombinations(data);
                if (ascii) // If the output has to be in ASCII, sort results by entropy
                {
                    Dictionary<string, double> entropies = [];
                    foreach (string s in combinations)
                    {
                        string result = Convert(s, fromBase, toBase, ascii);
                        entropies.Add(result, Entropy.Get(result));
                    }
                    IOrderedEnumerable<KeyValuePair<string, double>> sortedEntropies;
                    if (args.Length > 3 && (string.Equals(args[3], "yes", o) || string.Equals(args[3], "y", o)))
                    {
                        sortedEntropies = from entry in entropies orderby entry.Value descending select entry;
                    }
                    else
                    {
                        sortedEntropies = from entry in entropies orderby entry.Value ascending select entry;
                    }
                    for (int i = 0; i < sortedEntropies.Count(); i++)
                    {
                        Console.WriteLine(sortedEntropies.ElementAt(i).Key + " - Entropy: " + sortedEntropies.ElementAt(i).Value);
                    }
                }
                else
                {
                    foreach (string s in combinations)
                    {
                        Console.WriteLine(Convert(s, fromBase, toBase, ascii));
                    }
                }
                Environment.Exit(0);
            }
            else if (args[1].StartsWith("bfl", o) || args[1].StartsWith("bruteforceless", o) || args[1].StartsWith("bruteforcel", o)) // Bruteforce the "fromBase" starting from lowest base possible for that string
            {
                // Get the new max base if requested
                string newBase = string.Empty;
                for (int i = args[1].Length - 1; i > 2; i--)
                {
                    char c = args[1][i];
                    if (char.IsDigit(c)) { newBase = c + newBase; }
                }
                if (!string.IsNullOrEmpty(newBase)) { _ = int.TryParse(newBase, out bruteforceMaxBase); }

                int minBase = Assignment.BaseIdentifier(data);
                Console.WriteLine($"Bruteforcing every base from {minBase} to {bruteforceMaxBase}...\n");
                for (int i = minBase; i <= bruteforceMaxBase; i++)
                {
                    Console.WriteLine($"From base {i} to {resultBase}:\n{Convert(data, i, toBase, ascii)}\n");
                }
                Environment.Exit(0);
            }
            else if (args[1].StartsWith("bf", o) || args[1].StartsWith("bruteforce", o)) // Bruteforce the "fromBase" starting from lowest base possible (2)
            {
                // Get the new max base if requested
                string newBase = string.Empty;
                for (int i = args[1].Length - 1; i > 1; i--)
                {
                    char c = args[1][i];
                    if (char.IsDigit(c)) { newBase = c + newBase; }
                }
                if (!string.IsNullOrEmpty(newBase)) { _ = int.TryParse(newBase, out bruteforceMaxBase); }

                Console.WriteLine($"Bruteforcing every base from 2 to {bruteforceMaxBase}...\n");
                for (int i = 2; i <= bruteforceMaxBase; i++)
                {
                    Console.WriteLine($"From base {i} to {resultBase}:\n{Convert(data, i, toBase, ascii)}\n");
                }
                Environment.Exit(0);
            }
            else if (args[1].Equals("ASCII", o) && !ascii) // Input string is in ASCII
            {
                fromAscii = true;
            }
            else if (!int.TryParse(args[1], out fromBase) || fromBase <= 1)
            {
                Error();
            }
            if (fromAscii) { resultFromBase = "ASCII"; } else { resultFromBase = $"base {fromBase}"; }

            // Normal Execution Code
            Console.WriteLine($"Conversion from {resultFromBase} to {resultBase}:");
            if (fromAscii)
            {
                Console.WriteLine(ConvertFromASCII(data, toBase));
            }
            else
            {
                Console.WriteLine(Convert(data, fromBase, toBase, ascii));
            }
        }


        private static string Convert(string data, int fromBase, int toBase, bool ascii)
        {
            // Character Map Length Check with toBase
            if (toBase > chars.Length)
            {
                Console.Write($"The base {toBase} is higher than the characters map length ({chars.Length}). Are you sure you want to proceed (y/n)? ");
                char response = Console.ReadKey().KeyChar;
                if (response == 'n' || response == 'N')
                {
                    Environment.Exit(0);
                }
                else
                {
                    Console.Write(Environment.NewLine);
                }
            }

            string result = string.Empty;
            if (data.Contains(' '))
            {
                string[] datas = data.Split(' ');
                foreach (string info in datas)
                {
                    result += BaseToBase(info, fromBase, toBase, ascii);
                    if (!ascii) { result += ' '; }
                }
            }
            else
            {
                result = BaseToBase(data, fromBase, toBase, ascii);
            }
            return result;
        }

        private static string ConvertFromASCII(string data, int toBase)
        {
            string base10 = string.Empty;
            for (int i = 0; i < data.Length; i++)
            {
                base10 += ((int)data[i]).ToString();
                if (i != data.Length - 1) { base10 += ' '; }
            }
            return Convert(base10, 10, toBase, false);
        }

        public static string BaseToBase(string data, int inputBase, int outputBase, bool ascii = false)
        {
            long base10 = ToBase10(data, inputBase);
            if (outputBase == 10) { return base10.ToString(); }
            if (ascii)
            {
                return ((char)base10).ToString();
            }
            else
            {
                return FromBase10(base10, outputBase);
            }
        }

        private static string FromBase10(long input, int outputBase)
        {
            string result = string.Empty;
            while (input > 0)
            {
                result = chars[(int)(input % outputBase)] + result;
                input /= outputBase;
            }
            return result;
        }

        private static long ToBase10(string input, int inputBase)
        {
            long result = 0;
            foreach (char c in input.ToUpper())
            {
                result = result * inputBase + chars.IndexOf(c);
            }
            return result;
        }


        private static void Error(bool nonInline = false)
        {
            Console.WriteLine("Usage:\nBaseDecoder <string> <fromBase> <toBase> <inverse> <chars>");
            Console.WriteLine("- Split groups of values with spaces");
            Console.WriteLine("- <inverse> is only needed when using \"autoall\" and the default value is \"no\"");
            Console.WriteLine("- <chars> sets the characters to use; the default ones are \"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ\"");
            Console.WriteLine("-- You can put \"a\" or \"+\" in front of the characters to use in <chars> to add them to the default ones");
            Console.WriteLine("- You can decode directly to ASCII by using \"ASCII\" as the <toBase>");
            Console.WriteLine("- You can decode directly from ASCII by using \"ASCII\" as the <fromBase>");
            Console.WriteLine($"- You can use \"bf\" or \"bruteforce\" as <fromBase> to try to convert from every base from 2 to {bruteforceMaxBase}");
            Console.WriteLine($"-- You can use \"bfl\" or \"bruteforceless\" as <fromBase> to try to convert from every base from the lowest possible for that string to {bruteforceMaxBase}");
            Console.WriteLine($"-- You can put a number at the end of <fromBase> to set the max base for the bruteforce (default is {bruteforceMaxBase})");
            Console.WriteLine("- You can use \"auto\" as <fromBase> to automatically identify the most probable base of the string, trying the most probable combination");
            Console.WriteLine("- You can use \"autoall\" as <fromBase> to automatically identify the most probable base of the string, trying every possible combination");
            Console.WriteLine("-- In that case, results will be sorted by entropy (lowest to highest) and you can use \"yes\" or \"y\" as <inverse> to sort from highest to lowest");
            Console.WriteLine("v0.3   -   Check \"https://github.com/DabMK/BaseDecoder\" for updates");
            if (nonInline) { Console.ReadKey(); }
            Environment.Exit(1);
        }
    }
}