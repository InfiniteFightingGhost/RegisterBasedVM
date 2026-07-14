# Raptor VM Benchmark Consolidated Report
Generated on: 2026-07-13 17:25:39

## CallStackBenchmark

| Method             | Mean        | Error     | StdDev    | Gen0     | Allocated |
|------------------- |------------:|----------:|----------:|---------:|----------:|
| CallStack_Depth_10 |   871.68 μs |  5.095 μs |  4.766 μs |        - |         - |
| CallStack_Depth_30 | 2,318.44 μs | 15.071 μs | 14.097 μs |        - |         - |
| Ffi_InternalCall   |    83.96 μs |  1.661 μs |  3.318 μs |        - |         - |
| Ffi_DirectBind     |    49.78 μs |  0.193 μs |  0.181 μs |        - |         - |
| Ffi_TypedWrapper   |    61.52 μs |  0.329 μs |  0.308 μs |        - |         - |
| Ffi_Fallback       |   850.82 μs | 16.915 μs | 16.613 μs | 171.8750 | 1440001 B |
| Ffi_NoWorkDoneCall |    49.56 μs |  0.207 μs |  0.173 μs |        - |         - |

## ControlFlowBenchmark

| Method                        | Mean     | Error     | StdDev  | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|--------:|------:|--------:|----------:|------------:|
| Benchmark_PredictableBranch   | 204.6 μs | 117.14 μs | 6.42 μs |  1.00 |    0.04 |         - |          NA |
| Benchmark_UnpredictableBranch | 581.7 μs |  16.26 μs | 0.89 μs |  2.85 |    0.08 |         - |          NA |

## GameplayBenchmark

| Method                   | Mean     | Error    | StdDev   | Allocated |
|------------------------- |---------:|---------:|---------:|----------:|
| Gameplay_EcsUpdate       | 25.31 μs | 1.386 μs | 0.076 μs |         - |
| Gameplay_GridPathfinding | 16.28 μs | 2.977 μs | 0.163 μs |         - |
| Gameplay_DialogueTree    | 88.56 μs | 5.323 μs | 0.292 μs |         - |
| Gameplay_InventorySort   | 58.63 μs | 3.968 μs | 0.217 μs |         - |

## InstructionLatencyBenchmark

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

## LifecycleBenchmark

| Method            | Mean          | Error          | StdDev       | Gen0   | Allocated |
|------------------ |--------------:|---------------:|-------------:|-------:|----------:|
| Lifecycle_Compile |  36,832.69 ns |  23,412.635 ns | 1,283.326 ns | 2.4414 |   20920 B |
| Lifecycle_Verify  |      71.18 ns |       3.756 ns |     0.206 ns | 0.0114 |      96 B |
| Lifecycle_Load    |     122.50 ns |      15.395 ns |     0.844 ns |      - |         - |
| Lifecycle_Execute | 178,801.94 ns | 179,836.145 ns | 9,857.426 ns |      - |         - |

## MemoryBenchmark

| Method                   | Mean     | Error    | StdDev   | Allocated |
|------------------------- |---------:|---------:|---------:|----------:|
| Memory_ArrayAccess       | 66.08 μs | 1.779 μs | 0.098 μs |         - |
| Memory_AllocDeallocClean | 14.30 μs | 0.245 μs | 0.013 μs |         - |
| Memory_AllocDeallocChurn | 17.60 μs | 2.613 μs | 0.143 μs |         - |

## MultithreadedBenchmark

| Method                | Mean     | Error    | StdDev  | Gen0   | Allocated |
|---------------------- |---------:|---------:|--------:|-------:|----------:|
| Multithreaded_Scale_1 | 184.5 μs |  3.31 μs | 0.18 μs |      - |         - |
| Multithreaded_Scale_4 | 212.9 μs | 44.89 μs | 2.46 μs | 0.2441 |    2386 B |
| Multithreaded_Scale_8 | 324.1 μs | 65.30 μs | 3.58 μs |      - |    3296 B |

## RegisterPressureBenchmark

| Method                 | Mean     | Error       | StdDev   | Allocated |
|----------------------- |---------:|------------:|---------:|----------:|
| Registers_Pressure_4   | 976.2 μs |    84.72 μs |  4.64 μs |         - |
| Registers_Pressure_64  | 762.6 μs | 1,195.36 μs | 65.52 μs |         - |
| Registers_Pressure_128 | 771.7 μs | 1,088.65 μs | 59.67 μs |         - |

## VerifierBenchmark

| Method                        | Mean        | Error       | StdDev    | Gen0   | Gen1   | Allocated |
|------------------------------ |------------:|------------:|----------:|-------:|-------:|----------:|
| Verifier_Scale_100            |    290.2 ns |    61.82 ns |   3.39 ns | 0.0305 |      - |     256 B |
| Verifier_Scale_1000           |  2,679.4 ns |    52.47 ns |   2.88 ns | 0.2441 |      - |    2048 B |
| Verifier_Scale_10000          | 26,751.1 ns | 2,247.05 ns | 123.17 ns | 2.3804 | 0.0305 |   20048 B |
| Verifier_Safety_InvalidJump   |  1,368.0 ns |   237.20 ns |  13.00 ns | 0.0648 |      - |     552 B |
| Verifier_Safety_InvalidMemory |  1,472.6 ns |   200.84 ns |  11.01 ns | 0.0820 |      - |     688 B |

## VmBenchmarks

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

