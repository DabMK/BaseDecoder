using System;
using System.Text;

namespace BaseDecoder
{
    internal class Entropy
    {
        public static double Get(string info)
        {
            int range = byte.MaxValue + 1;
            byte[] data = Encoding.ASCII.GetBytes(info);

            long[] counts = new long[range];
            foreach (byte value in data)
            {
                counts[value]++;
            }
            double entropy = 0;
            foreach (long count in counts)
            {
                if (count != 0)
                {
                    double probability = (double)count / data.LongLength;
                    entropy -= probability * Math.Log(probability, range);
                }
            }
            return entropy;
        }
    }
}