using League.Api.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

namespace League.Api.Events;

public class ConeHitHandler(LeagueDbContext db) : ICourseEventHandler<ConeHit>
{
    public async Task HandleAsync(ConeHit courseEvent, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.Database.ExecuteSqlAsync($"""
            SELECT pg_advisory_xact_lock(hashtextextended({courseEvent.RunId}::text, 0::bigint))
            """, ct);

        var runExists = await db.Runs.AnyAsync(r => r.Id == courseEvent.RunId, ct);
        if (runExists)
        {
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO penalties (run_id, cone_id, seconds, sensor_ts)
                VALUES ({courseEvent.RunId}, {courseEvent.ConeId}, 5, {courseEvent.SensorTs.ToUniversalTime()})
                ON CONFLICT (run_id, cone_id) DO NOTHING
                """, ct);
        }
        else
        {
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO pending_cone_hits (run_id, cone_id, sensor_ts)
                VALUES ({courseEvent.RunId}, {courseEvent.ConeId}, {courseEvent.SensorTs.ToUniversalTime()})
                ON CONFLICT (run_id, cone_id) DO NOTHING
                """, ct);
        }

        await tx.CommitAsync(ct);
    }
}
