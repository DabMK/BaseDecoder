using System;

namespace BaseDecoder
{
    internal class Program
    {
        readonly private static string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private static void Main(string[] args)
        {
            // Set Variables
            string data;
            string resultBase;
            int toBase = 0;
            int fromBase = 0;
            bool ascii = false;

            // Checks
            if (args.Length != 3)
            {
                Error();
            }
            data = args[0];
            if (args[2].Equals("ASCII", StringComparison.OrdinalIgnoreCase))
            {
                ascii = true;
            }
            else if (!int.TryParse(args[2], out toBase) || toBase <= 1)
            {
                Error();
            }
            if (ascii) { resultBase = "ASCII"; } else { resultBase = $"base {toBase}"; }
            if (args[1].Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                fromBase = Assignment.BaseIdentifier(data);
                data = Assignment.AutoDecoder(data);
                Console.WriteLine($"\nMost Probable Combination: {data}\nBase Identified: {fromBase}\n");
            }
            else if (args[1].Equals("autoall", StringComparison.OrdinalIgnoreCase))
            {
                fromBase = Assignment.BaseIdentifier(data);
                Console.WriteLine($"\nThe program will try every single combination and output them sort them by entropy in case you chose ASCII");
                Console.WriteLine($"Conversion of all cases from base {fromBase} to {resultBase}:\n");

                if (ascii)
                {
                    Dictionary<string, double> entropies = [];
                    foreach (string s in Assignment.GetAllCombinations(data))
                    {
                        string result = Convert(s, fromBase, toBase, ascii);
                        entropies.Add(result, Entropy.Get(result));
                    }
                    var sortedEntropies = from entry in entropies orderby entry.Value ascending select entry;
                    for (int i = 0; i < sortedEntropies.Count(); i++)
                    {
                        Console.WriteLine(sortedEntropies.ElementAt(i).Key + " - Entropy: " + sortedEntropies.ElementAt(i).Value);
                    }
                }
                else
                {
                    foreach (string s in Assignment.GetAllCombinations(data))
                    {
                        Console.WriteLine(Convert(s, fromBase, toBase, ascii));
                    }
                }
                Environment.Exit(0);
            }
            else if (!int.TryParse(args[1], out fromBase) || fromBase <= 1)
            {
                Error();
            }

            // Actual Code
            Console.WriteLine($"Conversion from base {fromBase} to {resultBase}:\n{Convert(data, fromBase, toBase, ascii)}\n");
        }


        private static string Convert(string data, int fromBase, int toBase, bool ascii)
        {
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

        private static string BaseToBase(string data, int inputBase, int outputBase, bool ascii = false)
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


        private static void Error()
        {
            Console.WriteLine("Usage:\nBaseDecoder <string> <fromBase> <toBase> (you can also decode directly to ASCII by putting \"ASCII\" as the <toBase>)");
            Console.WriteLine("- Split more strings with spaces");
            Console.WriteLine("- You can put \"auto\" as <fromBase> to automatically identify the base of the string, trying the most probable combination");
            Console.WriteLine("- You can put \"autoall\" as <fromBase> to automatically identify the base of the string, trying every possible combination");
            Environment.Exit(1);
        }
    }
}