[![](https://img.shields.io/nuget/v/soenneker.utils.cancellationscopes.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.cancellationscopes/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.cancellationscopes/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.cancellationscopes/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.utils.cancellationscopes.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.cancellationscopes/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.cancellationscopes/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.utils.cancellationscopes/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Utils.CancellationScopes
A lightweight library for creating, resetting, and cancelling CancellationToken instances in a thread-safe, reusable way.

## Installation

```bash
dotnet add package Soenneker.Utils.CancellationScopes
```

## Quick start

```csharp
using Soenneker.Utils.CancellationScopes;
```

Create `CancellationScope` directly or depend on `ICancellationScope` in code that uses the abstraction.

## Common operations

- `Cancel()` - Cancels any in-flight work associated with the current `CancellationToken`. This method is a no-op if no `CancellationTokenSource` has been created yet.
- `ResetCancellation()` - Cancels the current `CancellationToken` (if any) and replaces it with a fresh `CancellationTokenSource` for new work. This method allows a consumer to cleanly cancel in-progress work and immediately prepare for a new operation without lingering cancellation state.
- `DisposeAsync()` - Cancels the scope and asynchronously releases its cancellation resources.
