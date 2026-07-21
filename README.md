<p align="center">
  <img src="./raptor-banner.svg" alt="Raptor VM & Scripting Language" width="100%">
</p>



<h1 align="center">Raptor VM & Scripting Language</h1>

<p align="center">
  <strong>A high-throughput, zero-allocation, register-based virtual machine and scripting pipeline built for .NET 10.0 game engines and systems.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-blueviolet?style=flat-square" alt=".NET 10.0">
  <img src="https://img.shields.io/badge/Performance-360--660_MIPS-brightgreen?style=flat-square" alt="Performance MIPS">
  <img src="https://img.shields.io/badge/GC-Zero_Managed_Allocations-blue?style=flat-square" alt="Zero GC">
  <img src="https://img.shields.io/badge/FFI_Overhead-%3C_5ns_Direct_Call-orange?style=flat-square" alt="FFI Overhead">
</p>

---

## What is Raptor?

**Raptor** is a complete scripting pipeline consisting of **RaptorScript** (a high-level programming language), an optimizing compiler with source map debugging, a command-line toolchain (CLI), and a register-based virtual machine interpreter written in C# targeting **.NET 10.0**.

Raptor is built specifically for game engine hot loops where garbage collection pauses can ruin frame pacing. The register file lives directly on the thread stack via `stackalloc`, keeping 256 virtual registers warm inside CPU L1 cache. Restricting registers to 64-bit doubles is the deliberate design tradeoff that eliminates heap allocations during interpretation and achieves **360 to 660+ MIPS** on consumer hardware.

## Installation

### .NET (NuGet)
Install the `Raptor.VM` package via .NET CLI:
```bash
dotnet add package Raptor.VM
```

### Unity (Package Manager)
Open Unity's **Package Manager** (`Window -> Package Manager`), click **+**, select **Add package from git URL...**, and enter:
```text
https://github.com/InfiniteFightingGhost/Raptor.git?path=/Raptor
```

---

## Quickstart: Embed Raptor in C#

Add `Raptor.VM` to your project and execute scripts in a few lines of C#:

```csharp
using Raptor;
using Raptor.StdLib;

// 1. Initialize engine and register standard FFI modules
using var engine = new ScriptEngine();
var table = new FFIHostTable();
table.RegisterModule(typeof(RaptorMath));
table.RegisterModule(typeof(RaptorPeripherals));
engine.RegisterHostTable(table);

// 2. Compile high-level RaptorScript into an optimized VM chunk
VMChunk chunk = engine.CompileRaptorScript(@"
    var radius = 5.0;
    var area = math.pi() * math.pow(radius, 2.0);
    peri.print(area);
");

// 3. Execute with zero GC allocations
ExecutionResult result = engine.Execute(chunk);
```

---

## The Raptor Scripting Pipeline

Raptor includes a compiler, CLI toolchain, source-mapping error translator, and VM interpreter:

```mermaid
graph TD
    A[RaptorScript .rapt] -->|Compiler| B[Raptor Assembly .rasm]
    A -->|Source Map| E[Error Translator]
    B -->|Assembler & Verifier| C[Raptor Bytecode .rbc]
    C -->|ScriptWatcher| D[Virtual Machine RunFast]
    D -->|Runtime Error IP| E
```

### 1. High-Level RaptorScript Language (`.rapt`)
Write scripts using a clean, standard syntax supporting variables, loops, branches, nested math, and bitwise logic:
```javascript
// script.rapt
var result = 8 | 4 ^ 2 & 10 == 5 << 1 && 3 || 9;
peri.print(result);

for(var i = 0; i < 10; i++) {
    peri.print(i);
}
```

#### Compiled Output (Raptor Assembly - `.rasm`)
The compiler generates optimized, register-friendly assembly. For example, a simple conditional branch:

```javascript
// RaptorScript (.rapt)
var x = 10;
if (x < 20) {
    peri.print(x);
}
```

Translates directly to:

```assembly
; Raptor Assembly (.rasm)
LOADC r1 10.0            ; Load x (10) into register r1
LT 1 r1 20.0             ; Compare r1 < 20.0 (expected true, skip JUMP if met)
JUMP logic_end           ; Jump past body if comparison is false
CALL peri.print() r1     ; Call FFI print with register r1
logic_end:               ; End of branch
HALT                     ; Stop VM execution
```

