using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string serviceName = "WeatherForecast";

var openTelemetryBuilder = builder.Services.AddOpenTelemetry() // add the OpenTelemetry.Extensions.Hosting nuget package
    .ConfigureResource(resource => resource.AddService(serviceName));

openTelemetryBuilder
    .WithTracing(tracerProviderBuilder =>
            tracerProviderBuilder
                .AddSource(serviceName)
                .AddAspNetCoreInstrumentation(options => options.RecordException = true) // add the pre-release OpenTelemetry.Instrumentation.AspNetCore nuget package
                // .SetSampler(new AlwaysOnSampler()) // https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/trace/customizing-the-sdk/README.md#samplers
                .AddHttpClientInstrumentation() // add the pre-release OpenTelemetry.Instrumentation.Http nuget package
                .AddConsoleExporter() // add the OpenTelemetry.Exporter.Console nuget package
                .AddOtlpExporter() // 4317 // add the OpenTelemetry.Exporter.OpenTelemetryProtocol nuget package
    );


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
