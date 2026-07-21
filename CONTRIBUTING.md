# Contributing to Raptor

Thank you for your interest in contributing to **Raptor**! We welcome contributions, bug reports, performance enhancements, and documentation improvements from the community.

## Development Prerequisites & Setup

Raptor is built on **.NET 10.0**. Before contributing, ensure you have the following installed:

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Git

### Cloning and Building the Repository

```bash
git clone https://github.com/InfiniteFightingGhost/Raptor.git
cd Raptor
dotnet build Raptor.sln --configuration Release
```

## Running Tests & Benchmarks

### Unit Tests
Raptor maintains strict unit test coverage across the compiler, bytecode verifier, VM execution loop, and FFI system. All tests must pass before submitting a Pull Request.

```bash
dotnet test --configuration Release
```

### Code Formatting
We enforce consistent C# formatting. Run `dotnet format` to verify or auto-format code:

```bash
# Check formatting without modifying files
dotnet format Raptor.sln --verify-no-changes

# Automatically fix formatting issues
dotnet format Raptor.sln
```

### Performance Benchmarks
Raptor is engineered for zero GC allocations and sub-5ns FFI latency.

When modifying hot interpreter paths ([VirtualMachine.cs](Raptor/VirtualMachine.cs)), pointer arithmetic, or FFI dispatch, you must run benchmarks to verify performance:

- **Quick Smoke Test (~2 seconds)**: Useful for fast local iteration while coding.
  ```bash
  dotnet run --configuration Release --project Raptor.Benchmarks -- --fast
  ```
- **Fast Benchmark Suite (~1-2 minutes - REQUIRED for hot path PRs)**: High-accuracy benchmark run using BenchmarkDotNet fast jobs. Required for any PR modifying hot paths.
  ```bash
  dotnet run --configuration Release --project Raptor.Benchmarks -- fast
  ```
- **Full Benchmark Suite (~20 minutes)**: Peak accuracy across all instruction latency, memory, call stack, register pressure, and gameplay workloads. Recommended before major releases.
  ```bash
  dotnet run --configuration Release --project Raptor.Benchmarks
  ```

## How to Submit a Pull Request

1. **Fork the Repository**: Create your feature branch off `main` (`git checkout -b feature/my-cool-feature`).
2. **Commit Changes**: Keep commits atomic and messages descriptive.
3. **Verify Standards**:
   - Run `dotnet test` (all 73+ unit tests must pass).
   - Run `dotnet format Raptor.sln --verify-no-changes` (zero formatting warnings).
4. **Submit PR**: Open a Pull Request against the `main` branch using our PR template.

## Coding & Performance Conventions

- **Zero Allocation in Hot Paths:** Keep interpreter execution loops in `VirtualMachine.cs` entirely free of heap allocations (`new`). Use `stackalloc`, `Span<T>`, or fixed pointer buffers to maintain zero-GC execution.
- **Pointer & Memory Safety:** Any raw pointer arithmetic must strictly respect memory boundaries and pass all `BytecodeVerifier` static analysis checks.
- **Public API Documentation:** Include standard XML doc comments (`/// <summary>`) for all new public classes, structs, methods, and FFI attributes.

## Community Guidelines

Please note that this project is governed by the [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

For general questions, architecture discussions, or feature proposals before writing code, join us on [GitHub Discussions](https://github.com/InfiniteFightingGhost/Raptor/discussions).

