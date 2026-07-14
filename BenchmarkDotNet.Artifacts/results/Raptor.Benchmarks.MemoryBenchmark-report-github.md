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
| Memory_ArrayAccess       | 66.08 μs | 1.779 μs | 0.098 μs |         - |
| Memory_AllocDeallocClean | 14.30 μs | 0.245 μs | 0.013 μs |         - |
| Memory_AllocDeallocChurn | 17.60 μs | 2.613 μs | 0.143 μs |         - |
