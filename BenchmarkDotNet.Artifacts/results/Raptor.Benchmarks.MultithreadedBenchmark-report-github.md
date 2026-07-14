```

BenchmarkDotNet v0.14.0, Arch Linux
AMD Ryzen 7 260 w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                | Mean     | Error    | StdDev  | Gen0   | Allocated |
|---------------------- |---------:|---------:|--------:|-------:|----------:|
| Multithreaded_Scale_1 | 184.5 μs |  3.31 μs | 0.18 μs |      - |         - |
| Multithreaded_Scale_4 | 212.9 μs | 44.89 μs | 2.46 μs | 0.2441 |    2386 B |
| Multithreaded_Scale_8 | 324.1 μs | 65.30 μs | 3.58 μs |      - |    3296 B |
