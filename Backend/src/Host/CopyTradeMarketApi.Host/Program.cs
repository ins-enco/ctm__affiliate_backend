// Step 1 — Build configuration (appsettings + env vars) for Serilog bootstrap
var bootstrapConfig = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Step 1b — Configure Serilog, adding Loki sink if URI is provided via environment
var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(bootstrapConfig)
    .Enrich.WithMachineName();

var lokiUri = bootstrapConfig["Loki:Uri"];
if (!string.IsNullOrWhiteSpace(lokiUri) && !lokiUri.StartsWith("SET_VIA"))
{
    loggerConfig.WriteTo.GrafanaLoki(
        uri: lokiUri,
        credentials: new LokiCredentials
        {
            Login = bootstrapConfig["Loki:Username"] ?? string.Empty,
            Password = bootstrapConfig["Loki:Password"] ?? string.Empty
        },
        labels: [new LokiLabel { Key = "app", Value = "copytrade-api" }],
        batchPostingLimit: 1000,
        queueLimit: 10000);
}

Log.Logger = loggerConfig.CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Collect all modules
var modules = new List<IModule>
{
    new AuthModule(),               // Step 2
    new TrackingModule(),           // Step 3
    new AffiliateModule(),          // Step 4
    new SubscriptionHistoryModule() // Step 5
};

// Step 5a — Mock module registered in Development only (FR-011)
if (builder.Environment.IsDevelopment())
    modules.Add(new MockModule());

// Step 5 — In-memory cache + cache abstraction
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

// Step 5b — Domain event publisher
builder.Services.AddScoped<IEventPublisher, EventPublisher>();

// Step 6 — Swagger
builder.Services.AddEndpointsApiExplorer();
var apiVersion = builder.Configuration["ApiVersion"] ?? "0.0.0-local";
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "CopyTrade Market API", Version = apiVersion });

    // JWT bearer support in Swagger UI
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token (without 'Bearer ' prefix)."
    });
    options.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            []
        }
    });
});

// Step 7 — JWT authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!))
        };
    });

// Step 8 — Authorization
builder.Services.AddAuthorization();

// Step 8b — CORS (allows the React frontend at localhost:3000 / Vite dev at 5173)
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins("http://localhost:3000", "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

// Step 9 — Register each module's services
foreach (var module in modules)
    module.RegisterServices(builder.Services, builder.Configuration);

// Controllers auto-discover from all module assemblies
var mvcBuilder = builder.Services.AddControllers()
    .AddApplicationPart(typeof(AuthModule).Assembly)
    .AddApplicationPart(typeof(TrackingModule).Assembly)
    .AddApplicationPart(typeof(AffiliateModule).Assembly)
    .AddApplicationPart(typeof(SubscriptionHistoryModule).Assembly);

// Mock module: only expose controllers in Development (FR-011).
// The assembly is a direct project reference so it's auto-added to ApplicationParts;
// explicitly remove it in non-Development environments so routes return 404.
if (!builder.Environment.IsDevelopment())
{
    mvcBuilder.ConfigureApplicationPartManager(apm =>
    {
        var mockAssembly = typeof(MockModule).Assembly;
        var part = apm.ApplicationParts
            .OfType<Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart>()
            .FirstOrDefault(p => p.Assembly == mockAssembly);
        if (part != null)
            apm.ApplicationParts.Remove(part);
    });
}

// Override model validation response: return 403 with field-level errors map
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = ctx =>
    {
        var errors = ctx.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors
                    .SelectMany(e => e.ErrorMessage.Split('|', StringSplitOptions.RemoveEmptyEntries))
                    .ToArray()
            );
        var problem = new ProblemDetails { Status = 403, Title = "Validation Failed" };
        problem.Extensions["errors"] = errors;
        return new ObjectResult(problem) { StatusCode = 403 };
    };
});

var app = builder.Build();

// Step 10a — Auto-migrate all module databases (idempotent, safe on every startup)
// Skipped for non-relational providers (e.g. EF InMemory used in integration tests)
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var authDb     = sp.GetRequiredService<AuthDbContext>();
    var affiliateDb = sp.GetRequiredService<AffiliateDbContext>();
    var trackingDb  = sp.GetRequiredService<TrackingDbContext>();

    if (authDb.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true)
    {
        // MySQL (production): run EF migrations
        await authDb.Database.MigrateAsync();
        await affiliateDb.Database.MigrateAsync();
        await trackingDb.Database.MigrateAsync();
    }
    else
    {
        // SQLite (integration tests): EnsureCreated so schema exists before DevDataSeeder runs
        authDb.Database.EnsureCreated();
        affiliateDb.Database.EnsureCreated();
        trackingDb.Database.EnsureCreated();
    }
}

// Step 10b — Seed dev data (Development only)
if (app.Environment.IsDevelopment())
    await DevDataSeeder.SeedAsync(app.Services, app.Logger);

// Step 11 — ExceptionHandlingMiddleware must be first
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();

// Step 12 — Swagger (all environments for now)
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "CopyTrade Market API v1"));

// Step 12 — Serilog request logging
app.UseSerilogRequestLogging();

// Step 13 — Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Step 12 — Map controllers
app.MapControllers();

// Meta
app.MapGet("/api/version", () => Results.Ok(new { version = apiVersion }))
   .WithTags("Meta")
   .AllowAnonymous();

// Let each module map its own endpoints
foreach (var module in modules)
    module.MapEndpoints(app);

app.Run();

// Expose Program to WebApplicationFactory in integration tests
public partial class Program { }
