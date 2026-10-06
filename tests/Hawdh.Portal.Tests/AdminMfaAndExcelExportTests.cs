using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Hawdh.Portal.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hawdh.Portal.Tests;

public sealed class AdminMfaAndExcelExportTests : IClassFixture<PortalFactory>
{
    private readonly PortalFactory _factory;

    public AdminMfaAndExcelExportTests(PortalFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_requires_mfa_while_billing_user_can_export_formula_workbooks()
    {
        await CreateBillingUserAndDataAsync();

        using var adminClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var adminLogin = await SignInAsync(adminClient, PortalFactory.AdminEmail, PortalFactory.AdminPassword);
        Assert.Equal(HttpStatusCode.Found, adminLogin.StatusCode);

        var deniedHome = await adminClient.GetAsync("/");
        Assert.Equal(HttpStatusCode.Found, deniedHome.StatusCode);
        Assert.Contains("/Account/AccessDenied", deniedHome.Headers.Location?.ToString());

        var setupPage = await adminClient.GetAsync("/Account/Manage/TwoFactorAuthentication");
        Assert.Equal(HttpStatusCode.OK, setupPage.StatusCode);

        var deniedExport = await adminClient.GetAsync("/reports/export.xlsx");
        Assert.Equal(HttpStatusCode.Found, deniedExport.StatusCode);
        Assert.Contains("/Account/AccessDenied", deniedExport.Headers.Location?.ToString());

        using var billingClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var billingLogin = await SignInAsync(billingClient, PortalFactory.BillingEmail, PortalFactory.BillingPassword);
        Assert.Equal(HttpStatusCode.Found, billingLogin.StatusCode);

        var customersPage = await billingClient.GetAsync("/customers");
        Assert.Equal(HttpStatusCode.OK, customersPage.StatusCode);
        Assert.True(customersPage.Headers.CacheControl?.NoStore, "Authenticated customer data must not be cached by the browser or intermediary.");
        Assert.Equal("nosniff", customersPage.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", customersPage.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("default-src 'self'", customersPage.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);

        var reportResponse = await billingClient.GetAsync("/reports/export.xlsx");
        Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);
        Assert.StartsWith("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", reportResponse.Content.Headers.ContentType?.ToString());
        using var reportStream = await reportResponse.Content.ReadAsStreamAsync();
        using var report = new XLWorkbook(reportStream);
        AssertDocumentFonts(report);
        Assert.Equal(XLCalculateMode.Auto, report.CalculateMode);

        var invoiceSheet = FindSheetByHeader(report, "رقم الفاتورة", "N° facture");
        Assert.StartsWith("MAX(", invoiceSheet.Cell(5, 10).FormulaA1, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("IF(", invoiceSheet.Cell(5, 11).FormulaA1, StringComparison.OrdinalIgnoreCase);

        var usageSheet = FindSheetByHeader(report, "الاسم واللقب", "Nom et prénom");
        Assert.StartsWith("MAX(", usageSheet.Cell(5, 7).FormulaA1, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("ROUND(", usageSheet.Cell(5, 9).FormulaA1, StringComparison.OrdinalIgnoreCase);

        var customerResponse = await billingClient.GetAsync("/customers/export.xlsx");
        Assert.Equal(HttpStatusCode.OK, customerResponse.StatusCode);
        using var customerStream = await customerResponse.Content.ReadAsStreamAsync();
        using var customers = new XLWorkbook(customerStream);
        AssertDocumentFonts(customers);
        var customerSheet = customers.Worksheet(1);
        Assert.StartsWith("MAX(", customerSheet.Cell(5, 11).FormulaA1, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("ROUND(", customerSheet.Cell(5, 13).FormulaA1, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("MAX(", customerSheet.Cell(5, 16).FormulaA1, StringComparison.OrdinalIgnoreCase);
        var totalsRow = customerSheet.RowsUsed()
            .First(row => row.Cell(1).GetString() is "الإجمالي" or "Total")
            .RowNumber();
        Assert.StartsWith("SUM(", customerSheet.Cell(totalsRow, 16).FormulaA1, StringComparison.OrdinalIgnoreCase);

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var billingUser = await users.FindByEmailAsync(PortalFactory.BillingEmail);
        Assert.NotNull(billingUser);
        const string replacementEmail = "billing-updated@example.local";
        var emailPage = await billingClient.GetStringAsync("/Account/Manage/Email");
        var emailInputIndex = emailPage.IndexOf("name=\"Input.NewEmail\"", StringComparison.Ordinal);
        Assert.True(emailInputIndex >= 0, "The email change form did not render its new-address field.");
        var emailFormStart = emailPage.LastIndexOf("<form", emailInputIndex, StringComparison.OrdinalIgnoreCase);
        var emailFormEnd = emailPage.IndexOf("</form>", emailInputIndex, StringComparison.OrdinalIgnoreCase);
        Assert.True(emailFormStart >= 0 && emailFormEnd > emailInputIndex, "The email change form was not rendered correctly.");
        var emailFormHtml = emailPage[emailFormStart..(emailFormEnd + "</form>".Length)];
        var emailFormFields = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = GetInputValue(emailFormHtml, "__RequestVerificationToken"),
            ["_handler"] = GetInputValue(emailFormHtml, "_handler")
        };
        emailFormFields["Input.NewEmail"] = PortalFactory.AdminEmail;
        var duplicateEmailRequest = await billingClient.PostAsync("/Account/Manage/Email", new FormUrlEncodedContent(emailFormFields));
        Assert.Equal(HttpStatusCode.OK, duplicateEmailRequest.StatusCode);
        var duplicateEmailHtml = await duplicateEmailRequest.Content.ReadAsStringAsync();
        Assert.Contains("هذا البريد الإلكتروني مرتبط بحساب آخر.", WebUtility.HtmlDecode(Regex.Replace(duplicateEmailHtml, "<[^>]+>", " ")));

        var emailBeforeChange = await users.Users.AsNoTracking().SingleAsync(user => user.Id == billingUser!.Id);
        Assert.Equal(PortalFactory.BillingEmail, emailBeforeChange.Email);
        var recordingEmailSender = _factory.Services.GetRequiredService<RecordingIdentityEmailSender>();
        Assert.Null(recordingEmailSender.LastConfirmationEmail);

        emailFormFields["Input.NewEmail"] = replacementEmail;
        var emailChangeRequest = await billingClient.PostAsync("/Account/Manage/Email", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = emailFormFields["__RequestVerificationToken"],
            ["_handler"] = emailFormFields["_handler"],
            ["Input.NewEmail"] = emailFormFields["Input.NewEmail"]
        }));
        Assert.Equal(HttpStatusCode.OK, emailChangeRequest.StatusCode);
        Assert.Equal(replacementEmail, recordingEmailSender.LastConfirmationEmail);
        Assert.False(string.IsNullOrWhiteSpace(recordingEmailSender.LastConfirmationLink));
        var confirmationUri = new Uri(recordingEmailSender.LastConfirmationLink!);
        Assert.Equal(replacementEmail, Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(confirmationUri.Query)["email"].ToString());

        var invalidConfirmationParameters = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(confirmationUri.Query)
            .ToDictionary(parameter => parameter.Key, parameter => (string?)(parameter.Key == "code" ? "tampered-token" : parameter.Value.ToString()));
        var invalidConfirmationUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
            confirmationUri.GetLeftPart(UriPartial.Path), invalidConfirmationParameters);
        var invalidConfirmationResponse = await billingClient.GetAsync(new Uri(invalidConfirmationUrl).PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, invalidConfirmationResponse.StatusCode);
        var userAfterInvalidLink = await users.Users.AsNoTracking().SingleAsync(user => user.Id == billingUser!.Id);
        Assert.Equal(PortalFactory.BillingEmail, userAfterInvalidLink.Email);
        Assert.Equal(PortalFactory.BillingEmail, userAfterInvalidLink.UserName);

        var confirmationResponse = await billingClient.GetAsync(confirmationUri.PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, confirmationResponse.StatusCode);
        var confirmationHtml = await confirmationResponse.Content.ReadAsStringAsync();
        var visibleConfirmationText = WebUtility.HtmlDecode(Regex.Replace(
            Regex.Replace(confirmationHtml, "<script\\b[^>]*>[\\s\\S]*?</script>", "", RegexOptions.IgnoreCase),
            "<[^>]+>", " "));
        Assert.True(visibleConfirmationText.Contains("تم تأكيد البريد", StringComparison.Ordinal),
            visibleConfirmationText[..Math.Min(900, visibleConfirmationText.Length)]);
        var updatedUser = await users.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Email == replacementEmail);
        Assert.NotNull(updatedUser);
        Assert.Equal(replacementEmail, updatedUser.UserName);
        Assert.True(updatedUser.EmailConfirmed);
    }

