using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;

#pragma warning disable SYSLIB1054 // Disabile warning for using DllImport
namespace BaseDecoder
{
    internal class Program
    {
        // Import system APIs
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleOutputCP(uint wCodePageID);
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCP(uint wCodePageID);

        readonly private static StringComparison o = StringComparison.OrdinalIgnoreCase;
        public static string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public static int splitBits = 0;        // Bits of each word to split
        public static bool splitAll = false;    // Split all Inputs
        public static bool split = false;       // Split power-2 bases inputs
        public static bool sam = false;         // Sign&Magnitude
        public static bool comp = false;        // Two's Complement

        private static void Main(string[] args)
        {
            // Set Variables
            string data;
            string resultBase = string.Empty;
            string resultFromBase = string.Empty;
            int toBase = 0;
            int fromBase = 0;
            bool ascii = false;
            bool fromAscii = false;

            // Set Unicode input and output
            SetConsoleOutputCP(65001);
            SetConsoleCP(65001);
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

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

            if (args.Length > 5) // Set character map
            {
                string newChars = args[5];
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
                resultBase = "ASCII";
            }
            else if (args[2].Equals("sam", o) || args[2].Equals("signandmagnitude", o)) // Check if ouput has to be Sign&Magnitude
            {
                toBase = 2;
                sam = true;
                resultBase = "Sign&Magnitude Base 2";
            }
            else if (args[2].Equals("comp", o) || args[2].Equals("twoscomplement", o)) // Check if ouput has to be Two's Complement
            {
                toBase = 2;
                comp = true;
                resultBase = "Two's Complement Base 2";
            }
            else if (!int.TryParse(args[2], out toBase) || toBase < 2) // Set output base
            {
                Error();
            }
            if (string.IsNullOrWhiteSpace(resultBase)) { resultBase = $"base {toBase}"; }

            if (args.Length > 3) // Set bits for each word
            {
                split = true;
                if (args[3].StartsWith("a", o))
                {
                    splitAll = true;
                    _ = int.TryParse(args[3][1..], out splitBits);
                }
                else
                {
                    _ = int.TryParse(args[3], out splitBits);
                }
                if (splitBits < 1) { split = false; }
            }

            if (args[1].StartsWith("autoall", o)) // Try out every combination of the most probable base
            {
                fromBase = Assignment.BaseIdentifier(data);
                Console.WriteLine($"The program will try every single combination and sort them by entropy in case you chose ASCII");
                Console.WriteLine($"Conversion of all cases from identified base {fromBase} to {resultBase}:\n");

                // Check if outputs has to be put in a file
                bool file = args[1].EndsWith("f", o);
                string filePath = string.Empty;
                if (file)
                {
                    filePath = $"{Path.GetTempPath()}basedecoder.txt";
                    if (File.Exists(filePath)) { File.Delete(filePath); }
                }

                List<string> combinations = Assignment.GetAllCombinations(data);
                if (ascii) // If the output has to be in ASCII, sort results by entropy
                {
                    Dictionary<string, double> entropies = [];
                    foreach (string s in combinations)
                    {
                        string result = Convert(s, fromBase, toBase, ascii);
                        if (entropies.ContainsKey(result)) { continue; }
                        entropies.Add(result, Entropy.Get(result));
                    }
                    IOrderedEnumerable<KeyValuePair<string, double>> sortedEntropies;
                    if (args.Length > 4 && (string.Equals(args[4], "yes", o) || string.Equals(args[4], "y", o))) // Check if to sort results descending or ascending
                    {
                        sortedEntropies = from entry in entropies orderby entry.Value descending select entry;
                    }
                    else
                    {
                        sortedEntropies = from entry in entropies orderby entry.Value ascending select entry;
                    }
                    for (int i = 0; i < sortedEntropies.Count(); i++)
                    {
                        string currentResult = sortedEntropies.ElementAt(i).Key + " - Entropy: " + sortedEntropies.ElementAt(i).Value;
                        Console.WriteLine(currentResult);
                        if (file) { File.AppendAllText(filePath, currentResult + Environment.NewLine); }
                    }
                }
                else
                {
                    foreach (string s in combinations)
                    {
                        string currentResult = Convert(s, fromBase, toBase, ascii);
                        Console.WriteLine(currentResult);
                        if (file) { File.AppendAllText(filePath, currentResult + Environment.NewLine); }
                    }
                }
                if (file) { Console.WriteLine($"\nThe results were put in the file \"{filePath}\""); }
                Environment.Exit(0);
            }
            else if (args[1].StartsWith("auto", o)) // Try out the most probable combination of the most probable base
            {
                fromBase = Assignment.BaseIdentifier(data);
                Console.Write($"Base Identified: {fromBase}");
                data = Assignment.AutoDecoder(data);
                Console.WriteLine($"\nMost Probable Combination: {data}\n");
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
                int bruteforceMaxBase = chars.Length;
                if (!string.IsNullOrEmpty(newBase)) { _ = int.TryParse(newBase, out bruteforceMaxBase); }

                int minBase = Assignment.MinimumBase(data);
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
                int bruteforceMaxBase = chars.Length;
                if (!string.IsNullOrEmpty(newBase)) { _ = int.TryParse(newBase, out bruteforceMaxBase); }

                Console.WriteLine($"Bruteforcing every base from 2 to {bruteforceMaxBase}...\n");
                for (int i = 2; i <= bruteforceMaxBase; i++)
                {
                    Console.WriteLine($"From base {i} to {resultBase}:\n{Convert(data, i, toBase, ascii)}\n");
                }
                Environment.Exit(0);
            }
            else if (args[1].Equals("sam", o) || args[1].Equals("signandmagnitude", o)) // Input string is in Sign&Magnitude Base2
            {
                fromBase = 2;
                sam = true;
                resultFromBase = "Sign&Magnitude Base 2";
            }
            else if (args[1].Equals("comp", o) || args[1].Equals("twoscomplement", o)) // Input string is in Two's Complement Base2
            {
                fromBase = 2;
                comp = true;
                resultFromBase = "Two's Complement Base 2";
            }
            else if (args[1].Equals("ASCII", o) && !ascii) // Input string is in ASCII
            {
                fromAscii = true;
                resultFromBase = "ASCII";
            }
            else if (!int.TryParse(args[1], out fromBase) || fromBase <= 1)
            {
                Error();
            }
            if (string.IsNullOrWhiteSpace(resultFromBase)) { resultFromBase = $"base {fromBase}"; }

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
            if (toBase > chars.Length) // Check if the base is higher than the character map
            {
                Warning($"The base {toBase} is higher than the characters map length ({chars.Length}). Are you sure you want to proceed (y/n)? ", false);
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

            for (int i = 0; i < data.Length; i++) // Check if the data isn't in the right base
            {
                if (data[i] == ' ' || data[i] == '-') { continue; }
                bool ok = false;
                for (int j = 0; j < fromBase; j++)
                {
                    if (data[i] == chars[j])
                    {
                        ok = true;
                        break;
                    }
                }
                if (!ok)
                {
                    Warning($"The input data contains \"{data[i]}\", that according to the current character map isn't part of the base {fromBase}. Results are gonna be inaccurate. Are you sure you want to proceed (y/n)? ", false);
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
                int digits = (int)Math.Ceiling(splitBits * Math.Log(2, fromBase));
                if (split && (IsPowerOf2(fromBase) || splitAll) && data.Length >= (2 * digits)) // Check if the starting base is a power of 2 and if there are enough digits to split
                { // Auto split chunks of data every "splitBits" (converted in "digits" for every base) bit for power-2 base decoding
                    var sb = new StringBuilder();
                    string semi = "";

                    for (int i = 0; i < data.Length; i++)
                    {
                        semi += data[i];
                        if ((i + 1) % digits == 0 || i == data.Length - 1)
                        {
                            sb.Append(BaseToBase(semi, fromBase, toBase, ascii));
                            semi = "";
                        }
                    }
                    result = sb.ToString();
                }
                else
                {
                    result = BaseToBase(data, fromBase, toBase, ascii);
                }
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
            long base10;
            if (inputBase == 10) // Skip base10 conversion if input base is already 10
            {
                if (!long.TryParse(data, out base10)) { Error(false, "Input isn't in base 10!"); } // Throw error if the input isn't actually in base10
            }
            else
            {
                base10 = ToBase10(data, inputBase);
            }
            if (outputBase == 10) { return base10.ToString(); } // Immediately return result if requested output base is 10

            if (ascii) // Check if has to oputput ascii or not
            {
                return ((char)base10).ToString();
            }
            else
            {
                return FromBase10(base10, outputBase);
            }
        }

        private static string FromBase10(long input, int outputBase) // TODO: Sign&Magnitude and Two's Complement encoding broken when it needs to add more bits
        {
            string result = string.Empty;
            int bitWidth = 0;
            int hasToAddMSB = 0; // 0 = No, 1 = Negative, 2 = Positive
            bool hasToBeComplemented = false;
            if (input == 0) { return "0"; }

            if (outputBase == 2)
            {
                bitWidth = GetMinBits(input);
                if (sam) // Encode in Sign&Magnitude
                {
                    if (input < 0)
                    {
                        hasToAddMSB = 1;
                    }
                    else
                    {
                        hasToAddMSB = 2;
                    }
                }
                else if (comp) // Encode in Two's Complement
                {
                    if (input < 0)
                    {
                        input = -input; // Get the opposite of the input
                        hasToBeComplemented = true;
                    }
                    else
                    {
                        hasToAddMSB = 2;
                    }
                }
            }

            while (input > 0)
            {
                result = chars[(int)(input % outputBase)] + result;
                input /= outputBase;
            }

            if (outputBase == 2)
            {
                int targetBits = bitWidth;
                if (hasToBeComplemented)
                {
                    string invertedResult = string.Empty;

                    foreach (char c in result) { invertedResult += (c == '0') ? '1' : '0'; }
                    string final = System.Convert.ToString(System.Convert.ToInt64(invertedResult, 2) + 1, 2);
                    return final.PadLeft(targetBits, '0');
                }
                if (hasToAddMSB != 0)
                {
                    if (hasToAddMSB == 1) { result = "1" + result; }
                    else { result = "0" + result; }

                    return result.PadLeft(targetBits, '0');
                }
                return result.PadLeft(targetBits, '0');
            }
            return result;
        }

        private static long ToBase10(string input, int inputBase)
        {
            bool negative = false;
            if (inputBase == 2)
            {
                char msb = input[0];
                if (sam) // Decode from Sign&Magnitude
                {
                    if (msb == '1')
                    {
                        negative = true;
                    }
                    input = input[1..]; // Remove msb
                }
                else if (comp) // Decode from Two's Complement
                {
                    if (msb == '1')
                    {
                        string inputm1 = System.Convert.ToString(System.Convert.ToInt64(input, 2) - 1, 2); // Remove 1 from input
                        string invertedInputm1 = string.Empty; foreach (char c in inputm1) { if (c == '0') { invertedInputm1 += "1"; } else { invertedInputm1 += "0"; } } // Invert Bits
                        negative = true; // Mark the number as negative
                        input = invertedInputm1;
                    }
                }
            }

            long result = 0;
            foreach (char c in input.ToUpper())
            {
                result = result * inputBase + chars.IndexOf(c);
            }
            if (negative) { result = -result; }
            return result;
        }


        private static void Warning(string msg, bool writeline = true)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            if (writeline) { Console.WriteLine($"[WARNING] - {msg}"); } else { Console.Write($"[WARNING] - {msg}"); }
            Console.ForegroundColor = oldColor;
        }

        private static void Error(bool nonInline = false, string errorMsg = "")
        {
            if (!string.IsNullOrWhiteSpace(errorMsg))
            {
                ConsoleColor oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] - {errorMsg}");
                Console.ForegroundColor = oldColor;
            }
            Console.WriteLine("Usage:\nBaseDecoder <string> <fromBase> <toBase> <bits> <inverse> <chars>");
            Console.WriteLine("- Split groups of values with spaces or use <bits>");
            Console.WriteLine("- <bits> lets you choose how many bits to use for each word if you don't split with spaces and if the input base is a power of 2 (usually they're split every 7 or 8 bits)");
            Console.WriteLine("-- By default strings are not split, and you can use \"0\" as <bits> to tell the program to not split data");
            Console.WriteLine("-- You can put a \"a\" in front of <bits> to split every base (not oly powers of 2) with the chosen amount of bits (Make sure that the input data is 0-padded)");
            Console.WriteLine("- <inverse> is only needed when using \"autoall\" and the default value is \"no\"");
            Console.WriteLine("- <chars> sets the characters to use; the default ones are \"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ\"");
            Console.WriteLine("-- You can put \"a\" or \"+\" in front of the characters to use in <chars> to add them to the default ones");
            Console.WriteLine("- You can decode directly to ASCII by using \"ASCII\" as the <toBase>");
            Console.WriteLine("- You can decode directly from ASCII by using \"ASCII\" as the <fromBase>");
            Console.WriteLine($"- You can use \"bf\" or \"bruteforce\" as <fromBase> to try to convert from every base from 2 to the max for the chosen characters");
            Console.WriteLine($"-- You can use \"bfl\" or \"bruteforceless\" as <fromBase> to try to convert from every base from the lowest possible for that string to the max for the chosen characters");
            Console.WriteLine($"-- You can put a number at the end of <fromBase> when using bruteforce to set the max base for the bruteforce (default is the max for the chosen characters)");
            Console.WriteLine("- You can use \"auto\" as <fromBase> to automatically identify the most probable base of the string, trying the most probable combination");
            Console.WriteLine("- You can use \"autoall\" as <fromBase> to automatically identify the most probable base of the string, trying every possible combination");
            Console.WriteLine("-- You can use \"autoallf\" as <fromBase> to write the results in a file that will be told to you at the end");
            Console.WriteLine("-- Results will be sorted by entropy (ascending) and you can use \"yes\" or \"y\" as <inverse> to sort it descending");
            Console.WriteLine("- You can use \"sam\" as <fromBase> or <toBase> to decode or encode from Base2 through Sign&Magnitude"); // TODO encoding:    BROKEN (see above)
            Console.WriteLine("- You can use \"comp\" as <fromBase> or <toBase> to decode or encode from Base2 through Two's Complement"); // TODO encoding: BROKEN (see above)
            Console.WriteLine("- You can use \"fixed\" as <fromBase> to decode from Base2 through Fixed Point"); // TODO
            Console.WriteLine("- You can use \"floating\" as <fromBase> to decode from Base2 through Floating Point"); // TODO
            Console.WriteLine("v0.9   -   Check \"https://github.com/DabMK/BaseDecoder\" for updates");
            if (nonInline) { Console.ReadKey(); }
            Environment.Exit(1);
        }


        // HELPER FUNCTIONS

        private static bool IsPowerOf2(int input)
        {
            return (input & (input - 1)) == 0;
        }

        private static int GetMinBits(long value)
        {
            value = Math.Abs(value);
            int bits = 1;
            while ((1L << bits) <= value) bits++;
            return bits + 1; // +1 for sign / safety
        }
    }
}