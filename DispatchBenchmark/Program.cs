using BenchmarkDotNet.Running;
using System;

namespace DispatchBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<DispatchBenchmark>();
            Console.WriteLine("Press a key");
            Console.ReadKey();
        }
    }
}