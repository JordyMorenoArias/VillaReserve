using Scalar.AspNetCore;
using VillaReserve.Api.API.Extensions;
using VillaReserve.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ----- Service Registration -----
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Configuration);

// ----- Application Build -----
var app = builder.Build();

// ----- Middleware Pipeline -----
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs/{documentName}", options =>
    {
        options.WithTitle("VillaReserve API");
    });
}

app.UseApi();

app.MapHealthChecks("/health").WithOpenApi();

app.Run();

// Expose the Program class for WebApplicationFactory in integration tests.
public partial class Program { }
