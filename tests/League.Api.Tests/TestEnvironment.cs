using League.Api.Events;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace League.Api.Tests;

[SetUpFixture]
public class TestEnvironment
{
    private static readonly PostgreSqlContainer Postgres = new PostgreSqlBuilder("postgres:17.11").Build();
    private static readonly RedisContainer Redis = new RedisBuilder("redis:7.4.11").Build();

    public static WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public static string PostgresConnectionString => Postgres.GetConnectionString();

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        await Task.WhenAll(Postgres.StartAsync(), Redis.StartAsync());

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:League", Postgres.GetConnectionString())
            .UseSetting("ConnectionStrings:Redis", Redis.GetConnectionString())
            .UseSetting("Logging:LogLevel:Microsoft.EntityFrameworkCore", "Warning"));
    }

    [OneTimeTearDown]
    public async Task StopAsync()
    {
        await Factory.DisposeAsync();
        await Postgres.DisposeAsync();
        await Redis.DisposeAsync();
    }

    public static async Task HandleAsync<T>(T courseEvent)
    {
        using var scope = Factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICourseEventHandler<T>>();
        await handler.HandleAsync(courseEvent, CancellationToken.None);
    }
}
