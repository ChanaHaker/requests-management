using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RequestsManagement.Api.Middleware;
using RequestsManagement.BL.Caching;
using RequestsManagement.BL.Services;
using RequestsManagement.DAL;
using RequestsManagement.DAL.Repositories;
using RequestsManagement.DAL.Seed;

var builder = WebApplication.CreateBuilder(args);

// Data access layer (Scoped: one DbContext per request)
builder.Services.AddDbContext<RequestsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RequestsDb")));
builder.Services.AddScoped<IRequestRepository, RequestRepository>();
builder.Services.AddScoped<RequestsSeeder>();

// Business logic layer
builder.Services.AddScoped<IRequestService, RequestService>();

// Cache (Singleton: one shared cache for all requests; depends only on singletons)
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IRequestStatisticsCache, MemoryRequestStatisticsCache>();

// API layer
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Global error handling: exceptions become Problem Details responses
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

await SeedDatabaseAsync(app, args);

// Must be first, so it catches exceptions from everything after it.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

static async Task SeedDatabaseAsync(WebApplication app, string[] args)
{
    var seeding = app.Configuration.GetSection("Seeding");
    if (!seeding.GetValue<bool>("Enabled"))
    {
        return;
    }

    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<RequestsSeeder>();

    var count = seeding.GetValue<int>("RequestCount");
    var reset = args.Contains("--reseed");

    await seeder.SeedAsync(count, reset);
}

// Makes the auto-generated Program class visible to the integration tests (WebApplicationFactory<Program>).
public partial class Program
{
}