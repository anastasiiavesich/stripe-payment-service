using Microsoft.EntityFrameworkCore;
using StripeCheckoutDemo.Services;
using StripeCheckoutDemo.Models;
using StripeCheckoutDemo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=payments.db"));

builder.Services
.AddOptions<StripeOptions>()
.Bind(builder.Configuration.GetSection("Stripe"))
.Validate(options =>
    !string.IsNullOrWhiteSpace(options.SecretKey),
    "Stripe SecretKey is not configured")
.Validate(options =>
    !string.IsNullOrWhiteSpace(options.WebhookSecret),
    "Stripe WebhookSecret is not configured");

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

builder.Services.AddScoped<IStripeService, StripeService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevPolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (db.Database.IsSqlite() &&
        db.Database.GetDbConnection().ConnectionString.Contains("Data Source=payments.db"))
    {
        if (!app.Environment.IsEnvironment("Testing"))
        {
            db.Database.Migrate();
        }
    }
}

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.UseCors("LocalDevPolicy");

app.MapControllers();
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}