### 2. Live Reloading (`ScriptWatcher`)
Engineered for rapid game-design iteration. The thread-safe `ScriptWatcher` monitors script files on disk and automatically recompiles and swaps the execution `VMChunk` on the fly, updating gameplay mechanics, stats, or AI state **without halting the main execution thread or stopping the game loop**.

### 3. Source Mapping & Diagnostics
When a runtime exception occurs, Raptor uses compiler-generated **Source Maps** to translate the execution Instruction Pointer (IP) offset back to the exact line number and source snippet of the original high-level `.rapt` file.

### 4. Auto-Generated Editor Autocomplete
The FFI system automatically generates autocomplete JSON files (`-api.json`) listing all registered host methods, descriptions, signatures, and constants, enabling easy integration with editor extensions and IDEs.

---

## Architectural Trade-offs & Scope

In game loops, scripting languages face a difficult trade-off between raw execution speed, C#/VM marshalling boundary costs, and GC-induced stutters:

| Feature / Metric | MoonSharp | NLua | LuaJIT (Interpreter) | Raptor VM |
| :--- | :--- | :--- | :--- | :--- |
| **Language** | Lua 5.2 | Lua 5.4 | Lua 5.1 | **RaptorScript / Assembly** |
| **Runtime Environment** | Pure C# (Managed) | C# Bindings + Native C | Native C / Assembly | **Pure C# (Unsafe/Managed)** |
| **Instruction Architecture** | Stack-based VM | Stack-based VM | Register-based VM | **Register-based VM** |
| **Execution Performance** | ~10–15 MIPS | ~50–80 MIPS | ~100–150 MIPS (No JIT) | **360–660+ MIPS** |
| **Garbage Collector (GC) pressure** | High (allocates per-instruction) | Low-to-Medium (native heap) | None (native heap) | **Zero Managed GC Allocations** |
| **FFI Call Overhead** | High (reflection/boxing) | Medium (~50–150 ns marshalling) | Low (~10-20 ns call cost) | **Low (< 5 ns direct call cost)** |
| **AOT / IL2CPP Compatibility** | Excellent (Refsafe JIT limits) | Complex (requires native libs) | Broken on iOS/Consoles | **Full (.NET native support)** |
| **Memory Locality** | Managed heap objects | Medium (C-structs) | High (C-structs) | **High (L1 Stack-allocated registers)** |

> [!NOTE]
> *Comparison context:* Unlike general-purpose Lua runtimes that manage dynamic table objects and metatables on the heap, Raptor restricts registers to 64-bit doubles to achieve zero-GC execution in hot game loops. By using a register layout with 256 virtual registers, Raptor cuts instruction dispatch overhead and keeps operands warm in CPU registers or L1 caches.

---

## Performance & Benchmarks

*Captured on an AMD Ryzen 7 (Zen 4 Architecture) running .NET 10.0.1 on Arch Linux.*

### 1. High-Frequency Gameplay Workloads
Realistic game loops written in RaptorScript running inside the virtual machine:

| Benchmark | Timing (μs) | Workload Details |
| :--- | :--- | :--- |
| **ECS Entity Update** | **20.79 μs** | Updates positions (`px`, `py`) using velocities and delta time for **1,000 entities** (20.79 ns per entity!). |
| **BFS Grid Pathfinding** | **13.25 μs** | Executes a wavefront path search on a **16x16 grid** to locate the target node. |
| **Dialogue Condition Tree** | **82.90 μs** | Evaluates a nested quest state and gold balance conditions **10,000 times** (8.29 ns per evaluation). |
| **Inventory Rarity Sort** | **49.88 μs** | Performs an $O(N^2)$ Selection Sort sorting **100 inventory loot items** by rarity. |

### 2. Instruction Latency
Opcode execution latencies inside the hot interpreter loop:

| Instruction | Latency (ns) | Execution Notes |
| :--- | :--- | :--- |
| **LOADC** | 0.89 ns | Load constant into register |
| **SUB** | 0.92 ns | Floating-point subtraction |
| **MOVE** | 1.10 ns | Register-to-register copy |
| **MUL** | 1.27 ns | Floating-point multiplication |
| **DIV** | 1.45 ns | Floating-point division |
| **SQRT** | 1.50 ns | Hardware-accelerated square root |
| **ADD** | 1.52 ns | Floating-point addition |
| **JUMP** | 1.53 ns | Unconditional PC offset branch |
| **RAND** | 2.43 ns | Custom bit-shifted Xorshift32 PRNG |
| **FISR** | 5.68 ns | Double-precision Fast Inverse Square Root |

