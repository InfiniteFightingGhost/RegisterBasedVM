```

BenchmarkDotNet v0.14.0, Arch Linux
AMD Ryzen 7 260 w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method            | Mean          | Error          | StdDev       | Gen0   | Allocated |
|------------------ |--------------:|---------------:|-------------:|-------:|----------:|
| Lifecycle_Compile |  36,832.69 ns |  23,412.635 ns | 1,283.326 ns | 2.4414 |   20920 B |
| Lifecycle_Verify  |      71.18 ns |       3.756 ns |     0.206 ns | 0.0114 |      96 B |
| Lifecycle_Load    |     122.50 ns |      15.395 ns |     0.844 ns |      - |         - |
| Lifecycle_Execute | 178,801.94 ns | 179,836.145 ns | 9,857.426 ns |      - |         - |
