```

BenchmarkDotNet v0.14.0, Arch Linux
AMD Ryzen 7 260 w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean        | Error       | StdDev    | Gen0   | Gen1   | Allocated |
|------------------------------ |------------:|------------:|----------:|-------:|-------:|----------:|
| Verifier_Scale_100            |    290.2 ns |    61.82 ns |   3.39 ns | 0.0305 |      - |     256 B |
| Verifier_Scale_1000           |  2,679.4 ns |    52.47 ns |   2.88 ns | 0.2441 |      - |    2048 B |
| Verifier_Scale_10000          | 26,751.1 ns | 2,247.05 ns | 123.17 ns | 2.3804 | 0.0305 |   20048 B |
| Verifier_Safety_InvalidJump   |  1,368.0 ns |   237.20 ns |  13.00 ns | 0.0648 |      - |     552 B |
| Verifier_Safety_InvalidMemory |  1,472.6 ns |   200.84 ns |  11.01 ns | 0.0820 |      - |     688 B |
