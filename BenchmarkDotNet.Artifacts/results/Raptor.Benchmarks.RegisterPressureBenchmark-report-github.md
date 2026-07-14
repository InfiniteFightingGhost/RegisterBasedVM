```

BenchmarkDotNet v0.14.0, Arch Linux
AMD Ryzen 7 260 w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | Mean     | Error       | StdDev   | Allocated |
|----------------------- |---------:|------------:|---------:|----------:|
| Registers_Pressure_4   | 976.2 μs |    84.72 μs |  4.64 μs |         - |
| Registers_Pressure_64  | 762.6 μs | 1,195.36 μs | 65.52 μs |         - |
| Registers_Pressure_128 | 771.7 μs | 1,088.65 μs | 59.67 μs |         - |
