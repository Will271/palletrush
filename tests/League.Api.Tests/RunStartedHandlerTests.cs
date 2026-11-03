using System.Net.Http.Json;
using League.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;

namespace League.Api.Tests;

public class RunStartedHandlerTests
{
    private const int HeatId = 1;
    private const int DriverId = 3;

    [Test]
    public async Task Handling_the_same_RunStarted_twice_creates_one_run()
    {
        var startedAt = new DateTimeOffset(2026, 11, 7, 9, 30, 0, TimeSpan.Zero);
        var runStarted = new RunStarted(Guid.NewGuid(), Guid.NewGuid(), HeatId, DriverId, startedAt, "v2");

        await TestEnvironment.HandleAsync(runStarted);
        await TestEnvironment.HandleAsync(runStarted);

        using var scope = TestEnvironment.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LeagueDbContext>();
        Assert.That(await db.Runs.CountAsync(r => r.Id == runStarted.RunId), Is.EqualTo(1));
    }

    [Test]
    public async Task Duplicate_RunStarted_reconciles_pending_hits_only_once()
    {
        var runId = Guid.NewGuid();
        var startedAt = new DateTimeOffset(2026, 11, 7, 9, 30, 0, TimeSpan.Zero);

        await TestEnvironment.HandleAsync(new ConeHit(Guid.NewGuid(), runId, "C3", startedAt.AddSeconds(30), "v2"));

        var runStarted = new RunStarted(Guid.NewGuid(), runId, HeatId, DriverId, startedAt, "v2");
        await TestEnvironment.HandleAsync(runStarted);
        await TestEnvironment.HandleAsync(runStarted);

        using var scope = TestEnvironment.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LeagueDbContext>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(await db.Runs.CountAsync(r => r.Id == runId), Is.EqualTo(1));
            Assert.That(await db.Penalties.CountAsync(p => p.RunId == runId), Is.EqualTo(1));
            Assert.That(await db.PendingConeHits.CountAsync(p => p.RunId == runId), Is.EqualTo(0));
        }

        using var client = TestEnvironment.Factory.CreateClient();
        var penalties = await client.GetFromJsonAsync<List<PenaltyRow>>($"/api/runs/{runId}/penalties");
        Assert.That(penalties, Has.Count.EqualTo(1));
    }

    private record PenaltyRow(string ConeId, int Seconds, DateTimeOffset SensorTs);
}
