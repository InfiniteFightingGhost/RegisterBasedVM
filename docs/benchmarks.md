# Raptor Performance & Benchmark History

This document records official performance baselines for the Raptor VM & Compiler.

## Benchmark Environment Standard

* **Target Framework:** .NET 10.0
* **Reference Hardware:** AMD Ryzen 7 (Zen 4 Architecture) / Linux x64
* **Compiler Configuration:** Release Mode (`-c Release`), `<Optimize>true</Optimize>`, `<PublishAot>true</PublishAot>`

## Version Baseline History

### v1.0.0-alpha (2026-07-20)

#### 1. Instruction Opcode Latency

| Instruction | Latency (ns) | Notes / Details |
| :--- | :--- | :--- |
| LOADC | 0.89 ns | Constant pool register load |
| SUB | 0.92 ns | Double-precision subtraction |
| MOVE | 1.10 ns | Register-to-register copy |
| MUL | 1.27 ns | Double-precision multiplication |
| DIV | 1.45 ns | Double-precision division |
| SQRT | 1.50 ns | Hardware-accelerated square root |
| ADD | 1.52 ns | Double-precision addition |
| JUMP | 1.53 ns | PC offset branch |
| RAND | 2.43 ns | Bit-shifted Xorshift32 PRNG |
| FISR | 5.68 ns | Fast Inverse Square Root |

#### 2. High-Frequency Gameplay Workloads

| Workload | Execution Time (μs) | Workload Specification |
| :--- | :--- | :--- |
| ECS Entity Update | 20.79 μs | Updates `px`, `py` using velocities across 1,000 entities (20.79 ns / entity). |
| BFS Grid Pathfinding | 13.25 μs | Wavefront path search on 16x16 grid. |
| Dialogue Tree Evaluation | 82.90 μs | Evaluates nested quest state & gold conditions 10,000 times (8.29 ns / eval). |
| Inventory Selection Sort | 49.88 μs | Selection sort ordering 100 loot items by rarity. |

#### 3. Host FFI Call Overhead

| FFI Mechanism | Overhead (ns) | Notes |
| :--- | :--- | :--- |
| Direct Host FFI Call | 4.70 ns | Direct method invocation via sliding register window pointer |
| Typed FFI Wrapper | < 5.00 ns | Reflected attribute method call overhead |

## Running Local Regression Tests

### Run Fast Benchmark Suite

Run PR validation suite (~1-2 min):
```bash
dotnet run --configuration Release --project Raptor.Benchmarks -- --fast
```

### Compare Against Baseline

Run head-to-head branch comparison:
```bash
# Save results on feature branch
dotnet run -c Release --project Raptor.Benchmarks -- --fast > feature_bench.txt

# Switch to main branch to run baseline
git checkout main
dotnet run -c Release --project Raptor.Benchmarks -- --fast > main_bench.txt
```
