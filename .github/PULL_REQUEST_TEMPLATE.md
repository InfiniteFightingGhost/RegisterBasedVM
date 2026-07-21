## Summary of Changes

Provide a brief summary of the changes introduced in this PR and the problem/feature they address.

## Type of Change

- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Performance optimization (enhancements to VM execution speed or memory allocations)
- [ ] Documentation update

## PR Checklist

- [ ] I have read the [CONTRIBUTING.md](../CONTRIBUTING.md) guide.
- [ ] All unit tests pass cleanly (`dotnet test --configuration Release`).
- [ ] Code formatting has been checked (`dotnet format Raptor.sln --verify-no-changes`).
- [ ] If modifying hot VM execution paths, I ran the fast benchmark suite (`dotnet run -c Release --project Raptor.Benchmarks -- fast`).
- [ ] New public methods/classes include standard XML documentation comments.
- [ ] Zero GC allocation guarantees have been preserved in hot VM paths.
