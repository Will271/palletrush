using System.Net.Http.Json;
using Shared.Contracts;

namespace League.Api.Tests;

public class ConeHitHandlerTests
{
    private const int HeatId = 1;
    private const int DriverId = 2;

    [Test]
    public async Task Handling_the_same_ConeHit_twice_adds_one_penalty()
    {
        var runId = Guid.NewGuid();
        var t0 = new DateTimeOffset(2026, 11, 7, 9, 30, 0, TimeSpan.Zero);
        await TestEnvironment.HandleAsync(new RunStarted(Guid.NewGuid(), runId, HeatId, DriverId, t0, "v2"));
        await TestEnvironment.HandleAsync(new RunFinished(Guid.NewGuid(), runId, t0.AddSeconds(90), "v2"));

        var coneHit = new ConeHit(Guid.NewGuid(), runId, "C3", t0.AddSeconds(30), "v2");
        await TestEnvironment.HandleAsync(coneHit);
        await TestEnvironment.HandleAsync(coneHit);

        using var client = TestEnvironment.Factory.CreateClient();
        var leaderboard = await client.GetFromJsonAsync<List<LeaderboardRow>>($"/api/heats/{HeatId}/leaderboard");
        var row = leaderboard!.Single(r => r.RunId == runId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(row.PenaltyCount, Is.EqualTo(1));
            Assert.That(row.TotalTimeMs, Is.EqualTo(row.RawTimeMs + 5000));
        }
    }

    [Test]
    public async Task Concurrent_ConeHit_and_RunStarted_store_the_penalty()
    {
        var runId = Guid.NewGuid();
        var t0 = new DateTimeOffset(2026, 11, 7, 9, 30, 0, TimeSpan.Zero);

        var runStarted = new RunStarted(Guid.NewGuid(), runId, HeatId, DriverId, t0, "v2");
        var coneHit = new ConeHit(Guid.NewGuid(), runId, "C3", t0.AddSeconds(30), "v2");

        var runTask = TestEnvironment.HandleAsync(runStarted);
        var coneTask = TestEnvironment.HandleAsync(coneHit);
        await Task.WhenAll(runTask, coneTask);

        await TestEnvironment.HandleAsync(new RunFinished(Guid.NewGuid(), runId, t0.AddSeconds(90), "v2"));

        using var client = TestEnvironment.Factory.CreateClient();
        var leaderboard = await client.GetFromJsonAsync<List<LeaderboardRow>>($"/api/heats/{HeatId}/leaderboard");
        var row = leaderboard!.Single(r => r.RunId == runId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(row.PenaltyCount, Is.EqualTo(1));
            Assert.That(row.TotalTimeMs, Is.EqualTo(row.RawTimeMs + 5000));
        }
    }

    [Test]
    public async Task V1_retransmissions_with_different_event_ids_are_deduped()
    {
        var runId = Guid.NewGuid();
        var t0 = new DateTimeOffset(2026, 11, 7, 9, 30, 0, TimeSpan.Zero);
        await TestEnvironment.HandleAsync(new RunStarted(Guid.NewGuid(), runId, HeatId, DriverId, t0, "v2"));
        await TestEnvironment.HandleAsync(new RunFinished(Guid.NewGuid(), runId, t0.AddSeconds(90), "v2"));

        var first = new ConeHit(Guid.NewGuid(), runId, "C3", t0.AddSeconds(30), "v1");
        var second = new ConeHit(Guid.NewGuid(), runId, "C3", t0.AddSeconds(30), "v1");

        await TestEnvironment.HandleAsync(first);
        await TestEnvironment.HandleAsync(second);

        using var client = TestEnvironment.Factory.CreateClient();
        var leaderboard = await client.GetFromJsonAsync<List<LeaderboardRow>>($"/api/heats/{HeatId}/leaderboard");
        var row = leaderboard!.Single(r => r.RunId == runId);
        Assert.That(row.PenaltyCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Pending_cone_hits_are_reconciled_when_run_starts()
    {
        var runId = Guid.NewGuid();
        var t0 = new DateTimeOffset(2026, 11, 7, 9, 30, 0, TimeSpan.Zero);

        var coneHit = new ConeHit(Guid.NewGuid(), runId, "C3", t0.AddSeconds(30), "v2");
        await TestEnvironment.HandleAsync(coneHit);

        await TestEnvironment.HandleAsync(new RunStarted(Guid.NewGuid(), runId, HeatId, DriverId, t0, "v2"));

        using var client = TestEnvironment.Factory.CreateClient();
        var penalties = await client.GetFromJsonAsync<List<PenaltyRow>>($"/api/runs/{runId}/penalties");
        Assert.That(penalties, Has.Count.EqualTo(1));
        Assert.That(penalties[0].ConeId, Is.EqualTo("C3"));

        await TestEnvironment.HandleAsync(new RunFinished(Guid.NewGuid(), runId, t0.AddSeconds(90), "v2"));

        var leaderboard = await client.GetFromJsonAsync<List<LeaderboardRow>>($"/api/heats/{HeatId}/leaderboard");
        var row = leaderboard!.Single(r => r.RunId == runId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(row.PenaltyCount, Is.EqualTo(1));
            Assert.That(row.TotalTimeMs, Is.EqualTo(row.RawTimeMs + 5000));
        }
    }

    [Test]
    public async Task Duplicate_v1_ConeHits_before_RunStarted_create_only_one_penalty()
    {
        var runId = Guid.NewGuid();
        var t0 = new DateTimeOffset(2026, 11, 7, 9, 30, 0, TimeSpan.Zero);

        var first = new ConeHit(Guid.NewGuid(), runId, "C3", t0.AddSeconds(30), "v1");
        var second = new ConeHit(Guid.NewGuid(), runId, "C3", t0.AddSeconds(30), "v1");

        await TestEnvironment.HandleAsync(first);
        await TestEnvironment.HandleAsync(second);

        await TestEnvironment.HandleAsync(new RunStarted(Guid.NewGuid(), runId, HeatId, DriverId, t0, "v2"));
        await TestEnvironment.HandleAsync(new RunFinished(Guid.NewGuid(), runId, t0.AddSeconds(90), "v2"));

        using var client = TestEnvironment.Factory.CreateClient();
        var leaderboard = await client.GetFromJsonAsync<List<LeaderboardRow>>($"/api/heats/{HeatId}/leaderboard");
        var row = leaderboard!.Single(r => r.RunId == runId);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(row.PenaltyCount, Is.EqualTo(1));
            Assert.That(row.TotalTimeMs, Is.EqualTo(row.RawTimeMs + 5000));
        }

        var penalties = await client.GetFromJsonAsync<List<PenaltyRow>>($"/api/runs/{runId}/penalties");
        Assert.That(penalties, Has.Count.EqualTo(1));
    }

    private record LeaderboardRow(Guid RunId, int RawTimeMs, int PenaltyCount, int TotalTimeMs);
    private record PenaltyRow(string ConeId, int Seconds, DateTimeOffset SensorTs);
}
