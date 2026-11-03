using League.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace League.Api.Services;

public record LeaderboardEntry(
    Guid RunId,
    int DriverId,
    string DriverName,
    string TeamName,
    int RawTimeMs,
    int PenaltyCount,
    int PenaltySeconds,
    int TotalTimeMs,
    int Rank);

public class LeaderboardService(LeagueDbContext db)
{
    public async Task<List<LeaderboardEntry>> GetAsync(int heatId, CancellationToken ct)
    {
        var runs = await db.Runs
            .Where(r => r.HeatId == heatId && r.RawTimeMs != null)
            .Select(r => new
            {
                RunId = r.Id,
                r.DriverId,
                DriverName = r.Driver.Name,
                TeamName = r.Driver.Team.Name,
                RawTimeMs = r.RawTimeMs!.Value,
                r.FinishedAt,
                PenaltyCount = db.Penalties.Count(p => p.RunId == r.Id)
            })
            .ToListAsync(ct);

        var ordered = runs
            .Select(r => new
            {
                r.RunId,
                r.DriverId,
                r.DriverName,
                r.TeamName,
                r.RawTimeMs,
                r.FinishedAt,
                r.PenaltyCount,
                PenaltySeconds = r.PenaltyCount * 5,
                TotalTimeMs = r.RawTimeMs + r.PenaltyCount * 5000
            })
            .OrderBy(r => r.TotalTimeMs)
            .ThenBy(r => r.PenaltyCount)
            .ThenBy(r => r.FinishedAt)
            .ToList();

        return ordered
            .Select((r, index) => new LeaderboardEntry(
                r.RunId,
                r.DriverId,
                r.DriverName,
                r.TeamName,
                r.RawTimeMs,
                r.PenaltyCount,
                r.PenaltySeconds,
                r.TotalTimeMs,
                index + 1))
            .ToList();
    }
}
