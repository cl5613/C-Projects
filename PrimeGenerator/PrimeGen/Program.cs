/// Prime Number Generator Class using RandomNumberGenerator and BigInteger
/// Author: Chen Lin

using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PrimeGen
{   
    
    static class Program
    {
        
        static void Main(String[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Error: Need two arguments only\n");
                CommandLineHelp();
            }

            else {

                try
                {
                    int bits = int.Parse(args[0]);
                    int count = int.Parse(args[1]);

                    PrintBigIntegerIsPrime(bits, count);

                }
                catch (FormatException)
                {
                    Console.WriteLine("Incorrect arguments: please enter integers only\n");
                    CommandLineHelp();

                }

            }

        }

        /*
         * print help message
         */
        static void CommandLineHelp()
        {
            Console.WriteLine("dotnet run <bits> <count=1>\n             " +
                "- bits - the number of bits of the prime number, this must be a\n               " +
                "multiple of 8, and at least 32 bits.\n             " +
                "- count - the number of prime numbers to generate, defaults to 1 ");
        }

        /*
         * Print random prime big integer given number of counts and bits
         */
        static void PrintBigIntegerIsPrime(int bits, int count)
        {

            if (count < 1 || bits < 32 || bits % 8 != 0)
            {
                Console.WriteLine("Error: check your enterings. Bits should be "
                    + "greater than\n or equal to 32 and a multiple of 8 "
                    + "and count should be at least 1\n");
                CommandLineHelp();
            }

            else
            {
                Console.WriteLine($"BitLength: {bits} bits");
                
                Stopwatch sw = new Stopwatch();
                sw.Start();

                Parallel.For(1, 2, i =>
                {
                    
                    for (; i <= count; i++)   
                    {
                        BigInteger RandomPrimeBigInteger = GeneratePrimeBigInteger(bits);

                        if (i < count)
                        {                           
                            Console.WriteLine($"{i}: {RandomPrimeBigInteger}\n");
                        }

                        if (i == count)
                        {
                            Console.WriteLine($"{i}: {RandomPrimeBigInteger}");
                        }
                                            
                    }

                });
                
                sw.Stop();
                TimeSpan elapsedTime = sw.Elapsed;

                string Time = string.Format("{0:00}:{1:00}:{2:00}.{3:0000000}",
                (int)elapsedTime.TotalHours,
                elapsedTime.Minutes,
                elapsedTime.Seconds,
                elapsedTime.Ticks % TimeSpan.TicksPerSecond);

                Console.WriteLine($"Time to Generate: {Time} ");

            }
        }

        /*
         * Generate one random big integer that is prime
         */
        static BigInteger GeneratePrimeBigInteger(int bits)
        {
            var rng = RandomNumberGenerator.Create();

            BigInteger PrimeBigInteger;
            do
            {
                var bytes = new byte[bits / 8];
                rng.GetBytes(bytes);

                PrimeBigInteger = new BigInteger(bytes);                

            } while (!IsProbablyPrime(PrimeBigInteger));
            
            return PrimeBigInteger;
        }


        /*
         * Miller Rabin's primarily test
         */
        static Boolean IsProbablyPrime(this BigInteger value, int k = 10)
        {
            if (value < 2 || value % 2 == 0)
            {
                return false;
            }

            if (value == 2 || value == 3)
            {
                return true;
            }

            BigInteger d = value - 1;
            int count = 0;

            while (d % 2 == 0)
            {
                d /= 2;
                count += 1;
            }

            byte[] bytes = new byte[value.ToByteArray().LongLength];
            var rng = RandomNumberGenerator.Create();

            BigInteger a = new BigInteger();

            for (int i = 0; i < k; i++)
            {                

                while (a < 2 || value - 2 <= a)
                {
                    rng.GetBytes(bytes);
                    a = new BigInteger(bytes);
                }

                BigInteger x = BigInteger.ModPow(a, d, value);

                if (x == 1 || x == value - 1)
                {
                    continue;
                }

                for (int r = 0; r < count; r++)
                {
                    x = BigInteger.ModPow(x, 2, value);

                    if (x == 1)
                    {
                        return false;
                    }

                    if (x == value - 1)
                    {
                        break;
                    }
                }

                if (x != value - 1)
                {
                    return false;
                }
            }
            return true;
        }

    }

}
