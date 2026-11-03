using System.Text.Json;
using Shared.Contracts;
using StackExchange.Redis;

namespace League.Api.Events;

public class CourseEventsConsumer(
    IConnectionMultiplexer redis,
    IServiceScopeFactory scopeFactory,
    ILogger<CourseEventsConsumer> logger) : BackgroundService
{
    private const string Group = "league";

    private readonly string _consumerName = $"{Environment.MachineName}-{Environment.ProcessId}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = redis.GetDatabase();
        var groupReady = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!groupReady)
                {
                    await CreateGroupAsync(db);
                    groupReady = true;
                }

                var pending = await db.StreamAutoClaimAsync(
                    CourseEvents.Stream,
                    Group,
                    _consumerName,
                    30_000,
                    StreamPosition.Beginning,
                    10);

                foreach (var entry in pending.ClaimedEntries)
                {
                    try
                    {
                        await DispatchAsync(entry, stoppingToken);
                        await db.StreamAcknowledgeAsync(CourseEvents.Stream, Group, entry.Id);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to handle pending course event {EntryId}", entry.Id);
                    }
                }

                var entries = await db.StreamReadGroupAsync(CourseEvents.Stream, Group, _consumerName, ">", count: 10);

                foreach (var entry in entries)
                {
                    try
                    {
                        await DispatchAsync(entry, stoppingToken);
                        await db.StreamAcknowledgeAsync(CourseEvents.Stream, Group, entry.Id);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to handle course event {EntryId}", entry.Id);
                    }
                }

                if (entries.Length == 0)
                    await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Failed to read from {Stream}", CourseEvents.Stream);
                groupReady = false;
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }

    private async Task CreateGroupAsync(IDatabase db)
    {
        try
        {
            await db.StreamCreateConsumerGroupAsync(CourseEvents.Stream, Group, StreamPosition.Beginning, createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.StartsWith("BUSYGROUP", StringComparison.Ordinal))
        {
        }
    }

    private async Task DispatchAsync(StreamEntry entry, CancellationToken ct)
    {
        var type = (string?)entry["type"];
        var payload = (string?)entry["payload"] ?? "";

        using var scope = scopeFactory.CreateScope();
        switch (type)
        {
            case nameof(RunStarted):
                await HandleAsync<RunStarted>(scope.ServiceProvider, payload, ct);
                break;
            case nameof(ConeHit):
                await HandleAsync<ConeHit>(scope.ServiceProvider, payload, ct);
                break;
            case nameof(RunFinished):
                await HandleAsync<RunFinished>(scope.ServiceProvider, payload, ct);
                break;
            default:
                logger.LogWarning("Unknown course event type {Type} in {EntryId}", type, entry.Id);
                break;
        }
    }

    private static Task HandleAsync<T>(IServiceProvider services, string payload, CancellationToken ct)
    {
        var courseEvent = JsonSerializer.Deserialize<T>(payload, CourseEvents.JsonOptions)!;
        return services.GetRequiredService<ICourseEventHandler<T>>().HandleAsync(courseEvent, ct);
    }
}
