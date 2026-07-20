using DLPManagementSystem.Authentication;
using DLPManagementSystem.Data.Seed;
using DLPManagementSystem.Helper.Health;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using DLPManagementSystem.Service.Service;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using DLPManagementSystem.CompanyDlpDashboard;

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

builder.Services.AddMemoryCache();

builder.Services.AddScoped<IPermissionLookupService, PermissionLookupService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILookupsService, LookupsService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IPermissionRequestService, PermissionRequestService>();
builder.Services.AddScoped<IPermissionGrantService, PermissionGrantService>();
builder.Services.AddScoped<IFileClassificationService, FileClassificationService>();
builder.Services.AddScoped<IFileKeyProtectionService, FileKeyProtectionService>();

builder.Services.AddDataProtection();

builder.Services.AddDbContext<DLPSystemContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecretKey = jwtSection["SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey configuration is missing.");

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
            ClockSkew = TimeSpan.FromMinutes(1)
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
    options.AddPolicy("DlpDashboardDevCors", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});




builder.Services
    .AddHealthChecks()
    .AddCheck(
        name: "self",
        check: () => HealthCheckResult.Healthy("API process is running."),
        tags: new[] { "live" })
    .AddCheck<DatabaseHealthCheck>(
        name: "database",
        tags: new[] { "ready" });

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


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

app.UseCors("DlpDashboardDevCors");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
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
