using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using System.Diagnostics;

namespace WebApplication1.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        Activity.Current?.AddEvent(new ActivityEvent("Starting Get"));

        using (var client = new HttpClient())
        {
            await RunSlowWork(client);
            await RunFastWork(client);
        }

        var weatherForecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();

        var averageTemperature = weatherForecasts.Average(x => x.TemperatureC);
        // Add an event with a custom tag
        var myTags = new Dictionary<string, object?> { { "AverageTemperature", averageTemperature } };
        Activity.Current?.AddEvent(new ActivityEvent("Forecast was generated", tags: new ActivityTagsCollection(myTags)));

        return weatherForecasts;
    }

    private static async Task RunFastWork(HttpClient client)
    {
        using var fast = Activity.Current?.Source.StartActivity("SomethingFast");
        var response = await client.GetStringAsync("http://www.google.com");
        Activity.Current?.SetTag("Google Response", response[..100]);
    }

    private static async Task RunSlowWork(HttpClient client)
    {
        using var slow = Activity.Current?.Source.StartActivity("SomethingSlow");
        Activity.Current?.AddEvent(new ActivityEvent("Starting slow Http requests"));
        await client.GetStringAsync("https://httpstat.us/200?sleep=1000");
        Activity.Current?.AddEvent(new ActivityEvent("Done"));
    }
}