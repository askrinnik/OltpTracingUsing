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
        // put some custom data/metrics to activity tags
        Activity.Current?
            .SetTag("foo", 1)
            .SetTag("bar", "Hello, World!")
            .SetTag("baz", new[] { 1, 2, 3 });

        using (var client = new HttpClient())
        {
            using (var slow = Activity.Current?.Source.StartActivity("SomethingSlow"))
            {
                Activity.Current?.AddEvent(new ActivityEvent("Starting slow Http requests"));
                await client.GetStringAsync("https://httpstat.us/200?sleep=1000");
                await client.GetStringAsync("https://httpstat.us/200?sleep=500");
                Activity.Current?.AddEvent(new ActivityEvent("Done"));
            }
            using (var fast = Activity.Current?.Source.StartActivity("SomethingFast"))
            {
                Activity.Current?.AddEvent(new ActivityEvent("Starting Fast Http requests"));
                await client.GetStringAsync("https://httpstat.us/301");
                Activity.Current?.AddEvent(new ActivityEvent("Done"));
            }
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
}