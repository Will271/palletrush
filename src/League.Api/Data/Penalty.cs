namespace League.Api.Data;

public class Penalty
{
    public int Id { get; set; }
    public Guid RunId { get; set; }
    public string ConeId { get; set; } = null!;
    public int Seconds { get; set; }
    public DateTimeOffset SensorTs { get; set; }

    public Run Run { get; set; } = null!;
}
