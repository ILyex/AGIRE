using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using Hawdh.Portal.Components;
using Hawdh.Portal.Components.Account;
using Hawdh.Portal.Data;

var builder = WebApplication.CreateBuilder(args);
var smtpEmailOptions = builder.Configuration.GetSection("Smtp").Get<SmtpEmailOptions>() ?? new SmtpEmailOptions();
var isDevelopment = builder.Environment.IsDevelopment();
var isLocal = builder.Environment.IsEnvironment("Local");
var isProduction = !isDevelopment && !isLocal;
var twoFactorEnabled = builder.Configuration.GetValue<bool>("Security:EnableTwoFactor");

if (isProduction)
{
    var allowedHosts = builder.Configuration["AllowedHosts"];
    if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Contains("*"))
        throw new InvalidOperationException("Production requires an explicit AllowedHosts list (semicolon-separated host names); wildcard hosts are not permitted.");
    if (!string.Equals(builder.Configuration["Database:Provider"], "PostgreSQL", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Production requires Database:Provider=PostgreSQL.");
    if (!IPAddress.TryParse(builder.Configuration["ReverseProxy:KnownProxy"], out var trustedProxy))
        throw new InvalidOperationException("Production requires ReverseProxy:KnownProxy to be the trusted reverse proxy IP address.");
    if (!smtpEmailOptions.IsProductionReady)
        throw new InvalidOperationException("Production requires valid SMTP settings with TLS: Smtp:Host, Smtp:FromAddress, Smtp:Port and Smtp:Security (StartTls or SslOnConnect). If SMTP authentication is used, set both Smtp:UserName and Smtp:Password.");
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        options.KnownProxies.Add(trustedProxy);
        options.ForwardLimit = 1;
    });
}

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (isProduction && string.IsNullOrWhiteSpace(dataProtectionKeysPath))
    throw new InvalidOperationException("Production requires a persistent DataProtection:KeysPath.");

var dataProtection = builder.Services.AddDataProtection().SetApplicationName("Hawdh.Portal");
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    var fullKeysPath = Path.GetFullPath(dataProtectionKeysPath);
    Directory.CreateDirectory(fullKeysPath);
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(fullKeysPath));
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Hawdh.Portal.Localization.UiText>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = isProduction ? "__Host-Hawdh.Antiforgery" : "Hawdh.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = isProduction ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
});
builder.Services.AddAuthorization(options =>
{
    var adminMfaPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireAssertion(context => !twoFactorEnabled || isDevelopment || isLocal || !context.User.IsInRole("Administrator")
            || context.User.HasClaim("hawdh:mfa", "true"))
        .Build();
    options.DefaultPolicy = adminMfaPolicy;
    options.FallbackPolicy = adminMfaPolicy;
    options.AddPolicy("AdminMfa", adminMfaPolicy);
    options.AddPolicy("AccountSecurity", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("TwoFactorFeature", policy => policy.RequireAuthenticatedUser()
        .RequireAssertion(_ => twoFactorEnabled));
});
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = isProduction ? "__Host-Hawdh.Session" : "Hawdh.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = isProduction ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Path = "/";
    options.Cookie.IsEssential = true;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
if ((isDevelopment || isLocal) && builder.Configuration["Database:Provider"] == "Sqlite")
{
    Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
    var sqlite = new SqliteConnectionStringBuilder(connectionString);
    if (!Path.IsPathRooted(sqlite.DataSource)) sqlite.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqlite.DataSource);
    connectionString = sqlite.ToString();
}
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    if ((isDevelopment || isLocal) && builder.Configuration["Database:Provider"] == "Sqlite")
        options.UseSqlite(connectionString);
    else
        options.UseNpgsql(connectionString);
});
if (isDevelopment)
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        // Users are provisioned by an administrator and marked confirmed.
        // Keep unverified external/self-service accounts out until a real mail sender exists.
        options.SignIn.RequireConfirmedAccount = true;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = isLocal || isDevelopment ? 8 : 12;
        options.Password.RequireDigit = !isLocal && !isDevelopment;
        options.Password.RequireLowercase = !isLocal && !isDevelopment;
        options.Password.RequireUppercase = !isLocal && !isDevelopment;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationClaimsPrincipalFactory>();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(5));

builder.Services.Configure<SmtpEmailOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender<ApplicationUser>, SmtpIdentityEmailSender>();

var app = builder.Build();
app.UseForwardedHeaders();

// Bound login and email confirmation submissions by the client address. ASP.NET Identity's
// per-account lockout remains the second independent login limit.
var loginLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
{
    var isEmailRequest = context.Request.Path.Equals("/Account/ResendEmailConfirmation", StringComparison.OrdinalIgnoreCase);
    var key = (context.Connection.RemoteIpAddress?.ToString() ?? "unknown") + (isEmailRequest ? ":email" : ":login");
    return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = isEmailRequest ? 5 : 30,
        Window = TimeSpan.FromMinutes(15),
        QueueLimit = 0,
        AutoReplenishment = true
    });
});
app.Lifetime.ApplicationStopped.Register(loginLimiter.Dispose);
app.Use(async (context, next) =>
{
    var isLoginRequest = context.Request.Path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase);
    var isEmailRequest = context.Request.Path.Equals("/Account/ResendEmailConfirmation", StringComparison.OrdinalIgnoreCase);
    if (HttpMethods.IsPost(context.Request.Method) && (isLoginRequest || isEmailRequest))
    {
        using var lease = await loginLimiter.AcquireAsync(context, 1, context.RequestAborted);
        if (!lease.IsAcquired)
        {
            context.Response.Headers.RetryAfter = "900";
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return;
        }
    }

    await next();
});

