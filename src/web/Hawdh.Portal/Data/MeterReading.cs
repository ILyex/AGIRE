namespace Hawdh.Portal.Data;

public sealed class MeterReading
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateOnly Period { get; set; }
    public DateOnly PreviousReadOn { get; set; }
    public decimal PreviousReading { get; set; }
    public DateOnly CurrentReadOn { get; set; }
    public decimal CurrentReading { get; set; }
    public decimal Rate { get; set; }
    public decimal Consumption => CurrentReading - PreviousReading;
    public decimal Charge => decimal.Round(Consumption * Rate, 2, MidpointRounding.AwayFromZero);
    public string? Note { get; set; }
    public string RecordedBy { get; set; } = "";
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
