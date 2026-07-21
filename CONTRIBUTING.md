# Contributing to Raptor

## Prerequisites & Setup

Raptor requires the [.NET 10.0 SDK](https://dotnet.microsoft.com/download) and Git.

### Building

```bash
git clone https://github.com/InfiniteFightingGhost/Raptor.git
cd Raptor
dotnet build Raptor.sln --configuration Release
```

## Running Tests & Benchmarks

### Unit Tests
All unit tests must pass before submitting a pull request:

```bash
dotnet test --configuration Release
```

### Code Formatting
Verify and apply C# formatting rules:

```bash
# Check formatting without modifying files
dotnet format Raptor.sln --verify-no-changes

# Fix formatting issues automatically
dotnet format Raptor.sln
```

### Performance Benchmarks
When modifying interpreter hot paths ([VirtualMachine.cs](Raptor/VirtualMachine.cs)), pointer arithmetic, or FFI dispatch, run benchmarks to verify performance:

- Quick Smoke Test (~2s): For fast local iteration while coding.
  ```bash
  dotnet run --configuration Release --project Raptor.Benchmarks -- --fast
  ```
- Fast Benchmark Suite (~1-2 min): BenchmarkDotNet run required for PRs modifying hot paths.
  ```bash
  dotnet run --configuration Release --project Raptor.Benchmarks -- fast
  ```
- Full Benchmark Suite (~20 min): Comprehensive benchmark across all workloads. Recommended before major releases.
  ```bash
  dotnet run --configuration Release --project Raptor.Benchmarks
  ```

## Submitting Pull Requests

1. Fork the repository and create a feature branch off `main`.
2. Keep commits atomic with descriptive messages.
3. Verify standards:
   - Run `dotnet test` (all 73+ unit tests must pass).
   - Run `dotnet format Raptor.sln --verify-no-changes` (zero formatting warnings).
4. Open a pull request against `main`.

## Coding & Performance Conventions

- Zero Allocation in Hot Paths: Keep interpreter execution loops in `VirtualMachine.cs` free of heap allocations (`new`). Use `stackalloc`, `Span<T>`, or fixed pointer buffers.
- Pointer & Memory Safety: Raw pointer arithmetic must respect memory boundaries and pass `BytecodeVerifier` checks.
- Public API Documentation: Include XML doc comments (`/// <summary>`) for public classes, structs, methods, and FFI attributes.

## Community Guidelines

Contributions are governed by the [Code of Conduct](CODE_OF_CONDUCT.md).

For questions, architecture discussions, or feature proposals, join [GitHub Discussions](https://github.com/InfiniteFightingGhost/Raptor/discussions).
