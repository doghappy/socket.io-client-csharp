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
        System.Reflection.Assembly.Load("SocketIO.Core"),
        System.Reflection.Assembly.Load("SocketIO.Serializer.Core"),
        // System.Reflection.Assembly.Load("SocketIO.Serializer.MessagePack"),
        // System.Reflection.Assembly.Load("SocketIO.Serializer.NewtonsoftJson"),
        System.Reflection.Assembly.Load("SocketIO.Serializer.SystemTextJson")
        // System.Reflection.Assembly.Load("SocketIOClient.Windows7")
        // System.Reflection.Assembly.Load("SocketIOClient"),
        // System.Reflection.Assembly.Load("SocketIOClient"),
        // System.Reflection.Assembly.Load("SocketIOClient"),
        // System.Reflection.Assembly.Load("SocketIOClient.UnitTests")
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