[![](https://img.shields.io/nuget/v/soenneker.utils.cancellationscopes.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.cancellationscopes/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.cancellationscopes/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.cancellationscopes/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.utils.cancellationscopes.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.cancellationscopes/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.cancellationscopes/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.utils.cancellationscopes/actions/workflows/codeql.yml)

# Soenneker.Utils.CancellationScopes

A thread-safe, reusable cancellation scope for replacing a token while cancelling work associated with the previous generation.

## Installation

```bash
dotnet add package Soenneker.Utils.CancellationScopes
```

## Usage

```csharp
await using var scope = new CancellationScope();

Task firstRun = RunAsync(scope.CancellationToken);

await scope.ResetCancellation(); // cancels firstRun's token and creates a fresh token

Task secondRun = RunAsync(scope.CancellationToken);
```

`CancellationToken` lazily creates the initial token source. `Cancel()` cancels the current token without replacing it, so later reads remain cancelled until `ResetCancellation()` is called.

To combine each generated token with an owning operation or application token, pass that token to the constructor:

```csharp
await using var scope = new CancellationScope(applicationStopping);
```

Every token generation remains linked to the constructor token. Once that parent is cancelled, resetting the scope cannot produce a non-cancelled token.

## Lifetime

Dispose the scope when its owner ends. Disposal cancels the current token source and releases it; subsequent token access returns `CancellationToken.None`, and reset becomes a no-op.

Callers that retain an older token can still observe its cancellation after a reset. Resetting does not wait for operations using that token to finish.
