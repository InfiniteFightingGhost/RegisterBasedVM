using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Raptor
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Raptor VM Executable");
                Console.WriteLine("Usage:");
                Console.WriteLine("  raptor benchmark <file.rasm>");
                Console.WriteLine("  raptor build <file.rapt>");
                Console.WriteLine("  raptor run <file.rbc>");
                Console.WriteLine("  raptor help");
                return;
            }
            else
            {
                switch (args[0])
                {
                    case "benchmark":
                        string filePath = string.Empty;
                        if (args.Length == 1)
                        {
                            Console.WriteLine("Enter path for <file.rasm>");
                            filePath = Console.ReadLine();
                        }
                        else
                            filePath = args[1];
                        RunBenchmark(filePath);

                        break;
                }
            }
        }

        private static void RunBenchmark(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                Environment.Exit(1);
            }

            string sourceText = File.ReadAllText(filePath);

            // Mute stderr to silence assembler instruction parsing logs
            var originalStdErr = Console.Error;
            Console.SetError(TextWriter.Null);

            // 1. Compile Phase
            var sw = Stopwatch.StartNew();
            var chunk = new VMChunk();
            var assembler = new Assembler(chunk);
            assembler.Parse(sourceText.Split('\n').ToList());
            sw.Stop();
            double compileMs = sw.Elapsed.TotalMilliseconds;

            // 2. Verify Phase
            sw = Stopwatch.StartNew();
            BytecodeVerifier.Verify(chunk, 16 * 1024 * 1024);
            sw.Stop();
            double verifyMs = sw.Elapsed.TotalMilliseconds;

            // 3. Load Phase
            var vm = new VirtualMachine();
            sw = Stopwatch.StartNew();
            vm.LoadProgram(chunk);
            sw.Stop();
            double loadMs = sw.Elapsed.TotalMilliseconds;

            var opcodeCounters = new ulong[48];

            // Warmup JIT compiler for execution and profiling
            vm.RunProfile(opcodeCounters, out _);

            // Reset VM state (both program and registers)
            vm.LoadProgram(chunk);
            for (int i = 0; i < 256; i++)
            {
                vm.SetRegister(i, 0.0);
            }

            // Redirect stdout to silence script PRINT calls during benchmark
            var stdout = Console.Out;
            Console.SetOut(TextWriter.Null);

            // 4. Execute Phase with profiling & allocation tracking
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long startAllocated = GC.GetAllocatedBytesForCurrentThread();
            int startGC0 = GC.CollectionCount(0);
            int startGC1 = GC.CollectionCount(1);
            int startGC2 = GC.CollectionCount(2);

            sw.Restart();
            var result = vm.RunProfile(opcodeCounters, out var totalInstructions);
            sw.Stop();

            long endAllocated = GC.GetAllocatedBytesForCurrentThread();

            // Restore stdout & stderr
            Console.SetOut(stdout);
            Console.SetError(originalStdErr);

            double executeMs = sw.Elapsed.TotalMilliseconds;
            double execSeconds = sw.Elapsed.TotalSeconds;

            long bytesAllocated = Math.Max(0, endAllocated - startAllocated);
            // Subtract 2096 bytes allocated on exit for the double[256] register snapshot (2072B) and StackFrame[0] snapshot (24B)
            long executionAllocations = Math.Max(0, bytesAllocated - 2096);
            int gcCount =
                (GC.CollectionCount(0) - startGC0)
                + (GC.CollectionCount(1) - startGC1)
                + (GC.CollectionCount(2) - startGC2);

            double mips = execSeconds > 0 ? (totalInstructions / (execSeconds * 1000000.0)) : 0;

            // Print report
            Console.WriteLine("Raptor Benchmark");
            Console.WriteLine();
            Console.WriteLine($"Program: {Path.GetFileName(filePath)}");
            Console.WriteLine();
            Console.WriteLine($"Compile:         {compileMs:F2} ms");
            Console.WriteLine($"Verify:          {verifyMs:F2} ms");
            Console.WriteLine($"Load:            {loadMs:F2} ms");
            Console.WriteLine($"Execute:         {executeMs:F2} ms");
            Console.WriteLine();
            Console.WriteLine($"Instructions:    {totalInstructions:N0}");
            Console.WriteLine($"MIPS:            {mips:F0}");
            Console.WriteLine($"Allocations:     {executionAllocations} B");
            Console.WriteLine($"GCs:             {gcCount}");
            Console.WriteLine();
            Console.WriteLine("Top opcodes");
            Console.WriteLine();

            if (totalInstructions > 0)
            {
                var topOpcodes = new List<(OpCode op, ulong count)>();
                for (int i = 0; i < opcodeCounters.Length; i++)
                {
                    if (opcodeCounters[i] > 0)
                    {
                        topOpcodes.Add(((OpCode)i, opcodeCounters[i]));
                    }
                }

                var sorted = topOpcodes.OrderByDescending(o => o.count).Take(5).ToList();
                foreach (var item in sorted)
                {
                    double pct = (double)item.count / totalInstructions * 100.0;
                    Console.WriteLine($"{item.op.ToString().PadRight(8)} {pct:F0}%");
                }
            }
            else
            {
                Console.WriteLine("No instructions executed.");
            }
        }
    }
}
