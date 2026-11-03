using System.Net.Http.Json;
using Npgsql;
using Shared.Contracts;

namespace League.Api.Tests;

/// <summary>
/// Deterministic ordering tests for <see cref="ConeHit"/> vs <see cref="RunStarted"/>.
/// Instead of Task.Delay, the test holds an ACCESS EXCLUSIVE table lock so the first
/// handler blocks inside its transaction while holding the per-run advisory lock.
/// pg_locks then proves the first handler owns the advisory lock before the second
/// handler is started, and proves the second handler is queued on it.
/// </summary>
public class EventOrderingTests
{
    private const int HeatId = 1;
    private const int DriverId = 2;

    [Test]
    public async Task ConeHit_acquires_the_advisory_lock_first_and_RunStarted_waits()
    {
        EnsureHostStarted();
        var (runStarted, coneHit, runId, t0) = NewEvents();

        // Block ConeHitHandler's INSERT into pending_cone_hits while it holds the advisory lock.
        await using var blocker = await LockTableAsync("pending_cone_hits");

        var coneTask = TestEnvironment.HandleAsync(coneHit);
        await WaitForAdvisoryLockAsync(runId, granted: true);

        var runTask = TestEnvironment.HandleAsync(runStarted);
        await WaitForAdvisoryLockAsync(runId, granted: false);

        await ReleaseTableLockAsync(blocker);
        await Task.WhenAll(coneTask, runTask);

        await AssertSinglePenaltyAsync(runId, t0);
    }

    [Test]
    public async Task RunStarted_acquires_the_advisory_lock_first_and_ConeHit_waits()
    {
        EnsureHostStarted();
        var (runStarted, coneHit, runId, t0) = NewEvents();

        // Block RunStartedHandler's INSERT into runs while it holds the advisory lock.
        await using var blocker = await LockTableAsync("runs");

        var runTask = TestEnvironment.HandleAsync(runStarted);
        await WaitForAdvisoryLockAsync(runId, granted: true);

        var coneTask = TestEnvironment.HandleAsync(coneHit);
        await WaitForAdvisoryLockAsync(runId, granted: false);

        await ReleaseTableLockAsync(blocker);
        await Task.WhenAll(runTask, coneTask);

        await AssertSinglePenaltyAsync(runId, t0);
    }

    // The host (and its EF migrations) starts lazily on first access; the raw
    // Postgres connection below must not run before the schema exists.
    private static void EnsureHostStarted() => _ = TestEnvironment.Factory.Services;

    private static (RunStarted RunStarted, ConeHit ConeHit, Guid RunId, DateTimeOffset T0) NewEvents()
    {
        var runId = Guid.NewGuid();
        var t0 = new DateTimeOffset(2026, 11, 7, 9, 30, 0, TimeSpan.Zero);
        return (
            new RunStarted(Guid.NewGuid(), runId, HeatId, DriverId, t0, "v2"),
            new ConeHit(Guid.NewGuid(), runId, "C3", t0.AddSeconds(30), "v2"),
            runId,
            t0);
    }

    private static async Task AssertSinglePenaltyAsync(Guid runId, DateTimeOffset t0)
    {
        await TestEnvironment.HandleAsync(new RunFinished(Guid.NewGuid(), runId, t0.AddSeconds(90), "v2"));

        using var client = TestEnvironment.Factory.CreateClient();

        var penalties = await client.GetFromJsonAsync<List<PenaltyRow>>($"/api/runs/{runId}/penalties");
        Assert.That(penalties, Has.Count.EqualTo(1));

        var leaderboard = await client.GetFromJsonAsync<List<LeaderboardRow>>($"/api/heats/{HeatId}/leaderboard");
        var row = leaderboard!.Single(r => r.RunId == runId);
        Assert.That(row.PenaltyCount, Is.EqualTo(1));
    }

    private static async Task<NpgsqlConnection> LockTableAsync(string table)
    {
        var connection = new NpgsqlConnection(TestEnvironment.PostgresConnectionString);
        await connection.OpenAsync();
        await using (var begin = new NpgsqlCommand("BEGIN", connection))
            await begin.ExecuteNonQueryAsync();
        await using (var lockTable = new NpgsqlCommand($"LOCK TABLE {table} IN ACCESS EXCLUSIVE MODE", connection))
            await lockTable.ExecuteNonQueryAsync();
        return connection;
    }

    private static async Task ReleaseTableLockAsync(NpgsqlConnection connection)
    {
        await using var commit = new NpgsqlCommand("COMMIT", connection);
        await commit.ExecuteNonQueryAsync();
    }

    // Matches the per-run advisory lock key used by the handlers:
    // pg_advisory_xact_lock(hashtextextended(runId::text, 0::bigint)).
    // For a bigint key, pg_locks stores the high 32 bits in classid and the
    // low 32 bits in objid (both unsigned), with objsubid = 1.
    private static async Task WaitForAdvisoryLockAsync(Guid runId, bool granted, int timeoutMs = 15000)
    {
        await using var connection = new NpgsqlConnection(TestEnvironment.PostgresConnectionString);
        await connection.OpenAsync();

        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (true)
        {
            await using var command = new NpgsqlCommand("""
                WITH k AS (SELECT hashtextextended(@runId::text, 0::bigint) AS key)
                SELECT COUNT(*)
                FROM pg_locks l, k
                WHERE l.locktype = 'advisory'
                  AND l.objsubid = 1
                  AND l.granted = @granted
                  AND l.classid::text = (((k.key >> 32) & 4294967295))::text
                  AND l.objid::text = ((k.key & 4294967295))::text
                """, connection);
            command.Parameters.AddWithValue("runId", runId.ToString());
            command.Parameters.AddWithValue("granted", granted);
            var count = (long)(await command.ExecuteScalarAsync())!;
            if (count > 0)
                return;
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException($"Timed out waiting for advisory lock on {runId} granted={granted}");
            await Task.Delay(25);
        }
    }

    private record LeaderboardRow(Guid RunId, int PenaltyCount);
    private record PenaltyRow(string ConeId, int Seconds, DateTimeOffset SensorTs);
}
