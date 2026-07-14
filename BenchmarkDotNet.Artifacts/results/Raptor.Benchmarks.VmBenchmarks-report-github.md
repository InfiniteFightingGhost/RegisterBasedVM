```

BenchmarkDotNet v0.14.0, Arch Linux
AMD Ryzen 7 260 w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean           | Error         | StdDev      | Gen0     | Allocated |
|------------------------------- |---------------:|--------------:|------------:|---------:|----------:|
| Benchmark_Fibonacci            |       139.7 ns |       9.31 ns |     0.51 ns |        - |         - |
| Benchmark_MonteCarlo           | 1,842,739.9 ns | 161,918.55 ns | 8,875.30 ns |        - |         - |
| Benchmark_Perceptron           |   189,750.9 ns |  18,959.64 ns | 1,039.24 ns |        - |         - |
| Benchmark_RayTracerSingleFrame |     8,160.1 ns |     361.53 ns |    19.82 ns |        - |         - |
| Benchmark_FfiDirectBind        |    49,977.5 ns |   4,917.51 ns |   269.55 ns |        - |         - |
| Benchmark_FfiTypedWrapper      |    62,125.0 ns |   2,098.34 ns |   115.02 ns |        - |         - |
| Benchmark_InternalCall         |    82,586.3 ns |  43,882.29 ns | 2,405.34 ns |        - |         - |
| Benchmark_FfiFallback          |   759,439.5 ns |  72,407.36 ns | 3,968.89 ns | 171.8750 | 1440001 B |
| Benchmark_PhysicsMovement      |   188,850.9 ns |  22,622.24 ns | 1,240.00 ns |        - |         - |
| Benchmark_CombatDamage         |       866.3 ns |     164.71 ns |     9.03 ns |        - |         - |
