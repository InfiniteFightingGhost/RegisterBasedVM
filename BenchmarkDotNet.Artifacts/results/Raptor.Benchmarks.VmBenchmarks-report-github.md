```

BenchmarkDotNet v0.14.0, Arch Linux
AMD Ryzen 7 260 w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                         | Mean       | Error   | StdDev  | Allocated |
|------------------------------- |-----------:|--------:|--------:|----------:|
| Benchmark_Fibonacci            |   503.1 μs | 2.05 μs | 1.92 μs |     105 B |
| Benchmark_MonteCarlo           | 1,610.2 μs | 3.10 μs | 2.90 μs |     106 B |
| Benchmark_Perceptron           |   238.2 μs | 3.26 μs | 3.05 μs |     104 B |
| Benchmark_RayTracerSingleFrame |   273.7 μs | 2.93 μs | 2.75 μs |     105 B |