await DatabaseBootstrap.InitializeAsync(app.Services, app.Configuration, app.Logger, isDevelopment || isLocal);

var supportedCultures = new[] { new CultureInfo("ar-DZ"), new CultureInfo("fr-DZ") };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("ar-DZ")
    .AddSupportedCultures(supportedCultures.Select(culture => culture.Name).ToArray())
    .AddSupportedUICultures(supportedCultures.Select(culture => culture.Name).ToArray());
localizationOptions.RequestCultureProviders = [new CookieRequestCultureProvider()];
app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (isDevelopment)
{
    app.UseMigrationsEndPoint();
}
else
{
    if (isProduction)
        app.UseHsts();
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
if (!isLocal)
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; form-action 'self'; img-src 'self' data:; font-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; connect-src 'self' ws: wss:";
        if (context.User.Identity?.IsAuthenticated == true) context.Response.Headers.CacheControl = "no-store";
        return Task.CompletedTask;
    });
    await next();
});

app.MapGet("/culture/{culture}", (string culture, string? returnUrl, HttpContext context) =>
{
    if (culture is not ("ar-DZ" or "fr-DZ")) return Results.BadRequest();
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax, Path = "/", Secure = isProduction || context.Request.IsHttps });
    var safeReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
        || !returnUrl.StartsWith('/')
        || returnUrl.StartsWith("//", StringComparison.Ordinal)
        || returnUrl.Contains('\\')
        || returnUrl.Any(char.IsControl)
        ? "/"
        : returnUrl;
    return Results.LocalRedirect(safeReturnUrl);
}).AllowAnonymous();

app.MapPost("/Account/CompleteProfile", async (HttpContext context, UserManager<ApplicationUser> users) =>
{
    var user = await users.GetUserAsync(context.User);
    if (user is null) return Results.Redirect("/Account/Login");
    var form = await context.Request.ReadFormAsync();
    var firstName = form["FirstName"].ToString().Trim();
    var lastName = form["LastName"].ToString().Trim();
    var rank = form["Rank"].ToString().Trim();
    if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(rank))
        return Results.Redirect("/");
    user.FirstName = firstName;
    user.LastName = lastName;
    user.Rank = rank;
    user.JobTitle = rank;
    user.DisplayName = $"{firstName} {lastName}";
    user.ProfileCompleted = true;
    var result = await users.UpdateAsync(user);
    return result.Succeeded ? Results.Redirect("/") : Results.Redirect("/");
}).RequireAuthorization("AccountSecurity");

