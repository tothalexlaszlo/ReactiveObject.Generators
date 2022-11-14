using Xunit.Abstractions;

namespace ReactiveObject.Generators.Tests;

[UsesVerify] // 👈 Adds hooks for Verify into XUnit
public class ReactivePropertyTests
{
    private const string _snapshotsDirectory = "Snapshots";
    private readonly ITestOutputHelper _output;

    public ReactivePropertyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public Task CanGenerateReactivePropertyInChildNamespace()
    {
        const string input = @"
using ReactiveObject.Generators;

namespace MyTestNamespace
{
    public class MyTestClass
    {
        [ReactiveProperty]
        private DateTime _dateTime;
    }
}";

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput<ReactivePropertySourceGenerator>(input);

        Assert.Empty(diagnostics);
        return Verifier.Verify(output).UseDirectory(_snapshotsDirectory);
    }
}
