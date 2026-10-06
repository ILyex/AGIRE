using Hawdh.Portal.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hawdh.Portal.Data.Migrations;

public sealed partial class AddCustomerMeterDetails : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MeterNumber",
            table: "Customers",
            type: "character varying(80)",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "SubscriptionYear",
            table: "Customers",
            type: "integer",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "MeterNumber", table: "Customers");
        migrationBuilder.DropColumn(name: "SubscriptionYear", table: "Customers");
    }
}
