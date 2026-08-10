using DLPManagementSystem.Authentication;
using DLPManagementSystem.Data.Seed;
using DLPManagementSystem.Helper.Email;
using DLPManagementSystem.Helper.Health;
using DLPManagementSystem.Helper.Retention;
using DLPManagementSystem.Helper.DeviceMonitoring;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.BackgroundServices;
using DLPManagementSystem.Service.Interface;
using DLPManagementSystem.Service.Service;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using DLPManagementSystem.CompanyDlpDashboard;
using DLPManagementSystem.Common;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter a valid JWT access token."
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
builder.Services.AddScoped<IAgentAuditEventService, AgentAuditEventService>();
builder.Services.AddScoped<IAgentEnrollmentService, AgentEnrollmentService>();
builder.Services.AddScoped<IAgentHeartbeatService, AgentHeartbeatService>();
builder.Services.AddScoped<IAgentPolicyService, AgentPolicyService>();
builder.Services.Configure<PolicySigningOptions>(builder.Configuration.GetSection("PolicySigning"));
builder.Services.AddSingleton<IPolicySigningService, EcdsaPolicySigningService>();

builder.Services.AddMemoryCache();

builder.Services.AddScoped<IPermissionLookupService, PermissionLookupService>();
builder.Services.AddScoped<IPolicyVersionService, PolicyVersionService>();
builder.Services.AddScoped<IAdminAuditLogService, AdminAuditLogService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILookupsService, LookupsService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IAuditEventService, AuditEventService>();
builder.Services.AddScoped<IPermissionRequestService, PermissionRequestService>();
builder.Services.AddScoped<IDictionaryRuleService, DictionaryRuleService>();
builder.Services.AddScoped<IPermissionGrantService, PermissionGrantService>();
builder.Services.Configure<FileClassificationApiOptions>(builder.Configuration.GetSection("FileClassificationApi"));
builder.Services.AddHttpClient("FileClassificationApi", (services, client) =>
{
    var options = services.GetRequiredService<IOptions<FileClassificationApiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl);
    }
});
builder.Services.AddScoped<IFileClassificationService, FileClassificationService>();
builder.Services.AddScoped<IFileKeyProtectionService, FileKeyProtectionService>();
builder.Services.AddScoped<IEnrollmentTokenService, EnrollmentTokenService>();

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IAlertEmailService, AlertEmailService>();
builder.Services.AddHostedService<AlertEmailNotificationWorker>();

builder.Services.AddScoped<IDemoRequestEmailService, DemoRequestEmailService>();
builder.Services.AddScoped<IDemoRequestService, DemoRequestService>();

builder.Services.Configure<AuditRetentionOptions>(builder.Configuration.GetSection("AuditRetention"));
builder.Services.AddHostedService<AuditRetentionWorker>();
builder.Services.AddHostedService<PermissionGrantTransitionWorker>();

builder.Services.Configure<DeviceStaleDetectionOptions>(builder.Configuration.GetSection("DeviceStaleDetection"));
builder.Services.AddHostedService<DeviceStaleDetectionWorker>();

// Explicit persistence + a stable application name: without these, ASP.NET Core's default Data
// Protection keyring can differ across redeploys or across instances, silently breaking every
// existing IDataProtector.Unwrap call (FileKeyProtectionService uses this to unwrap file encryption
// keys) as soon as the keyring changes - previously wrapped keys become permanently unwrappable.
// KeyStoragePath defaults to a location outside this app's own deployment directory specifically so
// a redeploy (which typically replaces the deployment directory wholesale) doesn't lose the keyring;
// see SECRETS.md if that directory needs to be created/made writable on the actual server ahead of time.
var dataProtectionKeyStoragePath = builder.Configuration["DataProtection:KeyStoragePath"];
var dataProtectionKeyDirectory = string.IsNullOrWhiteSpace(dataProtectionKeyStoragePath)
    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DLPManagementSystem", "DataProtectionKeys")
    : dataProtectionKeyStoragePath;

builder.Services.AddDataProtection()
    .SetApplicationName("DLPManagementSystem")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyDirectory));

builder.Services.AddDbContext<DLPSystemContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecretKey = jwtSection["SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey configuration is missing.");

// Fail closed: refuse to start in Production with the shipped placeholder (or anything too short
// to be a real key). Without this, "forgot to set the env var" silently ships a backend that signs
// SuperAdmin bearer tokens with a secret that's sitting in plain text in the public source tree -
// anyone who reads the repo can forge a valid token for any role. Documentation alone (SECRETS.md)
// isn't enough here, unlike ConnectionStrings/ApiKey, because there's no safe default to fall back
// to - a JWT signing key either is the real secret or it's a vulnerability.
if (builder.Environment.IsProduction() &&
    (jwtSecretKey == "CHANGE_THIS_TO_A_LONG_SECURE_SECRET_KEY_32_CHARS_MINIMUM" || jwtSecretKey.Length < 32))
{
    throw new InvalidOperationException(
        "Jwt:SecretKey is still the placeholder (or shorter than 32 characters) while running in " +
        "the Production environment. Set a real, unique secret via the Jwt__SecretKey environment " +
        "variable before starting this process - see SECRETS.md.");
}

