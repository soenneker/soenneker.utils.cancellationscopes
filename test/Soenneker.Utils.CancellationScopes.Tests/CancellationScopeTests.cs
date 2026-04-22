using Soenneker.Utils.CancellationScopes.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Utils.CancellationScopes.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class CancellationScopeTests : HostedUnitTest
{
    public CancellationScopeTests(Host host) : base(host)
    {
    }

    [Test]
    public void Default()
    {

    }
}
