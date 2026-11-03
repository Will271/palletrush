using League.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace League.Api.Services;

public record RegistrationEntry(int Id, int DriverId, string DriverName, string TeamName, DateTimeOffset CreatedAt);

public enum RegistrationResult
{
    Registered,
    HeatNotFound,
    HeatFull,
    DriverNotEligible,

    AlreadyRegistered,
}

public class RegistrationService(LeagueDbContext db)
{
    public Task<List<RegistrationEntry>> GetAsync(int heatId, CancellationToken ct) =>
        db.Registrations
            .Where(r => r.HeatId == heatId)
            .OrderBy(r => r.Id)
            .Select(r => new RegistrationEntry(r.Id, r.DriverId, r.Driver.Name, r.Driver.Team.Name, r.CreatedAt))
            .ToListAsync(ct);


    public async Task<RegistrationResult> RegisterAsync(
        int heatId,
        int driverId,
        CancellationToken ct)
    {
        await using var tx =
            await db.Database.BeginTransactionAsync(ct);

        // 1. Validate heat
        var heatExists = await db.Heats
            .AnyAsync(h => h.Id == heatId, ct);

        if (!heatExists)
        {
            return RegistrationResult.HeatNotFound;
        }

        // 2. Idempotency: already registered
        var alreadyRegistered = await db.Registrations
            .AnyAsync(
                r => r.HeatId == heatId &&
                     r.DriverId == driverId,
                ct);

        if (alreadyRegistered) {
            return RegistrationResult.AlreadyRegistered;
        }

        // 3. Validate driver eligibility
        var eligible = await db.Drivers
            .AnyAsync(
                d => d.Id == driverId && d.Team.Active,
                ct);

        if (!eligible){
            return RegistrationResult.DriverNotEligible;
        }

        // 4. Atomically reserve a spot
        var updated = await db.Heats
            .Where(h =>
                h.Id == heatId &&
                h.RegisteredCount < h.Capacity)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    h => h.RegisteredCount,
                    h => h.RegisteredCount + 1),
                ct);

        if (updated == 0)
        {
            // Check whether a concurrent request registered this driver.
            alreadyRegistered = await db.Registrations
                .AnyAsync(
                    r => r.HeatId == heatId &&
                         r.DriverId == driverId,
                    ct);

            if (alreadyRegistered) {
                return RegistrationResult.AlreadyRegistered;
            }

            // Distinguish a missing heat from a full heat.
            heatExists = await db.Heats
                .AnyAsync(h => h.Id == heatId, ct);

            return heatExists
                ? RegistrationResult.HeatFull
                : RegistrationResult.HeatNotFound;
        }

        // 5. Insert registration
        db.Registrations.Add(new Registration
        {
            HeatId = heatId,
            DriverId = driverId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (IsDuplicateRegistration(ex))
        {
            await tx.RollbackAsync(CancellationToken.None);
            return RegistrationResult.AlreadyRegistered;
        }

        // 6. Commit both changes
        await tx.CommitAsync(ct);
        return RegistrationResult.Registered;
    }

    private static bool IsDuplicateRegistration(
        DbUpdateException ex)
    {
        return ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_registrations_heat_driver"
        };
    }
}