---

## Architectural Highlights

### 1. Stack-Allocated Register Files & L1 Cache Locality
Interpreter registers are allocated on the local stack using `stackalloc`:
```csharp
double* RegPtr = stackalloc double[256];
```
This forces the VM's register file to remain warm inside the CPU's **L1 Data Cache** (~4 cycle access latency), bypassing managed allocations.

### 2. Fused Loop Control (`FOR` Super-Instruction)
Compiles loop increments, comparisons, and branches into a single two-word `FOR` super-instruction, reducing interpreter loop dispatch overhead by **50%**.

### 3. Zero-Check Array Pointers
Bypasses standard .NET array boundary checks (`IndexOutOfRangeException`) in the interpreter loop by pinning managed bytecode and constant blocks using `fixed` statements and resolving them via raw pointers.

---

## Embedded Raytracer

A custom double-precision 3D raytracer implemented in pure assembly, rendering a camera viewport orbiting a reflective sphere in 8.2 microseconds per frame.

![Orbit Animation](./orbit.gif)

---

## CLI Reference

Raptor includes a Spectre-based command-line interface (`Raptor.Cli`) to manage compile and run tasks.

### 1. Create a New Script
Creates a new `.rapt` script file initialized with the starter template:
```bash
dotnet run -c Release --project Raptor.Cli -- new script.rapt
```
*Options:*
- `-f | --force`: Overwrites the target `.rapt` file if it already exists.

### 2. Run a Script
Compiles, verifies, and runs a RaptorScript (`.rapt`) file:
```bash
dotnet run -c Release --project Raptor.Cli -- run script.rapt
```
*Options:*
- `--no-build`: Bypasses compilation and runs a pre-compiled `.rbc` file directly from the `build/` folder.
- `-a | --omit-assembly`: Omits outputting the intermediate assembly `.rasm` file when building.

### 3. Build / Compile a Script
Compiles high-level code to assembly (`.rasm`) and binary bytecode (`.rbc`), and generates an editor autocomplete metadata schema (`-api.json`):
```bash
dotnet run -c Release --project Raptor.Cli -- build script.rapt
```
*Options:*
- `-a | --omit-assembly`: Omits outputting the intermediate assembly `.rasm` file.
- `-p | --print-ast`: Prints the compiled abstract syntax tree to console.

### 4. Browse Documentation
Opens the online documentation reference page directly in your browser:
```bash
dotnet run -c Release --project Raptor.Cli -- docs
```

---

## Technical Directory

Detailed architectural layouts are located in the [docs/](docs/) and [examples/](examples/) directories:

### Core Architecture & Memory
- [Core Architecture & Calling Conventions](docs/architecture.md): Calling conventions, sliding windows, and instruction bit-packing.
- [Instruction Set Architecture (ISA) Reference](docs/isa.md): A complete instruction table detailing operational codes and syntax.
- [Assembler Pipeline & Constant Pool](docs/assembler.md): Constant pool deduplication and the two-pass assembly process.
- [Heap Memory Management & Custom Allocator](docs/memory.md): Free list allocator details, neighbor coalescing, and safety bounds.
- [Performance & Hardware-Level Optimizations](docs/optimizations.md): Advanced explanations on pointer pinning, cache locality, and register unions.
- [Performance & Benchmark Baselines](docs/benchmarks.md): Official version baseline history and instructions for regression testing.

### Example Assembly Workloads
- [Recursive & Linear Fibonacci](examples/fibonacci.md): Side-by-side analysis of recursion depth limits and flat arithmetic loops.
- [Monte Carlo Pi Approximation](examples/monte_carlo.md): Explores how a 4x loop unrolling optimization achieves a **25.6% speedup**.
- [Perceptron Machine Learning Model](examples/perceptron.md): Textual model training illustrating weight updates and FFI calling.
- [3D Raytracer Visual Render](examples/raytracer.md): Raytracer camera parameters, mathematical formulas, and PPM output formatting.

