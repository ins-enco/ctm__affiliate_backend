// Step 1 — Configure Serilog on the host
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .Enrich.WithMachineName()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Collect all modules
var modules = new List<IModule>
{
    new AuthModule(),       // Step 2
    new TrackingModule(),   // Step 3
    new AffiliateModule()   // Step 4
};

// Step 5 — In-memory cache
builder.Services.AddMemoryCache();

// Step 6 — JWT authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer();

// Step 7 — Authorization
builder.Services.AddAuthorization();

// Step 8 — Register each module's services
foreach (var module in modules)
    module.RegisterServices(builder.Services, builder.Configuration);

// Controllers auto-discover from all module assemblies
builder.Services.AddControllers()
    .AddApplicationPart(typeof(AuthModule).Assembly)
    .AddApplicationPart(typeof(TrackingModule).Assembly)
    .AddApplicationPart(typeof(AffiliateModule).Assembly);

var app = builder.Build();

// Step 9 — ExceptionHandlingMiddleware must be first
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Step 10 — Serilog request logging
app.UseSerilogRequestLogging();

// Step 11 — Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Step 12 — Map controllers
app.MapControllers();

// Let each module map its own endpoints
foreach (var module in modules)
    module.MapEndpoints(app);

app.Run();