app.MapGet("/health", async (IDbContextFactory<ApplicationDbContext> factory) =>
{
    try
    {
        await using var db = await factory.CreateDbContextAsync();
        IResult result = await db.Database.CanConnectAsync()
            ? Results.Ok(new { status = "ok" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        return result;
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

app.MapGet("/reports/export.xlsx", async (IDbContextFactory<ApplicationDbContext> factory, HttpContext context) =>
{
    var french = CultureInfo.CurrentUICulture.Name.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
    var rtl = !french;
    var periodValue = context.Request.Query["period"].ToString();
    DateOnly? selectedPeriod = DateOnly.TryParseExact(periodValue + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedPeriod)
        ? parsedPeriod
        : null;
    if (!string.IsNullOrWhiteSpace(periodValue) && selectedPeriod is null)
        return Results.BadRequest();
    await using var db = await factory.CreateDbContextAsync();
    var customers = await db.Customers.OrderBy(c => c.Name).ToListAsync();
    var allInvoices = await db.Invoices.Include(i => i.Customer).Include(i => i.Payments).OrderByDescending(i => i.IssuedOn).ToListAsync();
    var invoices = selectedPeriod is null ? allInvoices : allInvoices.Where(i => i.IssuedOn.Year == selectedPeriod.Value.Year && i.IssuedOn.Month == selectedPeriod.Value.Month).ToList();
    var allReadings = await db.MeterReadings.Include(r => r.Customer).OrderByDescending(r => r.Period).ToListAsync();
    var readings = selectedPeriod is null ? allReadings : allReadings.Where(r => r.Period.Year == selectedPeriod.Value.Year && r.Period.Month == selectedPeriod.Value.Month).ToList();
    using var workbook = new XLWorkbook();
    var agencyName = rtl ? "الوكالة الوطنية للتسيير المدمج للموارد المائية · AGIRE TAIRET" : "Agence nationale de gestion intégrée des ressources en eau · AGIRE TAIRET";
    var reportTitle = rtl ? "تقرير التسيير والتحصيل" : "Rapport de gestion et de recouvrement";
    var periodLabel = selectedPeriod?.ToString("yyyy-MM", CultureInfo.InvariantCulture) ?? (rtl ? "كل الفترات" : "Toutes les périodes");
    var generatedAt = DateTime.Now;
    workbook.Properties.Author = agencyName;
    workbook.Properties.Title = $"{reportTitle} — {periodLabel}";
    workbook.Properties.Subject = agencyName;
    workbook.CalculateMode = XLCalculateMode.Auto;
    object Formula(string value) => new ExcelFormula(value);
    var tableNumber = 0;
    var currencyColumn = -1;
    void SetSheet(string name, string[] headers, IEnumerable<object[]> data, IEnumerable<object[]>? totals = null)
    {
        var sheet = workbook.Worksheets.Add(name);
        currencyColumn = Array.FindIndex(headers, header => header is "العملة" or "Devise");
        sheet.RightToLeft = rtl;
        sheet.TabColor = XLColor.FromHtml("#16867e");
        sheet.Style.Font.FontName = rtl ? "Traditional Arabic" : "Arabic Typesetting";
        sheet.Style.Font.FontSize = 10;
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
        sheet.PageSetup.PagesWide = 1;
        sheet.PageSetup.PagesTall = 0;
        sheet.PageSetup.SetRowsToRepeatAtTop(1, 4);
        sheet.Range(1, 1, 1, headers.Length).Merge();
        var titleCell = sheet.Cell(1, 1);
        titleCell.Value = agencyName;
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 15;
        titleCell.Style.Font.FontColor = XLColor.White;
        titleCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#123B56");
        titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        titleCell.Style.Alignment.Horizontal = rtl ? XLAlignmentHorizontalValues.Right : XLAlignmentHorizontalValues.Left;
        sheet.Row(1).Height = 32;
        sheet.Range(2, 1, 2, headers.Length).Merge();
        var subtitleCell = sheet.Cell(2, 1);
        subtitleCell.Value = $"{reportTitle}  |  {(rtl ? "الفترة" : "Période")}: {periodLabel}  |  {(rtl ? "تاريخ الإصدار" : "Émis le")}: {generatedAt:dd/MM/yyyy HH:mm}";
        subtitleCell.Style.Font.FontColor = XLColor.FromHtml("#526873");
        subtitleCell.Style.Font.FontSize = 10;
        subtitleCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF3F3");
        subtitleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        subtitleCell.Style.Alignment.Horizontal = rtl ? XLAlignmentHorizontalValues.Right : XLAlignmentHorizontalValues.Left;
        sheet.Row(2).Height = 23;
        sheet.Row(3).Height = 8;
        for (var c = 0; c < headers.Length; c++)
        {
            var cell = sheet.Cell(4, c + 1); cell.Value = headers[c];
            cell.Style.Font.Bold = true; cell.Style.Font.FontColor = XLColor.FromHtml("#111111");
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#CCFFCC");
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }
        sheet.Row(4).Height = 28;
        var row = 5;
        foreach (var values in data)
        {
            for (var c = 0; c < values.Length; c++)
            {
                var cell = sheet.Cell(row, c + 1);
                var hasFormula = values[c] is ExcelFormula;
                if (values[c] is ExcelFormula formula) cell.FormulaA1 = formula.Value;
                else cell.Value = XLCellValue.FromObject(values[c]);
                cell.Style.Fill.BackgroundColor = row % 2 == 0 ? XLColor.FromHtml("#F3F5F5") : XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                if (values[c] is decimal || hasFormula)
                {
                    var header = headers[c];
                    var quantity = header.Contains("الاستهلاك", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("قراءة", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("m³", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("m3", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("index", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("consommation", StringComparison.OrdinalIgnoreCase);
                    var tariff = header.Contains("سعر الوحدة", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("tarif", StringComparison.OrdinalIgnoreCase);
                    var count = header.Contains("عدد", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("فواتير", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("factures", StringComparison.OrdinalIgnoreCase);
                    cell.Style.NumberFormat.Format = count ? "#,##0" : tariff
                        ? "#,##0.0000;[Red](#,##0.0000);-"
                        : quantity
                        ? "#,##0.000;[Red](#,##0.000);-"
                        : MoneyFormat(CurrencyFor(values));
                }
                else if (values[c] is int or long) cell.Style.NumberFormat.Format = "#,##0";
                else if (values[c] is DateTime) cell.Style.NumberFormat.Format = "dd/mm/yyyy";
            }
            row++;
        }
        var dataEndRow = Math.Max(4, row - 1);
        var dataRange = sheet.Range(4, 1, dataEndRow, headers.Length);
        dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#111111");
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#111111");
        var detailTable = dataRange.CreateTable($"DataTable{++tableNumber}");
        detailTable.Theme = XLTableTheme.TableStyleMedium7;
        detailTable.ShowRowStripes = true;
        detailTable.ShowColumnStripes = false;

        var footerRows = totals?.ToList() ?? [];
        var footerEndRow = dataEndRow;
        foreach (var values in footerRows)
        {
            for (var c = 0; c < Math.Min(values.Length, headers.Length); c++)
            {
                var cell = sheet.Cell(row, c + 1);
                var hasFormula = values[c] is ExcelFormula;
                if (values[c] is ExcelFormula formula) cell.FormulaA1 = formula.Value;
                else cell.Value = XLCellValue.FromObject(values[c]);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                if (values[c] is decimal || hasFormula)
                {
                    var header = headers[c];
                    var quantity = header.Contains("الاستهلاك", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("قراءة", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("m³", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("m3", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("index", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("consommation", StringComparison.OrdinalIgnoreCase);
                    var tariff = header.Contains("سعر الوحدة", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("tarif", StringComparison.OrdinalIgnoreCase);
                    var count = header.Contains("عدد", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("فواتير", StringComparison.OrdinalIgnoreCase)
                        || header.Contains("factures", StringComparison.OrdinalIgnoreCase);
                    cell.Style.NumberFormat.Format = count ? "#,##0" : tariff ? "#,##0.0000" : quantity ? "#,##0.000" : MoneyFormat(CurrencyFor(values));
                }
                else if (values[c] is int or long) cell.Style.NumberFormat.Format = "#,##0";
            }
            var footerRange = sheet.Range(row, 1, row, headers.Length);
            footerRange.Style.Font.Bold = true;
            footerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E7ECEC");
            footerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            footerRange.Style.Alignment.WrapText = true;
            footerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#111111");
            footerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#111111");
            sheet.Row(row).Height = 25;
            footerEndRow = row++;
        }
        sheet.SheetView.FreezeRows(4);
        sheet.Columns().AdjustToContents(8, 38);
        sheet.PageSetup.Margins.Top = 0.35;
        sheet.PageSetup.Margins.Bottom = 0.35;
        sheet.PageSetup.Margins.Left = 0.25;
        sheet.PageSetup.Margins.Right = 0.25;
        sheet.PageSetup.CenterHorizontally = true;
        sheet.PageSetup.PrintAreas.Add(1, 1, Math.Max(footerEndRow, dataEndRow), headers.Length);
    }
    string MoneyFormat(string currency) => $"#,##0.00 \"{currency}\";[Red](#,##0.00) \"{currency}\";-";
    string CurrencyFor(object[] values)
    {
        if (currencyColumn >= 0 && !string.IsNullOrWhiteSpace(values[currencyColumn]?.ToString()))
            return values[currencyColumn]!.ToString()!;
        var label = values.FirstOrDefault()?.ToString() ?? "";
        var opening = label.LastIndexOf('(');
        var closing = label.LastIndexOf(')');
        return opening >= 0 && closing > opening ? label[(opening + 1)..closing] : "DZD";
    }
    var totalUsage = readings.Sum(r => r.Consumption);
    var summary = new List<object[]>
    {
        new object[] { rtl ? "فترة التقرير" : "Période du rapport", selectedPeriod?.ToString("yyyy-MM") ?? (rtl ? "كل الفترات" : "Toutes les périodes") },
        new object[] { rtl ? "عدد الزبائن" : "Nombre de clients", customers.Count },
        new object[] { rtl ? "عدد الفواتير" : "Nombre de factures", invoices.Count },
        new object[] { rtl ? "الاستهلاك (م³)" : "Consommation (m³)", totalUsage }
    };
    foreach (var currency in allInvoices.Select(i => i.CurrencyCode).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c))
    {
        var periodInvoices = invoices.Where(i => string.Equals(i.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)).ToList();
        var issued = periodInvoices.Sum(i => i.Amount);
        var paid = allInvoices.Where(i => string.Equals(i.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase))
            .SelectMany(i => i.Payments)
            .Where(p => selectedPeriod is null || (p.PaidOn.Year == selectedPeriod.Value.Year && p.PaidOn.Month == selectedPeriod.Value.Month))
            .Sum(p => p.Amount);
        var remaining = periodInvoices.Sum(i => Math.Max(0, i.Amount - i.Payments.Sum(p => p.Amount)));
        summary.Add(new object[] { $"{(rtl ? "إجمالي الفواتير" : "Total facturé")} ({currency})", issued });
        summary.Add(new object[] { $"{(rtl ? "المبالغ المحصلة خلال الفترة" : "Encaissé pendant la période")} ({currency})", paid });
        summary.Add(new object[] { $"{(rtl ? "المتبقي على فواتير الفترة" : "Solde des factures de la période")} ({currency})", remaining });
    }
    var customerData = customers.SelectMany(c =>
    {
        var groups = invoices.Where(i => i.CustomerId == c.Id).GroupBy(i => i.CurrencyCode).ToList();
        if (groups.Count == 0) return new[] { new object[] { c.Code, c.Name, c.MeterNumber ?? "", c.SubscriptionYear?.ToString() ?? "", c.Phone ?? "", c.Location ?? "", c.IsActive ? (rtl ? "نشط" : "Actif") : (rtl ? "موقوف" : "Suspendu"), "DZD", 0, 0m, 0m, 0m, c.Notes ?? "" } };
        return groups.Select(g =>
        {
            var issued = g.Sum(i => i.Amount); var paid = g.Sum(i => i.Payments.Sum(p => p.Amount));
            return new object[] { c.Code, c.Name, c.MeterNumber ?? "", c.SubscriptionYear?.ToString() ?? "", c.Phone ?? "", c.Location ?? "", c.IsActive ? (rtl ? "نشط" : "Actif") : (rtl ? "موقوف" : "Suspendu"), g.Key, g.Count(), issued, paid, Math.Max(0, issued - paid), c.Notes ?? "" };
        });
    }).ToList();
    for (var index = 0; index < customerData.Count; index++)
    {
        var row = index + 5;
        customerData[index][11] = Formula($"=MAX(0,J{row}-K{row})");
    }
    var customerDataEnd = Math.Max(5, customerData.Count + 4);
    var customerCurrencies = customerData.Select(values => values[7]?.ToString() ?? "DZD").Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(currency => currency).ToList();
    var customerTotals = customerCurrencies.Select((currency, index) =>
    {
        var totalRow = customerData.Count + 5 + index;
        return new object[]
        {
            $"{(rtl ? "الإجمالي" : "Total")} ({currency})", "", "", "", "", "", "", currency,
            Formula("=SUMIF($H$5:$H$" + customerDataEnd + ",$H$" + totalRow + ",$I$5:$I$" + customerDataEnd + ")"),
            Formula("=SUMIF($H$5:$H$" + customerDataEnd + ",$H$" + totalRow + ",$J$5:$J$" + customerDataEnd + ")"),
            Formula("=SUMIF($H$5:$H$" + customerDataEnd + ",$H$" + totalRow + ",$K$5:$K$" + customerDataEnd + ")"),
            Formula("=SUMIF($H$5:$H$" + customerDataEnd + ",$H$" + totalRow + ",$L$5:$L$" + customerDataEnd + ")"), ""
        };
    }).ToList();
    SetSheet(rtl ? "الزبائن" : "Clients", rtl ? ["الرمز", "الاسم", "رقم العداد", "سنة الاشتراك", "الهاتف", "الموقع", "الحالة", "العملة", "الفواتير", "الإجمالي", "المدفوع", "المتبقي", "ملاحظات"] : ["Code", "Nom", "N° compteur", "Année d’abonnement", "Téléphone", "Secteur", "État", "Devise", "Factures", "Total", "Payé", "Solde", "Notes"], customerData, customerTotals);
    var invoiceData = invoices.Select((i, index) =>
    {
        var paid = i.Payments.Sum(p => p.Amount); var row = index + 5;
        var status = Formula($"=IF(H{row}<=0,\"{(rtl ? "مستحق" : "À régler")}\",IF(I{row}>=H{row},\"{(rtl ? "مسدد" : "Soldée")}\",IF(I{row}>0,\"{(rtl ? "جزئي" : "Partiel")}\",\"{(rtl ? "مستحق" : "À régler")}\")))");
        return new object[] { i.Number, i.Customer.Name, i.Description, i.IssuedOn.ToDateTime(TimeOnly.MinValue), i.IssuedOn.ToString("yyyy-MM"), i.DueOn.ToDateTime(TimeOnly.MinValue), i.CurrencyCode, i.Amount, paid, Formula($"=MAX(0,H{row}-I{row})"), status };
    }).ToList();
    var invoiceDataEnd = Math.Max(5, invoiceData.Count + 4);
    var invoiceTotals = invoices.Select(i => i.CurrencyCode).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(currency => currency).Select((currency, index) =>
    {
        var totalRow = invoiceData.Count + 5 + index;
        return new object[]
        {
            $"{(rtl ? "الإجمالي" : "Total")} ({currency})", "", "", "", "", "", currency,
            Formula("=SUMIF($G$5:$G$" + invoiceDataEnd + ",$G$" + totalRow + ",$H$5:$H$" + invoiceDataEnd + ")"),
            Formula("=SUMIF($G$5:$G$" + invoiceDataEnd + ",$G$" + totalRow + ",$I$5:$I$" + invoiceDataEnd + ")"),
            Formula("=SUMIF($G$5:$G$" + invoiceDataEnd + ",$G$" + totalRow + ",$J$5:$J$" + invoiceDataEnd + ")"), ""
        };
    }).ToList();
    SetSheet(rtl ? "الفواتير" : "Factures", rtl ? ["رقم الفاتورة", "الزبون", "البيان", "الإصدار", "الشهر", "الاستحقاق", "العملة", "الإجمالي", "المدفوع", "المتبقي", "الحالة"] : ["N° facture", "Client", "Description", "Émise le", "Mois", "Échéance", "Devise", "Total", "Payé", "Solde", "Statut"], invoiceData, invoiceTotals);
    var monthlyArchive = allInvoices.GroupBy(i => new { Month = i.IssuedOn.ToString("yyyy-MM"), i.CurrencyCode }).OrderBy(g => g.Key.Month).ThenBy(g => g.Key.CurrencyCode).Select(g => new object[] { g.Key.Month, g.Key.CurrencyCode, g.Count(), g.Sum(i => i.Amount), g.Sum(i => i.Payments.Sum(p => p.Amount)), Math.Max(0, g.Sum(i => i.Amount) - g.Sum(i => i.Payments.Sum(p => p.Amount))) }).ToList();
    var archiveDataEnd = Math.Max(5, monthlyArchive.Count + 4);
    var archiveTotals = allInvoices.Select(i => i.CurrencyCode).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(currency => currency).Select((currency, index) =>
    {
        var totalRow = monthlyArchive.Count + 5 + index;
        return new object[]
        {
            rtl ? "إجمالي كل الفترات" : "Total toutes périodes", currency,
            Formula("=SUMIF($B$5:$B$" + archiveDataEnd + ",$B$" + totalRow + ",$C$5:$C$" + archiveDataEnd + ")"),
            Formula("=SUMIF($B$5:$B$" + archiveDataEnd + ",$B$" + totalRow + ",$D$5:$D$" + archiveDataEnd + ")"),
            Formula("=SUMIF($B$5:$B$" + archiveDataEnd + ",$B$" + totalRow + ",$E$5:$E$" + archiveDataEnd + ")"),
            Formula("=SUMIF($B$5:$B$" + archiveDataEnd + ",$B$" + totalRow + ",$F$5:$F$" + archiveDataEnd + ")")
        };
    }).ToList();
    SetSheet(rtl ? "أرشيف شهري" : "Archives mensuelles", rtl ? ["الشهر", "العملة", "عدد الفواتير", "الإجمالي", "المحصل", "المتبقي"] : ["Mois", "Devise", "Factures", "Total", "Encaissé", "Solde dû"], monthlyArchive, archiveTotals);
    var selectedPayments = allInvoices.SelectMany(i => i.Payments.Select(p => new { Invoice = i, Payment = p }))
        .Where(x => selectedPeriod is null || (x.Payment.PaidOn.Year == selectedPeriod.Value.Year && x.Payment.PaidOn.Month == selectedPeriod.Value.Month));
    var paymentData = selectedPayments.Select(x => new object[]
    {
        rtl ? "مسجلة" : "Enregistré", x.Payment.Amount, x.Invoice.Customer.Name,
        x.Invoice.IssuedOn.ToString("yyyy-MM", CultureInfo.InvariantCulture), x.Invoice.Description,
        x.Invoice.Number, x.Invoice.Customer.MeterNumber ?? "", x.Payment.PaidOn.ToDateTime(TimeOnly.MinValue),
        x.Invoice.Customer.Location ?? "", x.Invoice.CurrencyCode, x.Payment.Reference ?? "", x.Payment.RecordedBy
    }).ToList();
    var paymentDataEnd = Math.Max(5, paymentData.Count + 4);
    var paymentTotals = paymentData.Select(values => values[9]?.ToString() ?? "DZD").Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(currency => currency).Select((currency, index) =>
    {
        var totalRow = paymentData.Count + 5 + index;
        return new object[]
        {
            $"{(rtl ? "إجمالي الدفعات" : "Total paiements")} ({currency})",
            Formula("=SUMIF($J$5:$J$" + paymentDataEnd + ",$J$" + totalRow + ",$B$5:$B$" + paymentDataEnd + ")"),
            "", "", "", "", "", "", "", currency, "", ""
        };
    }).ToList();
    SetSheet(rtl ? "الدفعات" : "Paiements", rtl
        ? ["حالة السداد", "المبلغ المدفوع", "اسم الزبون", "فترة الفاتورة", "بيان الفاتورة", "رقم الفاتورة", "رقم العداد", "تاريخ الدفع", "المنطقة", "العملة", "مرجع الدفع", "سُجل بواسطة"]
        : ["État du paiement", "Montant payé", "Nom du client", "Période de facture", "Objet de la facture", "N° facture", "N° compteur", "Date du paiement", "Secteur", "Devise", "Référence", "Enregistré par"], paymentData, paymentTotals);
    var usageData = readings.Select((r, index) =>
    {
        var prefix = $"USE-{r.Period:yyyyMM}-";
        var usageInvoice = invoices.FirstOrDefault(i => i.CustomerId == r.CustomerId && i.Number.StartsWith(prefix, StringComparison.Ordinal));
        var paid = usageInvoice?.Payments.Sum(p => p.Amount) ?? 0m;
        var status = usageInvoice is null ? (r.Charge == 0 ? (rtl ? "لا توجد فاتورة" : "Aucune facture") : (rtl ? "فاتورة غير مرتبطة" : "Facture non associée")) : usageInvoice.Amount <= paid ? (rtl ? "مدفوع" : "Payée") : paid > 0 ? (rtl ? "مدفوع جزئياً" : "Partiellement payée") : (rtl ? "غير مدفوع" : "Impayée");
        var row = index + 5;
        return new object[] { r.Customer.Name, r.Period.ToString("yyyy-MM"), r.PreviousReadOn.ToDateTime(TimeOnly.MinValue), r.PreviousReading, r.CurrentReadOn.ToDateTime(TimeOnly.MinValue), r.CurrentReading, Formula($"=MAX(0,F{row}-D{row})"), r.Rate, Formula($"=ROUND(G{row}*H{row},2)"), status, usageInvoice?.Number ?? "" };
    }).ToList();
    var usageDataEnd = Math.Max(5, usageData.Count + 4);
    var usageTotals = new object[] { rtl ? "الإجمالي" : "Total", "", "", "", "", "", usageData.Count == 0 ? Formula("=0") : Formula("=SUM(G5:G" + usageDataEnd + ")"), "", usageData.Count == 0 ? Formula("=0") : Formula("=SUM(I5:I" + usageDataEnd + ")"), "", "" };
    SetSheet(rtl ? "الاستهلاك" : "Consommation", rtl ? ["الاسم واللقب", "الشهر", "تاريخ القراءة السابقة", "القراءة السابقة م³", "تاريخ القراءة الجديدة", "القراءة الجديدة م³", "الاستهلاك م³", "سعر الوحدة دج", "المبلغ دج", "الحالة", "رقم الفاتورة"] : ["Nom et prénom", "Mois", "Date ancien relevé", "Ancien index m³", "Date nouveau relevé", "Nouvel index m³", "Consommation m³", "Tarif DZD", "Montant DZD", "Statut", "N° facture"], usageData, [usageTotals]);
    SetSheet(rtl ? "الملخص" : "Synthèse", [rtl ? "المؤشر" : "Indicateur", rtl ? "القيمة" : "Valeur"], summary);
    using var stream = new MemoryStream(); workbook.SaveAs(stream);
    context.Response.Headers.CacheControl = "no-store";
    var periodSuffix = selectedPeriod?.ToString("yyyy-MM", CultureInfo.InvariantCulture) ?? DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    return Results.File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"AGIRE-TAIRET-report-{periodSuffix}.xlsx");
}).RequireAuthorization("AdminMfa").RequireAuthorization(policy => policy.RequireRole("Administrator", "Billing", "ReadOnly"));

app.MapGet("/customers/export.xlsx", async (IDbContextFactory<ApplicationDbContext> factory, HttpContext context) =>
{
    var french = CultureInfo.CurrentUICulture.Name.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
    var rtl = !french;
    var agencyName = rtl ? "الوكالة الوطنية للتسيير المدمج للموارد المائية · AGIRE TAIRET" : "Agence nationale de gestion intégrée des ressources en eau · AGIRE TAIRET";
    var reportTitle = rtl ? "تقرير سجل الزبائن والعدادات والديون" : "Rapport des clients, compteurs et dettes";
    var generatedAt = DateTime.Now;
    await using var db = await factory.CreateDbContextAsync();
    var customers = await db.Customers.AsNoTracking().AsSplitQuery().Include(c => c.Invoices).ThenInclude(i => i.Payments).OrderBy(c => c.Name).ToListAsync();
    var readings = await db.MeterReadings.AsNoTracking().OrderByDescending(r => r.Period).ThenByDescending(r => r.CurrentReadOn).ToListAsync();
    var centralTariff = await db.WaterTariffSettings.AsNoTracking().Where(setting => setting.Id == 1).Select(setting => setting.RatePerCubicMetre).SingleOrDefaultAsync() ?? 0m;
    var latestReadings = readings.GroupBy(r => r.CustomerId).ToDictionary(g => g.Key, g => g.First());
    var rows = customers.Select(customer =>
    {
        latestReadings.TryGetValue(customer.Id, out var reading);
        var debts = customer.Invoices.GroupBy(i => i.CurrencyCode, StringComparer.OrdinalIgnoreCase)
            .Select(group => (Currency: group.Key, Amount: group.Sum(i => Math.Max(0, i.Amount - i.Payments.Sum(p => p.Amount)))))
            .Where(item => item.Amount > 0).OrderBy(item => item.Currency == "DZD" ? 0 : 1).ThenBy(item => item.Currency).ToList();
        var dzdInvoices = customer.Invoices.Where(invoice => string.Equals(invoice.CurrencyCode, "DZD", StringComparison.OrdinalIgnoreCase)).ToList();
        var dzdBilled = dzdInvoices.Sum(invoice => invoice.Amount);
        var dzdPaid = dzdInvoices.Sum(invoice => invoice.Payments.Sum(payment => payment.Amount));
        var otherDebtText = string.Join(" · ", debts.Where(item => item.Currency != "DZD").Select(item => $"{item.Amount:N2} {item.Currency}"));
        var debtStatus = debts.Count == 0 ? (rtl ? "لا توجد ديون" : "Aucune dette") : (rtl ? "مدين" : "Débiteur");
        return new { Customer = customer, Reading = reading, DzdBilled = dzdBilled, DzdPaid = dzdPaid, OtherDebtText = otherDebtText, DebtStatus = debtStatus };
    }).ToList();

    using var workbook = new XLWorkbook();
    workbook.Properties.Author = agencyName;
    workbook.Properties.Title = reportTitle;
    workbook.Properties.Subject = agencyName;
    workbook.CalculateMode = XLCalculateMode.Auto;
    var sheet = workbook.Worksheets.Add(rtl ? "سجل الزبائن" : "Registre clients");
    sheet.RightToLeft = rtl;
    sheet.TabColor = XLColor.FromHtml("#16867e");
    sheet.Style.Font.FontName = rtl ? "Traditional Arabic" : "Arabic Typesetting";
    sheet.Style.Font.FontSize = 10;
    sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
    sheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
    sheet.PageSetup.PagesWide = 1;
    sheet.PageSetup.PagesTall = 0;
    sheet.PageSetup.SetRowsToRepeatAtTop(1, 4);
    var headers = rtl
        ? new[] { "الرقم", "اسم الزبون", "رمز الزبون", "رقم العداد", "الهاتف", "الموقع", "سنة الاشتراك", "فترة القراءة", "القراءة السابقة م³", "القراءة الحالية م³", "الاستهلاك م³", "سعر الوحدة دج", "قيمة الاستهلاك دج", "إجمالي الفواتير دج", "المبلغ المحصل دج", "المتبقي دج", "ديون بعملات أخرى", "حالة الدين" }
        : new[] { "N°", "Nom du client", "Code client", "N° compteur", "Téléphone", "Secteur", "Année d’abonnement", "Période", "Ancien index m³", "Nouvel index m³", "Consommation m³", "Tarif DZD/m³", "Consommation DZD", "Facturé DZD", "Encaissé DZD", "Solde DZD", "Autres devises", "État de la dette" };
    sheet.Range(1, 1, 1, headers.Length).Merge();
    sheet.Cell(1, 1).Value = agencyName;
    sheet.Cell(1, 1).Style.Font.Bold = true;
    sheet.Cell(1, 1).Style.Font.FontSize = 15;
    sheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#172126");
    sheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.White;
    sheet.Cell(1, 1).Style.Alignment.Horizontal = rtl ? XLAlignmentHorizontalValues.Right : XLAlignmentHorizontalValues.Left;
    sheet.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    sheet.Row(1).Height = 32;
    sheet.Range(2, 1, 2, headers.Length).Merge();
    sheet.Cell(2, 1).Value = $"{reportTitle}  |  {(rtl ? "تاريخ إعداد التقرير" : "Généré le")}: {generatedAt:dd/MM/yyyy HH:mm}";
    sheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#526873");
    sheet.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.White;
    sheet.Cell(2, 1).Style.Alignment.Horizontal = rtl ? XLAlignmentHorizontalValues.Right : XLAlignmentHorizontalValues.Left;
    sheet.Row(2).Height = 24;
    sheet.Row(3).Height = 7;
    for (var column = 0; column < headers.Length; column++)
    {
        var cell = sheet.Cell(4, column + 1);
        cell.Value = headers[column];
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontColor = XLColor.FromHtml("#111111");
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#CCFFCC");
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cell.Style.Alignment.WrapText = true;
    }
    sheet.Row(4).Height = 34;
    var outputRow = 5;
    var sequence = 0;
    foreach (var row in rows)
    {
        sequence++;
        var customer = row.Customer;
        var reading = row.Reading;
        var dataExcelRow = outputRow;
        object[] values = [
            sequence, customer.Name, customer.Code, customer.MeterNumber ?? "", customer.Phone ?? "", customer.Location ?? "",
            customer.SubscriptionYear is null ? "" : (object)customer.SubscriptionYear.Value,
            reading?.Period.ToString("yyyy-MM") ?? (rtl ? "لا توجد قراءة مسجلة" : "Aucun relevé enregistré"),
            reading is null ? "" : (object)reading.PreviousReading, reading is null ? "" : (object)reading.CurrentReading,
            new ExcelFormula($"=MAX(0,J{dataExcelRow}-I{dataExcelRow})"), reading?.Rate ?? centralTariff,
            new ExcelFormula($"=ROUND(K{dataExcelRow}*L{dataExcelRow},2)"), row.DzdBilled, row.DzdPaid,
            new ExcelFormula($"=MAX(0,N{dataExcelRow}-O{dataExcelRow})"), row.OtherDebtText, row.DebtStatus
        ];
        for (var column = 0; column < values.Length; column++)
        {
            var cell = sheet.Cell(outputRow, column + 1);
            if (values[column] is ExcelFormula formula) cell.FormulaA1 = formula.Value;
            else cell.Value = XLCellValue.FromObject(values[column]);
            cell.Style.Fill.BackgroundColor = outputRow % 2 == 0 ? XLColor.FromHtml("#F3F5F5") : XLColor.White;
            cell.Style.Alignment.Horizontal = column == 1 ? XLAlignmentHorizontalValues.Right : XLAlignmentHorizontalValues.Center;
            if (values[column] is decimal or ExcelFormula)
            {
                var isMeasurement = column is 8 or 9 or 10;
                var isRate = column == 11;
                cell.Style.NumberFormat.Format = isMeasurement ? "#,##0.000;[Red](#,##0.000);-" : isRate ? "#,##0.0000" : rtl ? "#,##0.00 \"دج\";[Red](#,##0.00) \"دج\";-" : "#,##0.00 \"DZD\";[Red](#,##0.00) \"DZD\";-";
            }
            else if (values[column] is int) cell.Style.NumberFormat.Format = "0";
            if (column == 17 && row.DebtStatus == (rtl ? "مدين" : "Débiteur"))
            {
                cell.Style.Font.FontColor = XLColor.FromHtml("#9c4b36");
                cell.Style.Font.Bold = true;
            }
        }
        outputRow++;
    }
    var dataRange = sheet.Range(4, 1, Math.Max(4, outputRow - 1), headers.Length);
    dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    dataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#111111");
    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    dataRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#111111");
    var customerTable = dataRange.CreateTable("CustomerRegister");
    customerTable.Theme = XLTableTheme.TableStyleMedium7;
    customerTable.ShowRowStripes = true;
    customerTable.ShowColumnStripes = false;

    var totalRow = outputRow;
    sheet.Cell(totalRow, 1).Value = rtl ? "الإجمالي" : "Total";
    sheet.Range(totalRow, 2, totalRow, 8).Merge();
    sheet.Cell(totalRow, 2).Value = $"{(rtl ? "عدد الزبائن" : "Nombre de clients")}: {customers.Count:N0}";
    sheet.Cell(totalRow, 9).Value = "";
    sheet.Cell(totalRow, 10).Value = "";
    var lastDataRow = Math.Max(5, totalRow - 1);
    sheet.Cell(totalRow, 11).FormulaA1 = outputRow == 5 ? "0" : "SUM(K5:K" + lastDataRow + ")";
    sheet.Cell(totalRow, 11).Style.NumberFormat.Format = "#,##0.000;[Red](#,##0.000);-";
    sheet.Cell(totalRow, 12).Value = "";
    sheet.Cell(totalRow, 13).FormulaA1 = outputRow == 5 ? "0" : "SUM(M5:M" + lastDataRow + ")";
    sheet.Cell(totalRow, 14).FormulaA1 = outputRow == 5 ? "0" : "SUM(N5:N" + lastDataRow + ")";
    sheet.Cell(totalRow, 15).FormulaA1 = outputRow == 5 ? "0" : "SUM(O5:O" + lastDataRow + ")";
    sheet.Cell(totalRow, 16).FormulaA1 = outputRow == 5 ? "0" : "SUM(P5:P" + lastDataRow + ")";
    foreach (var column in new[] { 13, 14, 15, 16 })
        sheet.Cell(totalRow, column).Style.NumberFormat.Format = rtl ? "#,##0.00 \"دج\";[Red](#,##0.00) \"دج\";-" : "#,##0.00 \"DZD\";[Red](#,##0.00) \"DZD\";-";
    var otherDebtTotals = customers.SelectMany(customer => customer.Invoices)
        .GroupBy(invoice => invoice.CurrencyCode, StringComparer.OrdinalIgnoreCase)
        .Where(group => !string.Equals(group.Key, "DZD", StringComparison.OrdinalIgnoreCase))
        .Select(group => (Currency: group.Key, Amount: group.Sum(invoice => Math.Max(0, invoice.Amount - invoice.Payments.Sum(payment => payment.Amount)))))
        .Where(item => item.Amount > 0)
        .OrderBy(item => item.Currency)
        .Select(item => $"{item.Amount:N2} {item.Currency}");
    sheet.Cell(totalRow, 17).Value = string.Join(" · ", otherDebtTotals);
    sheet.Cell(totalRow, 18).Value = $"{(rtl ? "المدينون" : "Débiteurs")}: {rows.Count(item => item.DebtStatus == (rtl ? "مدين" : "Débiteur")):N0}";
    var totalRange = sheet.Range(totalRow, 1, totalRow, headers.Length);
    totalRange.Style.Font.Bold = true;
    totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E7ECEC");
    totalRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    totalRange.Style.Alignment.WrapText = true;
    totalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    totalRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#111111");
    totalRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    totalRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#111111");
    sheet.Row(totalRow).Height = 28;
    sheet.SheetView.FreezeRows(4);
    sheet.Columns().AdjustToContents(8, 28);
    sheet.PageSetup.Margins.Top = 0.35;
    sheet.PageSetup.Margins.Bottom = 0.35;
    sheet.PageSetup.Margins.Left = 0.25;
    sheet.PageSetup.Margins.Right = 0.25;
    sheet.PageSetup.CenterHorizontally = true;
    sheet.PageSetup.PrintAreas.Add(1, 1, totalRow, headers.Length);
    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    context.Response.Headers.CacheControl = "no-store";
    return Results.File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"AGIRE-TAIRET-customers-{DateTime.UtcNow:yyyyMMdd}.xlsx");
}).RequireAuthorization("AdminMfa").RequireAuthorization(policy => policy.RequireRole("Administrator", "Billing", "ReadOnly"));

app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

public partial class Program { }
