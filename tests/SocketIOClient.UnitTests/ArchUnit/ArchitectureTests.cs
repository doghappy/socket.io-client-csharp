using ArchUnitNET.Domain;
using ArchUnitNET.Fluent.Slices;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Assembly = System.Reflection.Assembly;

namespace SocketIOClient.UnitTests.ArchUnit;

public class ArchitectureTests
{
    private static readonly Architecture Architecture = new ArchLoader().LoadAssemblies(
        Assembly.Load("SocketIOClient"),
        Assembly.Load("SocketIOClient.Common"),
        Assembly.Load("SocketIOClient.Serializer")
    ).Build();

    [Fact]
    public void CycleDependencies_NotExists()
    {
        var sliceRuleInitializer = SliceRuleDefinition.Slices();
        var socketIOClientRule = sliceRuleInitializer.Matching($"{nameof(SocketIOClient)}.(*)").Should()
            .BeFreeOfCycles();

        socketIOClientRule.Check(Architecture);
    }
}