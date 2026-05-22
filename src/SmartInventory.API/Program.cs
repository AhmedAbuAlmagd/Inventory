using SmartInventory.API.Data;
using SmartInventory.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSmartInventoryServices(builder.Configuration, builder.Environment);

var app = builder.Build();

await DbSeeder.MigrateAndSeedAsync(app.Services);

app.UseSmartInventoryPipeline();

app.Run();

public partial class Program
{
}
