# Register-Based VM Development Roadmap

This document maps out the path to transforming the Register-Based VM into a production-ready, bulletproof, zero-allocation game scripting engine. The goal is to provide the creative flexibility of dynamic scripts with the safety, performance, and cross-platform reliability required for locked 60-FPS game loops.

---

## Phase 1: Hardcore Execution Safety (The Uncrashable Sandbox)
*High-frequency gameplay code and modding scripts must never crash the host game process.*

- [ ] **Gas Limit & Instruction Budget (Thread Lock Guard)** `[Priority: Critical]`
  - *Context:* Malicious or poorly written scripts containing infinite loops (e.g., `loop: JUMP loop`) will lock up the main Unity execution thread, freezing the game.
  - *Implementation:* Add an `InstructionLimit` or `Gas` counter to `VMState`. Decrement it on jumps/instructions; halt execution with a `GasExceeded` error code if it hits zero.
- [ ] **Call Stack Depth Limit (Recursion Guard)** `[Priority: Critical]`
  - *Context:* Unchecked recursion results in a C# `StackOverflowException`, which cannot be caught by `try/catch` blocks and immediately terminates the host process.
  - *Implementation:* Enforce a strict maximum size (e.g., 64 or 128 frames) on `CallStackPtr` inside `ExecuteCall` and panic gracefully on overflow.
- [ ] **Host Exception Isolation & Boundary** `[Priority: High]`
  - *Context:* When the VM calls C# host functions via FFI, any thrown C# exception (e.g., `NullReferenceException`) will escape the VM execution loop and crash the caller.
  - *Implementation:* Wrap FFI host calls in a `try/catch` boundary, intercept exceptions, store the exception details in the VM state, and halt the VM with a `HostError` state.
- [X] **AOT Bytecode Verifier (Static Analysis)** `[Priority: High]`
  - *Context:* Malicious compiled bytecode can contain invalid registers (e.g., `R999`), jump out of the code array bounds, or cause a memory access violation.
  - *Implementation:* Create a validator that runs *before* executing the bytecode:
    - Validate that all jump and branch targets land on valid instruction starts (protecting compound instruction structures like `FOR`).
    - Verify that no instruction can jump outside the instruction array bounds.
    - Confirm that register references fall strictly within the allocated frame size (0 to 255).
    - Ensure the program ends with a terminal instruction (`HALT` or `RETURN`).
- [X] **Heap Out-of-Bounds & Safety Guards** `[Priority: High]`
  - *Context:* Array operations (`SETARR`, `GETARR`) rely on raw pointer arithmetic. Reading/writing out-of-bounds corrupts the VM heap or crashes the process.
  - *Implementation:* During bytecode verification (static offsets) and execution (dynamic indexes), assert that pointer addresses plus their indexed offsets remain inside the allocated array limits and within the total `_heapSize`.
- [X] **Instruction Alignment Validation** `[Completed]`
  - *Context:* Ensure relative jumps do not land in the middle of a two-word instruction (like `FOR`).

---

## Phase 2: Zero-Allocation Execution (Strict Zero-GC)
*Evaluating thousands of scripts per frame must generate exactly zero bytes of garbage on the managed heap.*

- [X] **Eliminate Managed `StringBuilder` from `VMState`** `[Priority: Critical]`
  - *Context:* Instantiating a `StringBuilder` on `RunFast` setup generates garbage.
  - *Implementation:* Replace the managed `StringBuilder` with a caller-provided byte/char span or log callback delegate, maintaining complete Zero-GC execution.
- [ ] **Register Type Optimization (Eliminate Cast Stalls)** `[Priority: High]`
  - *Context:* The VM currently stores registers as `double`. Heap array addresses and bitwise operations require casting `double` to `uint`/`long` and back. These cast operations compile to slow CPU instructions that stall the CPU execution pipeline.
  - *Implementation:* Define a `Register` union struct using `[StructLayout(LayoutKind.Explicit)]` that overlays a `double` (64-bit float) and a `ulong`/`long` (64-bit bits/integers). This allows direct, zero-overhead integer and pointer reads/writes from registers.
- [ ] **Virtual Machine & State Reuse** `[Priority: Medium]`
  - *Context:* Allocating `VirtualMachine` classes or state blocks inside the game loop causes garbage collector spikes.
  - *Implementation:* Restructure state initialization to allow the host to pre-allocate, reset, and reuse `VirtualMachine` instances and their internal arrays/heaps across frames.

---

## Phase 3: Advanced Host FFI (The Bulletproof Bridge)
*Pass data and reference game components between C# and the VM securely with zero copy overhead.*

- [ ] **Host Function Registry (`CALL_HOST`)** `[Priority: Critical]`
  - *Context:* Scripts need to call game logic (e.g., `SetPosition`, `PlaySound`).
  - *Implementation:* Add a `CALL_HOST` opcode that maps to an index in a registry of `delegate* unmanaged` pointers (for maximum IL2CPP speed) or static delegates.
