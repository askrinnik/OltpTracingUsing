// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/trace/getting-started-console/README.md

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var myActivitySource = new ActivitySource("ConsoleApp1");

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: "ConsoleApp1", serviceVersion: "1.0.0"))
    .AddSource(myActivitySource.Name)
    .AddConsoleExporter() // Add OpenTelemetry.Exporter.Console package to use that
    .AddOtlpExporter() // 4317  Add OpenTelemetry.Exporter.OpenTelemetryProtocol package to use that
    .AddHttpClientInstrumentation() // Add pre-release OpenTelemetry.Instrumentation.Http package to use that
    .Build();

await RunRootWork(myActivitySource);
//await GetWeatherForecast();

Console.WriteLine("Press any key...");
Console.ReadLine();

async Task RunRootWork(ActivitySource activitySource)
{
    using (var activity = activitySource.StartActivity("RunRootWork"))
    {
        // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/baggage/api.md
        // The added Baggage is available in all places
        Baggage.SetBaggage("ProjectName", "OpenTelemetry using example");

        Activity.Current?.AddEvent(new ActivityEvent("Starting Http requests"));
        using (var client = new HttpClient())
        {
            await RunSlowWork(client);
            Baggage.SetBaggage("CurrentTime", DateTime.Now.ToLongTimeString());
            await RunFastWork(client);
        }

        Activity.Current?.SetStatus(ActivityStatusCode.Ok);

        // Add event with tags
        var eventTags = new Dictionary<string, object?> { { "tag1", "my value" } };
        Activity.Current?.AddEvent(new ActivityEvent("Done", tags: new ActivityTagsCollection(eventTags)));
    }
    foreach (var item in Baggage.GetBaggage())
        Console.WriteLine($"{item.Key} = {item.Value}");
}
async Task RunSlowWork(HttpClient httpClient)
{
    using (var slow = Activity.Current?.Source.StartActivity("RunSlowWork"))
    {
        Activity.Current?.AddEvent(new ActivityEvent("Starting slow Http requests"));
        await httpClient.GetStringAsync("https://httpstat.us/200?sleep=1000");
        Activity.Current?.AddEvent(new ActivityEvent("Done"));
    }
}
async Task RunFastWork(HttpClient client1)
{
    using (var fast = Activity.Current?.Source.StartActivity("RunFastWork"))
    {
        var response = await client1.GetStringAsync("http://www.google.com");
        // put some custom data/metrics to activity tags
        Activity.Current?.SetTag("Google Response", response[..40]);
    }
}

static async Task GetWeatherForecast()
{
    using var client = new HttpClient();
    var response = await client.GetStringAsync(new Uri("http://localhost:5233/WeatherForecast"));
}
