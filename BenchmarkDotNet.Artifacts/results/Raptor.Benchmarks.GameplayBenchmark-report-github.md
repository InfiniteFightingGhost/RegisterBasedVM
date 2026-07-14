```

BenchmarkDotNet v0.14.0, Arch Linux
AMD Ryzen 7 260 w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                   | Mean     | Error    | StdDev   | Allocated |
|------------------------- |---------:|---------:|---------:|----------:|
| Gameplay_EcsUpdate       | 25.31 μs | 1.386 μs | 0.076 μs |         - |
| Gameplay_GridPathfinding | 16.28 μs | 2.977 μs | 0.163 μs |         - |
| Gameplay_DialogueTree    | 88.56 μs | 5.323 μs | 0.292 μs |         - |
| Gameplay_InventorySort   | 58.63 μs | 3.968 μs | 0.217 μs |         - |
