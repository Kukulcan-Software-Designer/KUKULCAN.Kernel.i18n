using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using System.Net;

namespace KUKULCAN.Kernel.i18n.API.IntegrationTests;

public sealed class HealthEndpointTests : IAsyncLifetime
{
    private readonly TestcontainersContainer _postgres = new TestcontainersBuilder<TestcontainersContainer>()
        .WithImage("postgres:16-alpine")
        .WithEnvironment("POSTGRES_USER", "itzamna")
        .WithEnvironment("POSTGRES_PASSWORD", "itzamna_pass")
        .WithEnvironment("POSTGRES_DB", "itzamna_i18n_test")
        .WithPortBinding(55432, 5432)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    private ApiFactory? _factory;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        string conn = "Host=localhost;Port=55432;Database=itzamna_i18n_test;Username=itzamna;Password=itzamna_pass;";
        _factory = new ApiFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = conn,
            ["Atlas:Database:ConnectionString"] = conn,
            ["ConnectionStrings:Redis"] = "",
            ["Database:AutoMigrate"] = "false"
        });
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null) _factory.Dispose();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        using var client = _factory!.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
