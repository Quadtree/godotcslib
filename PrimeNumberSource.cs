using System.Collections.Generic;
using System.Linq;
using Godot;

public static class PrimeNumberSource
{
    private static List<ulong> Primes = new List<ulong>(new ulong[] { 1 });

    public static ulong GetPrimeAtIdx(int idx)
    {
        while (idx >= Primes.Count)
        {
            GenerateAnotherPrime();
        }

        return Primes[idx];
    }

    private static void GenerateAnotherPrime()
    {
        ulong test = Primes.Last() + 1;

        while (true)
        {
            bool isPrime = true;

            for (ulong i = 2; i < test; ++i)
            {
                if (test % i == 0)
                {
                    isPrime = false;
                    break;
                }
            }

            if (isPrime)
            {
                //GD.Print($"Added {test} as a prime");
                Primes.Add(test);
                break;
            }

            test++;
        }
    }
}