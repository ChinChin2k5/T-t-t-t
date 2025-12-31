var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Ensure JSON serializer does not escape non-ASCII characters (preserve Vietnamese accents)
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
});

var app = builder.Build();

app.MapOpenApi();

app.UseHttpsRedirection();

// Serve files from wwwroot (so a real UI can be returned at the root)
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Ensure text-based responses include utf-8 charset so browsers render accented characters correctly.
        var contentType = ctx.Context.Response.ContentType;
        if (!string.IsNullOrEmpty(contentType))
        {
            if ((contentType.StartsWith("text/") || contentType == "application/javascript" || contentType == "application/json")
                && !contentType.Contains("charset", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Context.Response.ContentType = contentType + "; charset=utf-8";
            }
        }
        else
        {
            ctx.Context.Response.ContentType = "text/plain; charset=utf-8";
        }
    }
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (HttpResponse response) =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

    // Ensure JSON response includes charset;
    response.ContentType = "application/json; charset=utf-8";
    return Results.Json(forecast);
})
.WithName("GetWeatherForecast");

// Fallback to an index.html in wwwroot so the app serves a real page at '/'
app.MapFallback(async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(System.IO.Path.Combine(app.Environment.ContentRootPath, "wwwroot", "index.html"));
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
