using System;
using System.IO;
using Raptor;
using Raptor.Attributes;
using Raptor.StdLib;

namespace Examples
{
    /// <summary>
    /// Custom Host Module exposed to RaptorScript via FFI.
    /// Methods decorated with [RaptorMethod] are callable directly inside .rapt scripts.
    /// </summary>
    [RaptorModule("game")]
    public static class GameHostModule
    {
        [RaptorMethod("get_dt")]
        public static double GetDeltaTime() => 0.016;

        [RaptorMethod("log")]
        public static void LogValue(double value)
        {
            Console.WriteLine($"[Host Engine Log]: {value}");
        }
    }

    /// <summary>
    /// Boilerplate example showing how to initialize Raptor VM, register FFI host tables,
    /// compile high-level RaptorScript (.rapt), and execute scripts in a C# host application.
    /// </summary>
    public class RaptorHostTemplate
    {
        public static void Main()
        {
            Console.WriteLine("--- Initializing Raptor Engine Host ---");

            // 1. Initialize ScriptEngine
            using var engine = new ScriptEngine();

            // 2. Register standard modules (Math, Peripherals) and custom game host module
            var hostTable = new FFIHostTable();
            hostTable.RegisterModule<RaptorMath>();
            hostTable.RegisterModule<RaptorPeripherals>();
            hostTable.RegisterModule<GameHostModule>();

            engine.RegisterHostTable(hostTable);

            // 3. Define RaptorScript code (.rapt)
            string raptorScriptCode =
                @"
                var dt = game.get_dt();
                var radius = 10.0;
                var area = math.pi() * math.pow(radius, 2.0);

                game.log(area);

                for (var i = 0; i < 3; i++) {
                    var val = i * dt;
                    game.log(val);
                }
            ";

            try
            {
                // 4. Compile script into an optimized register VMChunk
                VMChunk chunk = engine.CompileRaptorScript(raptorScriptCode);

                // 5. Execute chunk with zero GC allocations
                ExecutionResult result = engine.Execute(chunk);

                if (result.Success)
                {
                    Console.WriteLine($"[Execution Succeeded] Return Value: {result.ResultValue}");
                }
                else
                {
                    Console.WriteLine($"[Runtime Error]: {result.ErrorMessage}");
                }
            }
            catch (CompileException ex)
            {
                Console.WriteLine($"[Compilation Error]: {ex.Message}");
            }
        }
    }
}
