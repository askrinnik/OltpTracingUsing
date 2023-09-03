// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/trace/getting-started-console/README.md

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var myActivitySource = new ActivitySource("ConsoleApp1Source");

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: "ConsoleApp1Service", serviceVersion: "1.0.0"))
    .AddSource(myActivitySource.Name)
    .AddConsoleExporter() // Add OpenTelemetry.Exporter.Console package to use that
    .AddOtlpExporter() // 4317  Add OpenTelemetry.Exporter.OpenTelemetryProtocol package to use that
    .AddHttpClientInstrumentation() // Add pre-release OpenTelemetry.Instrumentation.Http package to use that
    .Build();

using (var activity = myActivitySource.StartActivity("RootActivity"))
{
    // put some custom data/metrics to activity tags
    activity?
        .SetTag("foo", 1)
        .SetTag("bar", "Hello, World!")
        .SetTag("baz", new[] { 1, 2, 3 });

    // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/baggage/api.md
    // The added Baggage is available in all places
    Baggage.SetBaggage("ProjectName", "OpenTelemetry using example");

    Activity.Current?.AddEvent(new ActivityEvent("Starting Http requests"));
    using (var client = new HttpClient())
    {
        using (var slow = myActivitySource.StartActivity("SomethingSlow"))
        {
            Activity.Current?.AddEvent(new ActivityEvent("Starting slow Http requests"));
            await client.GetStringAsync("https://httpstat.us/200?sleep=1000");
            await client.GetStringAsync("https://httpstat.us/200?sleep=500");
            Activity.Current?.AddEvent(new ActivityEvent("Done"));
        }
        Baggage.SetBaggage("CurrentTime", DateTime.Now.ToLongTimeString());
        using (var fast = myActivitySource.StartActivity("SomethingFast"))
        {
            Activity.Current?.AddEvent(new ActivityEvent("Starting Fast Http requests"));
            var response = await client.GetStringAsync("http://www.google.com");
            activity?.SetTag("Google Response", response[..40]);
            Activity.Current?.AddEvent(new ActivityEvent("Done"));
        }
    }
    Activity.Current?.SetStatus(ActivityStatusCode.Ok);
    var myTags = new Dictionary<string, object?> { {"tag1", "my value" }};
    Activity.Current?.AddEvent(new ActivityEvent("Done", tags: new ActivityTagsCollection(myTags)));
}

foreach (var item in Baggage.GetBaggage()) 
    Console.WriteLine($"{item.Key} = {item.Value}");

Console.WriteLine("Press any key...");
Console.ReadLine();