### Directory Structure
```text
Raptor/
├── .github/                  # CI/CD workflows, release automation, and issue templates
├── docs/                     # Architectural & specification documents (ISA, memory, pipeline)
├── examples/                 # Example workloads (raytracer, fibonacci, monte carlo, perceptron)
├── Raptor/                   # Core VM, Compiler, and FFI engine (Unity & .NET compatible)
│   ├── Attributes/           # FFI metadata attributes ([RaptorModule], [RaptorMethod], etc.)
│   ├── Compiler/             # Lexer, Parser, AST nodes, and RaptorScript bytecode compiler
│   ├── StdLib/               # Built-in native FFI modules (RaptorMath, RaptorPeripherals)
│   ├── ScriptEngine.cs       # High-level host embedding entry point
│   ├── VirtualMachine.cs     # Ultra-fast hot interpreter dispatch loop & opcode logic
│   ├── BytecodeVerifier.cs   # Bytecode safety validator & stack/register boundary verifier
│   ├── FFIHostTable.cs       # High-speed method reflection & zero-overhead invocation host table
│   ├── Assembler.cs          # Two-pass assembly parser, instruction encoder & constant pool
│   ├── Disassembler.cs       # Bytecode disassembler & instruction decoder
│   ├── ScriptWatcher.cs      # Thread-safe filesystem hot-reloader
│   ├── RaptorBinary.cs       # .rbc binary serialization and header verification engine
│   ├── VMState.cs            # CPU cache-friendly VM execution state struct
│   └── package.json          # Unity Package Manager (UPM) manifest & asmdef integration
├── Raptor.Cli/               # Spectre.Console CLI toolchain
│   ├── BuildCommand.cs       # Compiles .rapt -> .rasm / .rbc & exports editor API metadata
│   ├── RunCommand.cs         # Compiles & executes scripts directly from terminal
│   └── DocsCommand.cs        # Opens documentation reference in browser
├── Raptor.Benchmarks/        # BenchmarkDotNet performance benchmark suite
└── Raptor.Tests/             # Unit and integration test suites
    ├── VMIntegrationTests.cs # Full end-to-end VM script execution tests
    ├── BytecodeVerifierTests.cs # Safety, invalid opcode & boundary verification tests
    └── FfiReflectionTests.cs # FFI method registration & call overhead tests
```

---

## Built-In Standard Library

Raptor ships with core FFI modules exposed natively to RaptorScript:
* **`math`:** Under `RaptorMath.cs` (contains `Sin`, `Cos`, `Tan`, `Pow`, `Sqrt`, `Min`, `Max`, `Abs`, `Floor`, `Ceiling`, `Atan2`, `Clamp`, `Pi`).
* **`peri`:** Under `RaptorPeripherals.cs` (contains `Print` for console output).

These modules register using high-performance reflection via custom attributes (`[RaptorModule]`, `[RaptorMethod]`, `[RaptorDescription]`, `[RaptorParam]`, `[RaptorPure]`).

---

## Roadmap

Upcoming features and planned additions to the Raptor ecosystem:
- [ ] **Gas Budgeting & Instruction Limits:** Essential for running untrusted user scripts or mods. An instruction counter guard prevents infinite loops from hanging the main game thread without needing multi-threaded OS process isolation.
- [X] **Rust-Style Diagnostic Errors:** Current compiler errors output basic line numbers. Adding source-span spans with inline code snippets and fix hints will make debugging RaptorScript syntax errors significantly faster.
- [ ] **Standard Library Expansion:** Adding native 2D/3D vector math structs (`vec2`, `vec3`), string operations, and fixed-capacity lists directly into the built-in FFI host table so game developers don't have to write custom bindings for common data structures.
- [ ] **RaptorPure Handling:** Strict execution sandboxing that guarantees a script cannot trigger external host side-effects or mutate host state outside designated input/output buffers.
- [ ] **IDE Language Server Support:** A Language Server Protocol (LSP) implementation providing real-time diagnostics, auto-complete for registered FFI methods (`-api.json`), and syntax highlighting in VS Code and other editors.

---

## Community & Support

- **Questions & Discussions**: Ask questions, share engine integration ideas, or showcase projects on [GitHub Discussions](https://github.com/InfiniteFightingGhost/Raptor/discussions).
- **Bug Reports & Feature Requests**: Open an issue using our structured [Issue Templates](https://github.com/InfiniteFightingGhost/Raptor/issues/new/choose).
- **Security Disclosures**: Review our [Security Policy](SECURITY.md) to report vulnerabilities privately.

---

## License
Raptor is released under the [MIT License](LICENSE).

