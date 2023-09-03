// https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/trace/getting-started-console/README.md

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var myActivitySource = new ActivitySource("ConsoleApp1Source");

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    // Add a line below to see correct service name in the Jaeger/Grafana UI 
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: "ConsoleApp1Service", serviceVersion: "1.0.0"))
    .AddSource(myActivitySource.Name)
    .AddConsoleExporter() // Add OpenTelemetry.Exporter.Console package to use that
    .AddOtlpExporter() // 4317  Add OpenTelemetry.Exporter.OpenTelemetryProtocol package to use that
    .AddHttpClientInstrumentation() // Add pre-release OpenTelemetry.Instrumentation.Http package to use that
    .Build();

using (var activity = myActivitySource.StartActivity("RootActivity"))
{
    activity?.SetTag("foo", 1);
    activity?.SetTag("bar", "Hello, World!");
    activity?.SetTag("baz", new[] { 1, 2, 3 });
    activity?.SetStatus(ActivityStatusCode.Ok);

    using (var client = new HttpClient())
    {
        using (var slow = myActivitySource.StartActivity("SomethingSlow"))
        {
            await client.GetStringAsync("https://httpstat.us/200?sleep=1000");
            await client.GetStringAsync("https://httpstat.us/200?sleep=500");
        }

        using (var fast = myActivitySource.StartActivity("SomethingFast"))
        {
            await client.GetStringAsync("https://httpstat.us/301");
        }
    }
}

Console.WriteLine("Press any key...");
Console.ReadLine();