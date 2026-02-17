using PwaAdoBridge.Api.Options;
using PwaAdoBridge.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "PwaAdoBridge API",
        Version = "v1",
        Description = "Backend API that synchronizes Microsoft Project Online (PWA) projects and tasks with Azure DevOps work items.",
        Contact = new()
        {
            Name = "Bryan Cedeno",
            Email = "bcedeno2412@gmail.com"
        }
    });
});

builder.Services.Configure<AzureDevOpsOptions>(
    builder.Configuration.GetSection("AzureDevOps"));

builder.Services.AddHttpClient<AzureDevOpsClient>();

builder.Services.AddScoped<PwaToAdoSyncService>();
builder.Services.AddScoped<PwaClient>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();
