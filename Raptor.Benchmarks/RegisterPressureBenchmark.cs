using System;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using Raptor;

namespace Raptor.Benchmarks;

[MemoryDiagnoser]
public class RegisterPressureBenchmark
{
    private VirtualMachine _vm = null!;
    private VMChunk _reg4Chunk = null!;
    private VMChunk _reg64Chunk = null!;
    private VMChunk _reg128Chunk = null!;

    private const int Epochs = 3000;

    [GlobalSetup]
    public void Setup()
    {


        _vm = new VirtualMachine();
        var engine = new ScriptEngine();

        // 1. Registers 4: perform 128 additions using only r0 - r3
        var sb4 = new StringBuilder();
        sb4.AppendLine($"DEFINE epochs {Epochs}");
        sb4.AppendLine("DEFINE i r4"); // Loop counter uses r4
        sb4.AppendLine("LOADC r0 1.0");
        sb4.AppendLine("LOADC r1 2.0");
        sb4.AppendLine("LOADC r2 3.0");
        sb4.AppendLine("LOADC r3 4.0");
        sb4.AppendLine("LOADC i 0");
        sb4.AppendLine("loop:");
        for (int k = 0; k < 32; k++)
        {
            sb4.AppendLine("    ADD r0 r1 r2");
            sb4.AppendLine("    ADD r3 r0 r1");
            sb4.AppendLine("    ADD r2 r3 r0");
            sb4.AppendLine("    ADD r1 r2 r3");
        }
        sb4.AppendLine("    FOR i epochs 1 < loop");
        sb4.AppendLine("HALT");
        _reg4Chunk = engine.Compile(sb4.ToString());

        // 2. Registers 64: perform 128 additions using r0 - r63
        var sb64 = new StringBuilder();
        sb64.AppendLine($"DEFINE epochs {Epochs}");
        sb64.AppendLine("DEFINE i r64"); // Loop counter uses r64
        for (int r = 0; r < 64; r++)
        {
            sb64.AppendLine($"LOADC r{r} {(double)r}");
        }
        sb64.AppendLine("LOADC i 0");
        sb64.AppendLine("loop:");
        for (int k = 0; k < 2; k++) // 2 passes * 64 = 128 additions
        {
            for (int r = 0; r < 64; r++)
            {
                int rA = r;
                int rB = (r + 1) % 64;
                int rC = (r + 2) % 64;
                sb64.AppendLine($"    ADD r{rA} r{rB} r{rC}");
            }
        }
        sb64.AppendLine("    FOR i epochs 1 < loop");
        sb64.AppendLine("HALT");
        _reg64Chunk = engine.Compile(sb64.ToString());

        // 3. Registers 128: perform 128 additions using r0 - r127
        var sb128 = new StringBuilder();
        sb128.AppendLine($"DEFINE epochs {Epochs}");
        sb128.AppendLine("DEFINE i r128"); // Loop counter uses r128
        for (int r = 0; r < 128; r++)
        {
            sb128.AppendLine($"LOADC r{r} {(double)r}");
        }
        sb128.AppendLine("LOADC i 0");
        sb128.AppendLine("loop:");
        // 1 pass * 128 = 128 additions
        for (int r = 0; r < 128; r++)
        {
            int rA = r;
            int rB = (r + 1) % 128;
            int rC = (r + 2) % 128;
            sb128.AppendLine($"    ADD r{rA} r{rB} r{rC}");
        }
        sb128.AppendLine("    FOR i epochs 1 < loop");
        sb128.AppendLine("HALT");
        _reg128Chunk = engine.Compile(sb128.ToString());
    }

    [Benchmark]
    public void Registers_Pressure_4()
    {
        _vm.LoadProgram(_reg4Chunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Registers_Pressure_64()
    {
        _vm.LoadProgram(_reg64Chunk);
        _vm.RunFast();
    }

    [Benchmark]
    public void Registers_Pressure_128()
    {
        _vm.LoadProgram(_reg128Chunk);
        _vm.RunFast();
    }
}
