using League.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace League.Api.Controllers;

public record PenaltyEntry(string ConeId, int Seconds, DateTimeOffset SensorTs);

[ApiController]
[Route("api/runs")]
public class RunsController(LeagueDbContext db) : ControllerBase
{
    [HttpGet("{runId:guid}/penalties")]
    public Task<List<PenaltyEntry>> GetPenalties(Guid runId, CancellationToken ct) =>
        db.Penalties
            .Where(p => p.RunId == runId)
            .OrderBy(p => p.SensorTs)
            .Select(p => new PenaltyEntry(p.ConeId, p.Seconds, p.SensorTs))
            .ToListAsync(ct);
}
