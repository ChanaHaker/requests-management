using Microsoft.EntityFrameworkCore;
using RequestsManagement.DAL;
using RequestsManagement.DAL.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RequestsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RequestsDb")));

builder.Services.AddScoped<RequestsSeeder>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await SeedDatabaseAsync(app, args);

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