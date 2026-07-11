# Unsafe Register-Based Virtual Machine
![Orbit Animation](./orbit.gif)

Welcome to the **Unsafe Register-Based Virtual Machine**, a high-performance, single-threaded bytecode interpreter written in C# targeting **.NET 10.0**.

Unlike standard hobby runtimes(I am still a hobbyist, smh) which are typically stack-based, this project implements a register-based VM (reminiscent of the Lua 5.0) optimized to run extremely close to the metal. Leveraging raw pointer arithmetic, stack allocation, zero-copy sliding register windows, and manual heap block coalescing, it achieves execution speeds between **360 MIPS and 490+ MIPS** on standard consumer hardware.

---

## Key Performance Highlights

- **Screamingly Fast Interpreter:** Operates at **~10 CPU clock cycles per VM instruction** on modern architectures, executing **475+ million virtual instructions per second**.
- **Stack-Allocated Register File:** Zero garbage collection overhead during execution via `stackalloc` memory buffers.
- **Zero-Check Memory Operations:** Fixed pointer pins bypass all managed array bounds checks (`IndexOutOfRangeException`), yielding linear JIT-compiled assembly instructions.
- **Sliding Register Windows:** Dynamic function calls shift the register frame pointer directly (`BasePtr += offset`), enabling zero-copy parameter passing.
- **Intrinsic Memory Allocator:** A complete custom allocator (`NEWARR` / `FREEARR`) managing a linked list of free blocks directly within a raw heap byte array, featuring immediate block coalescing (compaction).

---

## Technical Directory

The project documentation is split into architectural specifications (`docs/`) and example programs (`examples/`):

### Core Architecture & Memory Details
- [Core Architecture & Calling Conventions](docs/architecture.md): Detailed specifications on instruction formats, the 256-register layout, Register/Constant (RC) addressing, sliding windows, and the call stack.
- [Instruction Set Architecture (ISA) Reference](docs/isa.md): A comprehensive reference table detailing operational behavior, encoding formats, and assembly syntax for every VM opcode.
- [The Assembler Pipeline & Constant Pool](docs/assembler.md): Details of the lexical scanner, macro parser, label symbol mapping (with two-word instruction offset correction), and constant pool deduplication.
- [Heap Memory Management & Custom Allocator](docs/memory.md): Deep-dive into the custom byte heap, the intrinsically linked list format, first-fit allocation, and immediate neighbor coalescing.
- [Performance & Hardware-Level Optimizations](docs/optimizations.md): Analysis of raw pointer mapping, `stackalloc` cache locality, the Xorshift32 PRNG, double-precision Fast Inverse Square Root (FISR), and the two-word compound `FOR` super-instruction.

### Example Assembly Workloads
- [Recursive & Linear Fibonacci Examples](examples/fibonacci.md): Side-by-side comparison of recursive and linear algorithms, recursion depth limits, and call-stack frame analysis.
- [Monte Carlo Pi Approximation](examples/monte_carlo.md): Mathematical explanation of the Pi estimation, and how a 4x loop unrolling optimization achieves a **25.6% speedup**.
- [Perceptron Machine Learning Model](examples/perceptron.md): Step-by-step training of a logical gate perceptron, detailing functional calls, weight updates, and execution speed.
- [3D Raytracer Visual Render](examples/raytracer.md): Summary of the orbit rendering raytracer producing a PPM image sequence, with camera orbit parameters.

---

## Project Structure

The codebase is highly modular, consisting of the following key source files:
- [VirtualMachine.cs](RegisterBasedVM/VirtualMachine.cs): The hot execution engine containing the dispatch loop and instruction handlers.
- [VMState.cs](RegisterBasedVM/VMState.cs): The execution state packed into a single unsafe struct passed by reference.
- [Instruction.cs](RegisterBasedVM/Instruction.cs): The bit-packed 32-bit instruction layout helper.
- [Assembler.cs](RegisterBasedVM/Assembler.cs): The three-pass assembler compiling textual assembly syntax into bytecode chunks.
- [VMChunk.cs](RegisterBasedVM/VMChunk.cs): Encapsulates compiled bytecode, the constant pool, and method lookup table.
- [StackFrame.cs](RegisterBasedVM/StackFrame.cs): Call stack frame metadata.
- [Program.cs](RegisterBasedVM/Program.cs): Entry point loading and running the benchmark suites and orbit raytracer.

---

## Quick Start & Building

To run the VM benchmarks and orbit raytracer:

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [ImageMagick](https://imagemagick.org/) (optional, required to merge rendered frames into `orbit.gif`)

### Build and Run
Build the project in Release configuration for maximum speed and run:
```bash
cd RegisterBasedVM
dotnet run -c Release
```

To render the 30-frame orbiting raytracer and generate the GIF:
```bash
cd RegisterBasedVM
chmod +x run_ray_tracer.sh
./run_ray_tracer.sh
```
This will compile and execute the raytracer, outputting `frame_00.ppm` to `frame_29.ppm`, and then merge them into `orbit.gif` before deleting the raw PPM files.
