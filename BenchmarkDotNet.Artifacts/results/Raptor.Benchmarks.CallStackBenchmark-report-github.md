```

BenchmarkDotNet v0.14.0, Arch Linux
AMD Ryzen 7 260 w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method             | Mean        | Error     | StdDev    | Gen0     | Allocated |
|------------------- |------------:|----------:|----------:|---------:|----------:|
| CallStack_Depth_10 |   871.68 μs |  5.095 μs |  4.766 μs |        - |         - |
| CallStack_Depth_30 | 2,318.44 μs | 15.071 μs | 14.097 μs |        - |         - |
| Ffi_InternalCall   |    83.96 μs |  1.661 μs |  3.318 μs |        - |         - |
| Ffi_DirectBind     |    49.78 μs |  0.193 μs |  0.181 μs |        - |         - |
| Ffi_TypedWrapper   |    61.52 μs |  0.329 μs |  0.308 μs |        - |         - |
| Ffi_Fallback       |   850.82 μs | 16.915 μs | 16.613 μs | 171.8750 | 1440001 B |
| Ffi_NoWorkDoneCall |    49.56 μs |  0.207 μs |  0.173 μs |        - |         - |
