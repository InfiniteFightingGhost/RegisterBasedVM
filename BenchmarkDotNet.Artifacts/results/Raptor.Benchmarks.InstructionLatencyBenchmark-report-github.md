```

BenchmarkDotNet v0.14.0, Arch Linux
AMD Ryzen 7 260 w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Benchmark_Baseline | 153.2 μs |   5.09 μs |  0.28 μs |  1.00 |    0.00 |         - |          NA |
| Benchmark_Add      | 223.7 μs | 242.89 μs | 13.31 μs |  1.46 |    0.08 |         - |          NA |
| Benchmark_Sub      | 230.9 μs |  11.82 μs |  0.65 μs |  1.51 |    0.00 |         - |          NA |
| Benchmark_Mul      | 208.8 μs | 242.20 μs | 13.28 μs |  1.36 |    0.08 |         - |          NA |
| Benchmark_Div      | 225.1 μs | 295.69 μs | 16.21 μs |  1.47 |    0.09 |         - |          NA |
| Benchmark_Sqrt     | 210.4 μs | 313.91 μs | 17.21 μs |  1.37 |    0.10 |         - |          NA |
| Benchmark_Fisr     | 427.2 μs |  30.04 μs |  1.65 μs |  2.79 |    0.01 |         - |          NA |
| Benchmark_Rand     | 263.4 μs |  12.18 μs |  0.67 μs |  1.72 |    0.00 |         - |          NA |
| Benchmark_Loadc    | 181.1 μs |  24.50 μs |  1.34 μs |  1.18 |    0.01 |         - |          NA |
| Benchmark_Move     | 180.1 μs |   5.00 μs |  0.27 μs |  1.18 |    0.00 |         - |          NA |
| Benchmark_Jump     | 239.1 μs |  16.42 μs |  0.90 μs |  1.56 |    0.01 |         - |          NA |
