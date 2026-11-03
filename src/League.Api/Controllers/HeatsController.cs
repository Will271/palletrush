using League.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace League.Api.Controllers;

public record RegisterRequest(int DriverId);

[ApiController]
[Route("api/heats")]
public class HeatsController(
    HeatService heats,
    RegistrationService registrations,
    LeaderboardService leaderboard) : ControllerBase
{
    [HttpGet]
    public Task<List<HeatSummary>> GetAll(CancellationToken ct) =>
        heats.GetAllAsync(ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<HeatSummary>> Get(int id, CancellationToken ct) =>
        await heats.GetAsync(id, ct) is { } heat ? heat : NotFound();

    [HttpGet("{id:int}/registrations")]
    public Task<List<RegistrationEntry>> GetRegistrations(int id, CancellationToken ct) =>
        registrations.GetAsync(id, ct);

    [HttpPost("{id:int}/registrations")]
    public async Task<IActionResult> Register(int id, RegisterRequest request, CancellationToken ct)
    {
        var result = await registrations.RegisterAsync(id, request.DriverId, ct);
        return result switch
        {
            RegistrationResult.Registered => Created(),
            RegistrationResult.HeatNotFound => NotFound(),
            RegistrationResult.HeatFull => Problem(title: "Heat is full", statusCode: StatusCodes.Status409Conflict),
            RegistrationResult.AlreadyRegistered => Problem(title: "Driver already registered for this heat", statusCode: StatusCodes.Status409Conflict),
            _ => Problem(title: "Driver is not eligible", statusCode: StatusCodes.Status422UnprocessableEntity),
        };
    }

    [HttpGet("{id:int}/leaderboard")]
    public Task<List<LeaderboardEntry>> GetLeaderboard(int id, CancellationToken ct) =>
        leaderboard.GetAsync(id, ct);
}
