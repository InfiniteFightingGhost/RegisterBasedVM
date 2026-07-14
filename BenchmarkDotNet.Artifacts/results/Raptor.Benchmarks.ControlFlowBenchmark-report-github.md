```

BenchmarkDotNet v0.14.0, Arch Linux
AMD Ryzen 7 260 w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev  | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|--------:|------:|--------:|----------:|------------:|
| Benchmark_PredictableBranch   | 204.6 μs | 117.14 μs | 6.42 μs |  1.00 |    0.04 |         - |          NA |
| Benchmark_UnpredictableBranch | 581.7 μs |  16.26 μs | 0.89 μs |  2.85 |    0.08 |         - |          NA |