    private async Task CreateBillingUserAndDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = new ApplicationUser
        {
            UserName = PortalFactory.AdminEmail,
            Email = PortalFactory.AdminEmail,
            EmailConfirmed = true,
            DisplayName = "MFA Test Administrator",
            IsActive = true
        };
        var adminCreated = await users.CreateAsync(admin, PortalFactory.AdminPassword);
        Assert.True(adminCreated.Succeeded, string.Join(", ", adminCreated.Errors.Select(error => error.Code)));
        var adminRole = await users.AddToRoleAsync(admin, "Administrator");
        Assert.True(adminRole.Succeeded, string.Join(", ", adminRole.Errors.Select(error => error.Code)));

        var billingUser = new ApplicationUser
        {
            UserName = PortalFactory.BillingEmail,
            Email = PortalFactory.BillingEmail,
            EmailConfirmed = true,
            DisplayName = "Billing Test"
        };
        var created = await users.CreateAsync(billingUser, PortalFactory.BillingPassword);
        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(error => error.Code)));
        var role = await users.AddToRoleAsync(billingUser, "Billing");
        Assert.True(role.Succeeded, string.Join(", ", role.Errors.Select(error => error.Code)));

        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Code = "TEST-EXCEL-001",
            Name = "Excel Formula Sample",
            MeterNumber = "M-0001",
            SubscriptionYear = DateTime.Today.Year,
            Location = "Test sector",
            IsActive = true,
            CreatedAtUtc = now,
            CreatedBy = PortalFactory.BillingEmail
        };
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Number = "TEST-INV-001",
            Customer = customer,
            Description = "Formula verification",
            CurrencyCode = "DZD",
            Amount = 1000m,
            IssuedOn = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1),
            DueOn = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            CreatedBy = PortalFactory.BillingEmail,
            CreatedAtUtc = now
        };
        db.Customers.Add(customer);
        db.Invoices.Add(invoice);
        db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            Invoice = invoice,
            Amount = 250m,
            PaidOn = invoice.IssuedOn.AddDays(1),
            Reference = "TEST-PAY-001",
            RecordedBy = PortalFactory.BillingEmail,
            RecordedAtUtc = now
        });
        db.MeterReadings.Add(new MeterReading
        {
            Id = Guid.NewGuid(),
            Customer = customer,
            Period = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1),
            PreviousReadOn = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)),
            PreviousReading = 40m,
            CurrentReadOn = DateOnly.FromDateTime(DateTime.Today),
            CurrentReading = 55m,
            Rate = 10m,
            RecordedBy = PortalFactory.BillingEmail,
            RecordedAtUtc = now
        });
        await db.SaveChangesAsync();
    }

    private static async Task<HttpResponseMessage> SignInAsync(HttpClient client, string email, string password)
    {
        var page = await client.GetStringAsync("/Account/Login");
        var token = GetInputValue(page, "__RequestVerificationToken");
        var handler = GetInputValue(page, "_handler");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["_handler"] = handler,
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.RememberMe"] = "false"
        });
        return await client.PostAsync("/Account/Login", form);
    }

    private static string GetInputValue(string html, string name)
    {
        var input = Regex.Matches(html, "<input[^>]+>")
            .Select(match => match.Value)
            .FirstOrDefault(value => value.Contains($"name=\"{name}\"", StringComparison.Ordinal));
        Assert.NotNull(input);
        var value = Regex.Match(input!, "value=\"([^\"]*)\"");
        Assert.True(value.Success, $"Input {name} did not include a value.");
        return WebUtility.HtmlDecode(value.Groups[1].Value);
    }

    private static IXLWorksheet FindSheetByHeader(XLWorkbook workbook, params string[] headers)
    {
        foreach (var sheet in workbook.Worksheets)
            if (headers.Contains(sheet.Cell(4, 1).GetString(), StringComparer.Ordinal))
                return sheet;
        throw new Xunit.Sdk.XunitException($"Could not find a worksheet with header {string.Join(" / ", headers)}.");
    }

    private static void AssertDocumentFonts(XLWorkbook workbook)
    {
        var allowedFonts = new[] { "Traditional Arabic", "Arabic Typesetting" };
        var usedFonts = workbook.Worksheets
            .SelectMany(sheet => sheet.CellsUsed())
            .Select(cell => cell.Style.Font.FontName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.NotEmpty(usedFonts);
        Assert.All(usedFonts, font => Assert.Contains(font, allowedFonts, StringComparer.OrdinalIgnoreCase));
    }
}

public sealed class PortalFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public const string AdminEmail = "admin-test@example.local";
    public const string AdminPassword = "AdminTest2026";
    public const string BillingEmail = "billing-test@example.local";
    public const string BillingPassword = "BillingTest2026";

    public PortalFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextFactory<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlite(_connection));
            services.RemoveAll<IEmailSender<ApplicationUser>>();
            services.AddSingleton<RecordingIdentityEmailSender>();
            services.AddSingleton<IEmailSender<ApplicationUser>>(provider => provider.GetRequiredService<RecordingIdentityEmailSender>());
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

public sealed class RecordingIdentityEmailSender : IEmailSender<ApplicationUser>
{
    public string? LastConfirmationEmail { get; private set; }
    public string? LastConfirmationLink { get; private set; }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        LastConfirmationEmail = email;
        LastConfirmationLink = confirmationLink;
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) => Task.CompletedTask;

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) => Task.CompletedTask;
}