- [ ] **Unity Object Handle Mapping** `[Priority: High]`
  - *Context:* Scripts must interact with managed objects (e.g., `Transform`, `GameObject`, `Actor`) but cannot hold managed references directly inside stack-allocated double/long registers.
  - *Implementation:* Build a handle table on the host. Pass objects to the VM as integer handles (`uint`). The VM passes the handle back to host FFI calls, which look up the original object in a high-speed, zero-allocation handle table.
- [ ] **Zero-Allocation String Marshalling** `[Priority: High]`
  - *Context:* Reading/writing text for dialogs, item names, or component keys.
  - *Implementation:*
    - Implement a zero-allocation helper `ReadString(uint address)` to expose string data as `ReadOnlySpan<byte>` or `ReadOnlySpan<char>` directly from the VM heap.
    - Implement a helper to write C# string/char data directly into the VM heap using the internal memory allocator.
- [ ] **Gameplay Vector Math Support** `[Priority: Medium]`
  - *Context:* High-frequency game math heavily utilizes `Vector3`, `Vector2`, and `Quaternion`. Copying these component-by-component into individual registers is slow.
  - *Implementation:* Define standard register mapping patterns or support packing short vectors (like `Vector3` components) into continuous register blocks, or add dedicated vector math operations to the ISA.

---

## Phase 4: Platform & Compiler Compatibility (Unity IL2CPP, Consoles, Mobile)
*The VM must compile and run on strict platform targets without compiler errors or type-loading failures.*

- [ ] **Eliminate Explicit Struct Alignment Risks** `[Priority: High]`
  - *Context:* `VMState` uses `[StructLayout(LayoutKind.Explicit, Size = 80)]` with hardcoded 8-byte pointer offsets. On 32-bit platforms (e.g., older mobile or specialized IoT targets), pointers are 4 bytes. This mismatch wastes space, can cause misalignment issues, and may trigger `TypeLoadException` with garbage-collected references.
  - *Implementation:* Switch `VMState` to `LayoutKind.Sequential` and use packing or automatic alignment attributes.
- [ ] **Unity IL2CPP AOT Validation** `[Priority: High]`
  - *Context:* Unity's IL2CPP compiler compiles C# code directly to C++ and has strict limits on dynamic generics, reflection, and certain raw pointer actions.
  - *Implementation:* Set up a pipeline check to ensure all `unsafe` operations, pointer arithmetic, and `stackalloc` constructs compile without warnings under IL2CPP.

---

## Phase 5: Debugging & Tooling (Developer Experience)
*When a designer's script fails in production, engineers need diagnostics without debugging VM internals.*

- [ ] **The Panic Dump** `[Priority: High]`
  - *Context:* Failures like division by zero, gas depletion, or heap exhaustion should output clear diagnostics.
  - *Implementation:* On panic, return an `ExecutionResult` containing the error code, the execution pointer index (`Ip`), and a diagnostic snapshot of the active register window and call stack trace.
- [ ] **Bytecode Disassembler** `[Priority: Medium]`
  - *Context:* Reading raw binary compiled files directly is impossible for designers.
  - *Implementation:* Implement `VirtualMachine.Disassemble(byte[] bytecode)` which translates raw binary instructions back into human-readable assembly text.
- [ ] **Zero-Overhead Debugger Hooks** `[Priority: Low]`
  - *Context:* Stepping through scripts or inspecting registers interactively.
  - *Implementation:* Provide a separate `RunDebug()` method/loop with step-by-step delegate callbacks (`OnInstructionExecuted`). By separating this from the release loop, the production `RunFast()` loop remains completely free of debugger check branches.

---

## Phase 6: Bytecode Standardization & Distribution
*Define the compiled package format and package the VM for game engine use.*

- [ ] **Standardized Binary Format** `[Priority: Medium]`
  - *Context:* Preventing the VM from attempting to run corrupt files, text scripts, or outdated bytecode formats.
  - *Implementation:* Add magic signature bytes (e.g., `0x52415054` for "RAPT") and a schema version header at the start of compiled binary files. Separately structure metadata, constants pool, and instructions.
- [ ] **Unity Package Manager (UPM) Support** `[Priority: Medium]`
  - *Context:* Integrating the VM into Unity projects cleanly.
  - *Implementation:* Create a `package.json` manifest and assembly definitions (`.asmdef`) to allow installing the engine directly via Git URLs or local folders.
- [ ] **High-Level Wrapper (`ScriptEngine`)** `[Priority: Medium]`
  - *Context:* Gameplay programmers should not have to manage `unsafe` or pointer logic directly.
  - *Implementation:* Write a clean, high-level wrapper class that hides state management, pointer pinning, and stack allocations behind simple methods like `engine.Execute("script.bin")`.
