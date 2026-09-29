using System.Reflection;
using Xunit;

namespace OsuHitsoundEditor.Tests;

public class TestOrganizationTests
{
    [Fact]
    public void BeatmapLoaderTests_AllTestsStartWithLoad()
    {
        MethodInfo[] testMethods = GetTestMethods(typeof(BeatmapLoaderTests));

        foreach (MethodInfo method in testMethods)
        {
            Assert.True(
                method.Name.StartsWith("Load_", StringComparison.Ordinal),
                $"The test '{method.Name}' is inside BeatmapLoaderTests, " +
                "but its name does not start with 'Load_'. " +
                "It may belong in another test class."
            );
        }
    }

    [Fact]
    public void BeatmapTests_NoTestsStartWithLoad()
    {
        MethodInfo[] testMethods = GetTestMethods(typeof(BeatmapTests));

        foreach (MethodInfo method in testMethods)
        {
            Assert.False(
                method.Name.StartsWith("Load_", StringComparison.Ordinal),
                $"The test '{method.Name}' is inside BeatmapTests, " +
                "but its name starts with 'Load_'. " +
                "It probably belongs in BeatmapLoaderTests."
            );
        }
    }

    private static MethodInfo[] GetTestMethods(Type testClass)
    {
        return testClass
            .GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly
            )
            .Where(method =>
                method.GetCustomAttribute<FactAttribute>() != null ||
                method.GetCustomAttribute<TheoryAttribute>() != null
            )
            .ToArray();
    }
}