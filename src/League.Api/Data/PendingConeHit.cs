namespace League.Api.Data;

public class PendingConeHit
{
    public int Id { get; set; }
    public Guid RunId { get; set; }
    public string ConeId { get; set; } = null!;
    public DateTimeOffset SensorTs { get; set; }
}
