using System;
using System.Linq;
using System.Text;
using System.Collections;

namespace BaseDecoder
{
    internal class Program
    {
        readonly private static string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        readonly private static StringComparison o = StringComparison.OrdinalIgnoreCase;

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

            // Checks
            if (args.Length < 1)
            {
                Error(true);
            }
            else if (args.Length != 3 && args.Length != 4)
            {
                Error();
            }
            data = args[0];
            if (args[2].Equals("ASCII", o)) // Check if output has to be in ASCII
            {
                ascii = true;
            }
            else if (!int.TryParse(args[2], out toBase) || toBase <= 1)
            {
                Error();
            }
            if (ascii) { resultBase = "ASCII"; } else { resultBase = $"base {toBase}"; }
            if (args[1].Equals("auto", o))
            {
                fromBase = Assignment.BaseIdentifier(data);
                Console.Write($"\nBase Identified: {fromBase}");
                if (fromBase > 10) // If the identified base is > 10, extracting it would be very complicated (TODO)
                {
                    Console.WriteLine("; this program cannot extract bases > 10");
                    Environment.Exit(1);
                }
                data = Assignment.AutoDecoder(data);
                Console.WriteLine($"\nMost Probable Combination: {data}\n");
            }
            else if (args[1].Equals("autoall", o))
            {
                fromBase = Assignment.BaseIdentifier(data);
                if (fromBase > 10) // If the identified base is > 10, extracting it would be very complicated (TODO)
                {
                    Console.WriteLine($"\nBase Identified: {fromBase}; this program cannot extract bases > 10");
                    Environment.Exit(1);
                }
                Console.WriteLine($"\nThe program will try every single combination and output them sort them by entropy in case you chose ASCII");
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
            else if (args[1].Equals("ASCII", o) && !ascii)
            {
                fromAscii = true;
            }
            else if (!int.TryParse(args[1], out fromBase) || fromBase <= 1)
            {
                Error();
            }
            if (fromAscii) { resultFromBase = "ASCII"; } else { resultFromBase = $"base {fromBase}"; }

            // Actual Code
            Console.WriteLine($"Conversion from {resultFromBase} to {resultBase}:\n{Convert(data, fromBase, toBase, ascii, fromAscii)}\n");
        }


        private static string Convert(string data, int fromBase, int toBase, bool ascii, bool fromAscii = false)
        {
            string result = string.Empty;
            if (fromAscii)
            {
                Console.WriteLine("Work in progress..."); // TODO
            }
            else // Decode from ASCII to Base 10 and then from Base 10 to the wanted base
            {
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
            }
            return result;
        }

        public static string BaseToBase(string data, int inputBase, int outputBase, bool ascii = false)
        {
            long base10 = ToBase10(data, inputBase);
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
            Console.WriteLine("Usage:\nBaseDecoder <string> <fromBase> <toBase> <inverse>");
            Console.WriteLine("- <inverse> is only needed when using \"autoall\" and the default value is \"no\"");
            Console.WriteLine("- Split groups of numbers with spaces");
            Console.WriteLine("- You can decode directly to ASCII by putting \"ASCII\" as the <toBase>");
            Console.WriteLine("- You can decode directly from ASCII by putting \"ASCII\" as the <fromBase>");
            Console.WriteLine("- You can put \"auto\" as <fromBase> to automatically identify the base of the string, trying the most probable combination");
            Console.WriteLine("- You can put \"autoall\" as <fromBase> to automatically identify the base of the string, trying every possible combination");
            Console.WriteLine("-- In that case, results will be sorted by entropy (lowest to highest) and you can use \"yes\" or \"y\" as <inverse> to sort from highest to lowest");
            if (nonInline) { Console.ReadKey(); }
            Environment.Exit(1);
        }
    }
}