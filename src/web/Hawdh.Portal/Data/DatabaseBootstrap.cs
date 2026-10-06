using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hawdh.Portal.Data;

public static class DatabaseBootstrap
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, ILogger logger, bool development)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (development && configuration["Database:Provider"] == "Sqlite")
        {
            await db.Database.EnsureCreatedAsync();
            await EnsureDevelopmentCustomerFieldsAsync(db);
            await EnsureDevelopmentWaterTariffTableAsync(db);
        }
        else
            await db.Database.MigrateAsync();

        if (!await db.WaterTariffSettings.AnyAsync(setting => setting.Id == 1))
        {
            db.WaterTariffSettings.Add(new WaterTariffSetting { Id = 1, RatePerCubicMetre = null, UpdatedBy = "" });
            await db.SaveChangesAsync();
        }

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var name in new[] { "Administrator", "Billing", "ReadOnly" })
            if (!await roles.RoleExistsAsync(name))
            {
                var roleResult = await roles.CreateAsync(new IdentityRole(name));
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException($"Required role '{name}' could not be created: " + string.Join("; ", roleResult.Errors.Select(error => error.Code)));
            }

        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (!development && !await users.Users.AnyAsync())
                throw new InvalidOperationException("No users exist. Configure SeedAdmin__Email and SeedAdmin__Password for the initial production administrator.");
            logger.LogWarning("No bootstrap administrator configured. Set SeedAdmin__Email and SeedAdmin__Password before enabling user access.");
            return;
        }

        var existingAdmin = await users.FindByEmailAsync(email);
        if (existingAdmin is not null)
        {
            if (development && !string.IsNullOrWhiteSpace(password))
            {
                var resetToken = await users.GeneratePasswordResetTokenAsync(existingAdmin);
                var passwordReset = await users.ResetPasswordAsync(existingAdmin, resetToken, password);
                if (!passwordReset.Succeeded)
                    throw new InvalidOperationException("Bootstrap administrator password could not be updated: " + string.Join("; ", passwordReset.Errors.Select(error => error.Code)));
            }
            if (string.IsNullOrWhiteSpace(existingAdmin.DisplayName)
                || string.Equals(existingAdmin.DisplayName.Trim(), "Administrator", StringComparison.OrdinalIgnoreCase))
            {
                existingAdmin.DisplayName = "MOUTFI AHMED";
                var profileUpdate = await users.UpdateAsync(existingAdmin);
                if (!profileUpdate.Succeeded)
                    throw new InvalidOperationException("Bootstrap administrator profile update failed: " + string.Join("; ", profileUpdate.Errors.Select(error => error.Code)));
            }

            if (!await users.IsInRoleAsync(existingAdmin, "Administrator"))
            {
                var assignment = await users.AddToRoleAsync(existingAdmin, "Administrator");
                if (!assignment.Succeeded)
                    throw new InvalidOperationException("Bootstrap administrator role assignment failed: " + string.Join("; ", assignment.Errors.Select(error => error.Code)));
            }
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email.Trim(),
            Email = email.Trim(),
            EmailConfirmed = true,
            DisplayName = "MOUTFI AHMED",
            IsActive = true,
            LockoutEnabled = true
        };
        var result = await users.CreateAsync(admin, password);
        if (!result.Succeeded)
            throw new InvalidOperationException("Bootstrap administrator could not be created: " + string.Join("; ", result.Errors.Select(error => error.Code)));
        var roleAssignment = await users.AddToRoleAsync(admin, "Administrator");
        if (!roleAssignment.Succeeded)
            throw new InvalidOperationException("Bootstrap administrator role assignment failed: " + string.Join("; ", roleAssignment.Errors.Select(error => error.Code)));
        logger.LogInformation("Bootstrap administrator account created. Change its initial password after first sign-in.");
    }

    private static async Task EnsureDevelopmentWaterTariffTableAsync(ApplicationDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "WaterTariffSettings" (
                "Id" INTEGER NOT NULL PRIMARY KEY CHECK ("Id" = 1),
                "RatePerCubicMetre" TEXT NULL,
                "UpdatedAtUtc" TEXT NOT NULL,
                "UpdatedBy" TEXT NOT NULL
            );
            """);
    }

    // EnsureCreated does not evolve an existing local SQLite database. Keep this
    // additive compatibility step until development databases use migrations.
    private static async Task EnsureDevelopmentCustomerFieldsAsync(ApplicationDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var closeAfter = connection.State != System.Data.ConnectionState.Open;
        if (closeAfter) await connection.OpenAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA table_info('Customers');";
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync()) columns.Add(reader.GetString(1));

            if (!columns.Contains("MeterNumber"))
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Customers\" ADD COLUMN \"MeterNumber\" TEXT NULL;");
            if (!columns.Contains("SubscriptionYear"))
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Customers\" ADD COLUMN \"SubscriptionYear\" INTEGER NULL;");
        }
        finally
        {
            if (closeAfter) await connection.CloseAsync();
        }
    }
}
