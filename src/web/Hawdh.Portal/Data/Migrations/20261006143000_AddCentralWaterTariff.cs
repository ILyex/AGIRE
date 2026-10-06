using Hawdh.Portal.Data;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hawdh.Portal.Data.Migrations;

public sealed partial class AddCentralWaterTariff : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WaterTariffSettings",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false),
                RatePerCubicMetre = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WaterTariffSettings", x => x.Id);
                table.CheckConstraint("CK_WaterTariffSettings_Singleton", "\"Id\" = 1");
            });

        migrationBuilder.InsertData(
            table: "WaterTariffSettings",
            columns: new[] { "Id", "RatePerCubicMetre", "UpdatedAtUtc", "UpdatedBy" },
            values: new object[] { 1, null, DateTime.UtcNow, "" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "WaterTariffSettings");
}