// Fail closed, same reasoning as the Jwt:SecretKey check above: without this, "forgot to set the
// env var" silently ships a backend that sends every device an unsigned ("DEVELOPMENT-UNSIGNED")
// policy. A Production-installed device (AllowUnsignedDevelopmentPolicy=false locally) would then
// reject every policy update forever - a functional dead end, not just a security gap - while a
// Development-configured device would silently accept it, which is arguably worse. There is no safe
// default to fall back to here, so refuse to start rather than let either happen.
var policySigningPrivateKeyPem = builder.Configuration.GetSection("PolicySigning")["PrivateKeyPem"] ?? "";
if (builder.Environment.IsProduction())
{
    var isValidEcdsaPrivateKey = false;
    if (!string.IsNullOrWhiteSpace(policySigningPrivateKeyPem))
    {
        try
        {
            using var ecdsaStartupCheck = System.Security.Cryptography.ECDsa.Create();
            ecdsaStartupCheck.ImportFromPem(policySigningPrivateKeyPem);
            isValidEcdsaPrivateKey = true;
        }
        catch
        {
            isValidEcdsaPrivateKey = false;
        }
    }

    if (!isValidEcdsaPrivateKey)
    {
        throw new InvalidOperationException(
            "PolicySigning:PrivateKeyPem is missing or is not a valid ECDSA private key while running " +
            "in the Production environment. Generate a P-256 keypair (see " +
            "win-form/Al-Ameen-windows/scripts/generate-policy-signing-keys.ps1) and set the private key " +
            "via the PolicySigning__PrivateKeyPem environment variable before starting this process - " +
            "see SECRETS.md.");
    }
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = "role"
        };
    })
    .AddScheme<AuthenticationSchemeOptions, DeviceBearerAuthenticationHandler>(
        DeviceBearerDefaults.SchemeName, null);

builder.Services.AddAuthorization();

builder.Services.Configure<DlpDashboardOptions>(
    builder.Configuration.GetSection("DlpDashboard"));

builder.Services.AddScoped<IDlpDashboardQueryService, SqlDlpDashboardQueryService>();

builder.Services.AddCors(options =>
{
    // This was AllowAnyOrigin() - almost certainly a quick fix for a real CORS error hit while
    // connecting the deployed frontend (161.97.90.171:1200) to this backend, since the previous
    // policy only ever allowed http://localhost:4200 and was never updated for the real deployed
    // origin. AllowAnyOrigin() on an admin API that issues Bearer tokens is too broad - any site
    // can call these endpoints cross-origin. Config-driven instead: appsettings.Development.json
    // keeps the dev-server origin, appsettings.Production.json carries the real deployed frontend
    // origin - update it there (or via the CORS__AllowedOrigins__0 env var) if the frontend origin
    // ever changes, instead of hardcoding it here again.
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    options.AddPolicy("DlpDashboardCors", policy =>
    {
        if (allowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod();
        }
    });
});

// Basic anti-spam for the public (no-auth) demo-request submission endpoint: 5 submissions per
// 10 minutes per client IP, with no queueing - a 6th request in the window is rejected immediately
// with 429 rather than made to wait.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimiterPolicies.DemoRequests, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));
});

builder.Services
    .AddHealthChecks()
    .AddCheck(
        name: "self",
        check: () => HealthCheckResult.Healthy("API process is running."),
        tags: new[] { "live" })
    .AddCheck<DatabaseHealthCheck>(
        name: "database",
        tags: new[] { "ready" })
    .AddCheck<PolicySigningHealthCheck>(
        name: "policy-signing",
        tags: new[] { "ready" });

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Self-hosted Chrome/Edge extension update manifest + .crx (ExtensionInstallForcelist points here).
// No auth: the browser updater doesn't send credentials. Deploy by copying
// win-form/Al-Ameen-windows/artifacts/publish/extensions/* into wwwroot/extensions/.
var extensionContentTypes = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
extensionContentTypes.Mappings[".crx"] = "application/x-chrome-extension";
extensionContentTypes.Mappings[".xml"] = "application/xml";
var extensionsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "extensions");
if (!Directory.Exists(extensionsPath))
{
    Directory.CreateDirectory(extensionsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/extensions",
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(extensionsPath),
    ContentTypeProvider = extensionContentTypes,
    ServeUnknownFileTypes = false
});


static async Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var response = new
    {
        status = report.Status.ToString(),
        totalDuration = report.TotalDuration.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description,
            duration = entry.Value.Duration.ToString()
        })
    };

    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("DlpDashboardCors");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DLPSystemContext>();
    await dbContext.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    await seeder.Seed();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = WriteHealthResponseAsync
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponseAsync
});


app.Run();
