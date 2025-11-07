using ArchUnitNET.xUnit;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent.Slices;
using ArchUnitNET.Loader;
using Xunit;

namespace SocketIOClient.UnitTests.ArchUnit;

public class ArchitectureTests
{
    private static readonly Architecture Architecture = new ArchLoader().LoadAssemblies(
        System.Reflection.Assembly.Load("SocketIOClient"),
        System.Reflection.Assembly.Load("SocketIOClient.Core"),
        System.Reflection.Assembly.Load("SocketIOClient.Serializer")
    ).Build();

    [Fact(Skip = "Test")]
    public void CycleDependencies_NotExists()
    {
        var sliceRuleInitializer = SliceRuleDefinition.Slices();
        var socketIOClientRule = sliceRuleInitializer.Matching($"{nameof(SocketIOClient)}.(*)").Should()
            .BeFreeOfCycles();

        socketIOClientRule.Check(Architecture);
    }
}