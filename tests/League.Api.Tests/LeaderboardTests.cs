using System.Net.Http.Json;
using Shared.Contracts;

namespace League.Api.Tests;

public class LeaderboardTests
{
    private const int HeatId = 2;

    [Test]
    public async Task Leaderboard_ranks_by_total_time_then_penalties_then_finish_time()
    {
        var t0 = new DateTimeOffset(2026, 11, 7, 9, 30, 0, TimeSpan.Zero);

        // total 80000ms, 4 penalties, finishedAt t0+60s
        var earliestFinish = await CompleteRunAsync(30, t0, t0.AddSeconds(60), penaltyCount: 4);
        // total 80000ms, 4 penalties, finishedAt t0+70s -> loses tiebreak on FinishedAt
        var laterFinish = await CompleteRunAsync(31, t0.AddSeconds(10), t0.AddSeconds(70), penaltyCount: 4);
        // total 85000ms, 1 penalty
        var fewerPenalties = await CompleteRunAsync(32, t0, t0.AddSeconds(80), penaltyCount: 1);
        // total 85000ms, 2 penalties -> loses tiebreak on PenaltyCount
        var morePenalties = await CompleteRunAsync(33, t0, t0.AddSeconds(75), penaltyCount: 2);
        // total 100000ms, 0 penalties
        var clean = await CompleteRunAsync(34, t0, t0.AddSeconds(100), penaltyCount: 0);
        // total 100000ms, 2 penalties -> loses tiebreak on PenaltyCount
        var penalized = await CompleteRunAsync(35, t0, t0.AddSeconds(90), penaltyCount: 2);

        // Unfinished run must be excluded from the leaderboard.
        var unfinished = Guid.NewGuid();
        await TestEnvironment.HandleAsync(new RunStarted(Guid.NewGuid(), unfinished, HeatId, 36, t0, "v2"));

        var expectedOrder = new[] { earliestFinish, laterFinish, fewerPenalties, morePenalties, clean, penalized };

        using var client = TestEnvironment.Factory.CreateClient();
        var leaderboard = await client.GetFromJsonAsync<List<LeaderboardRow>>($"/api/heats/{HeatId}/leaderboard");
        Assert.That(leaderboard, Is.Not.Null);

        // Heat 2 has 2 seeded finished runs; this test adds 6 more.
        Assert.That(leaderboard!.Count, Is.EqualTo(8));

        var mine = leaderboard.Where(r => expectedOrder.Contains(r.RunId)).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(mine.Select(r => r.RunId), Is.EqualTo(expectedOrder));
            Assert.That(mine.All(r => leaderboard[r.Rank - 1].RunId == r.RunId), Is.True,
                "Rank must match the entry's position in the leaderboard");
            Assert.That(leaderboard.Any(r => r.RunId == unfinished), Is.False);
        }
    }

    private static async Task<Guid> CompleteRunAsync(int driverId, DateTimeOffset startedAt, DateTimeOffset finishedAt, int penaltyCount)
    {
        var runId = Guid.NewGuid();
        await TestEnvironment.HandleAsync(new RunStarted(Guid.NewGuid(), runId, HeatId, driverId, startedAt, "v2"));
        for (var i = 0; i < penaltyCount; i++)
            await TestEnvironment.HandleAsync(new ConeHit(Guid.NewGuid(), runId, $"C{i + 1}", startedAt.AddSeconds(5 + i), "v2"));
        await TestEnvironment.HandleAsync(new RunFinished(Guid.NewGuid(), runId, finishedAt, "v2"));
        return runId;
    }

    private record LeaderboardRow(Guid RunId, int RawTimeMs, int PenaltyCount, int PenaltySeconds, int TotalTimeMs, int Rank);
}
