using OpenRouter.NET;

var builder = WebApplication.CreateBuilder(args);

var apiKey = builder.Configuration["OpenRouter:ApiKey"] 
    ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

if (string.IsNullOrEmpty(apiKey))
{
    throw new InvalidOperationException(
        "OpenRouter API key not found. Set OPENROUTER_API_KEY environment variable or configure OpenRouter:ApiKey in appsettings.json");
}

builder.Services.AddSingleton(sp => new OpenRouterClient(new OpenRouterClientOptions
{
    ApiKey = apiKey,
    SiteUrl = builder.Configuration["OpenRouter:SiteUrl"],
    SiteName = builder.Configuration["OpenRouter:SiteName"] ?? "OpenRouter Test API"
}));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
