namespace Hawdh.Portal.Data;

/// <summary>The authoritative per-cubic-metre water tariff. Id=1 is the singleton setting.</summary>
public sealed class WaterTariffSetting
{
    public int Id { get; set; } = 1;
    public decimal? RatePerCubicMetre { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "";
}
