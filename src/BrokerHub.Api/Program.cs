using BrokerHub.Api.Configuration;
using BrokerHub.Api.Authorization;
using BrokerHub.Api.Identity;
using BrokerHub.Application;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Infrastructure;
using BrokerHub.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<BrokerHubOptions>()
    .Bind(builder.Configuration.GetRequiredSection(BrokerHubOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<IdentityLinkOptions>()
    .Bind(builder.Configuration.GetRequiredSection(IdentityLinkOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddBrokerHubAuthorization();
builder.Services.AddBrokerHubApiSecurity(builder.Configuration,builder.Environment);
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDevelopmentSwagger();
}

var app = builder.Build();

app.UseMiddleware<ApiResponseBoundaryMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseDevelopmentSwagger();
}
app.UseRouting();
app.UseCors(ApiSecurityExtensions.CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ApiRequestVerificationMiddleware>();

app.MapControllers();
app.MapBrokerHubHealthEndpoints();

app.Run();

public partial class Program;
