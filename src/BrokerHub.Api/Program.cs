using BrokerHub.Api.Configuration;
using BrokerHub.Api.Authorization;
using BrokerHub.Api.Identity;
using BrokerHub.Application;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<BrokerHubOptions>()
    .Bind(builder.Configuration.GetRequiredSection(BrokerHubOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddBrokerHubAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();

public partial class Program;
