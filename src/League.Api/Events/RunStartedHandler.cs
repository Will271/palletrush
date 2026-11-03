using League.Api.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

namespace League.Api.Events;

public class RunStartedHandler(LeagueDbContext db) : ICourseEventHandler<RunStarted>
{
    public async Task HandleAsync(RunStarted courseEvent, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.Database.ExecuteSqlAsync($"""
            SELECT pg_advisory_xact_lock(hashtextextended({courseEvent.RunId}::text, 0::bigint))
            """, ct);

        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO runs (id, heat_id, driver_id, started_at)
            VALUES ({courseEvent.RunId}, {courseEvent.HeatId}, {courseEvent.DriverId}, {courseEvent.SensorTs.ToUniversalTime()})
            ON CONFLICT (id) DO NOTHING
            """, ct);

        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO penalties (run_id, cone_id, seconds, sensor_ts)
            SELECT run_id, cone_id, 5, sensor_ts
            FROM pending_cone_hits
            WHERE run_id = {courseEvent.RunId}
            ON CONFLICT (run_id, cone_id) DO NOTHING
            """, ct);

        await db.Database.ExecuteSqlAsync($"""
            DELETE FROM pending_cone_hits
            WHERE run_id = {courseEvent.RunId}
            """, ct);

        await tx.CommitAsync(ct);
    }
}
