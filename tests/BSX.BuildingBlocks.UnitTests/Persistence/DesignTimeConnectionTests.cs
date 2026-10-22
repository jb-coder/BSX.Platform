using BSX.BuildingBlocks.Persistence;

namespace BSX.BuildingBlocks.UnitTests.Persistence;

public sealed class DesignTimeConnectionTests
{
    private const string Fallback = "Host=localhost;Database=fallback";

    [Fact]
    public void Should_PreferTheConnectionArgument()
    {
        string connectionString = DesignTimeConnection.Resolve(
            ["--connection", "Host=db;Database=from-argument"],
            UniqueModule(),
            Fallback);

        connectionString.Should().Be("Host=db;Database=from-argument");
    }

    [Fact]
    public void Should_FallBackToTheModuleEnvironmentVariable()
    {
        string module = UniqueModule();
        string variable = $"ConnectionStrings__{module}";

        try
        {
            Environment.SetEnvironmentVariable(variable, "Host=db;Database=from-environment");

            string connectionString = DesignTimeConnection.Resolve([], module, Fallback);

            connectionString.Should().Be("Host=db;Database=from-environment");
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Fact]
    public void Should_UseTheFallback_WhenNoOverrideIsSupplied()
    {
        string connectionString = DesignTimeConnection.Resolve([], UniqueModule(), Fallback);

        connectionString.Should().Be(Fallback);
    }

    private static string UniqueModule() => $"Module{Guid.NewGuid():N}";
}
