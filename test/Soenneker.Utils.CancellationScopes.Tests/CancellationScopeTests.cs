using Soenneker.Utils.CancellationScopes.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.Utils.CancellationScopes.Tests;

[Collection("Collection")]
public sealed class CancellationScopeTests : FixturedUnitTest
{
    public CancellationScopeTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
    }

    [Fact]
    public void Default()
    {

    }
}
