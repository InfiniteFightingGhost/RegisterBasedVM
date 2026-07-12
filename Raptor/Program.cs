using System;

namespace Raptor
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Raptor VM Executable");
            Console.WriteLine("All compiler, disassembler, VM, FFI, panic dump, and script engine unit tests have been migrated to the Raptor.Tests project.");
            Console.WriteLine("To run the test suite, run the following command in the solution root directory:");
            Console.WriteLine("  dotnet test");
        }
    }
}
