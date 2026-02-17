using PwaAdoBridge.Api.Options;
using PwaAdoBridge.Api.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();


builder.Services.Configure<AzureDevOpsOptions>(
    builder.Configuration.GetSection("AzureDevOps"));


builder.Services.AddHttpClient<AzureDevOpsClient>();


builder.Services.AddScoped<PwaToAdoSyncService>();

builder.Services.AddScoped<PwaClient>();



var app = builder.Build();

// Middleware pipeline
app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();
