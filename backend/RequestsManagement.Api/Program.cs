using Microsoft.EntityFrameworkCore;
using RequestsManagement.DAL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RequestsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RequestsDb")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();