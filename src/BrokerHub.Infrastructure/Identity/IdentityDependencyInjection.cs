using BrokerHub.Infrastructure.Persistence;
using BrokerHub.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BrokerHub.Infrastructure.Identity;

internal static class IdentityDependencyInjection
{
    internal static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if(configuration["DataProtection:KeysPath"] is {Length:>0} keysPath)
        {
            if(!Path.IsPathFullyQualified(keysPath))throw new InvalidOperationException("A private absolute data-protection directory is required.");
            var resolved=Path.GetFullPath(keysPath);var content=Path.GetFullPath(environment.ContentRootPath).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(resolved.StartsWith(content,StringComparison.OrdinalIgnoreCase)||resolved.Equals(content.TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Data-protection keys require a private directory outside API content.");
            services.AddDataProtection().SetApplicationName("BrokerHub").PersistKeysToFileSystem(new DirectoryInfo(resolved));
        }
        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = environment.IsDevelopment()
                ? "BrokerHub.Auth.Development"
                : "__Host-BrokerHub.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.Cookie.Path = "/";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events.OnValidatePrincipal = CurrentAccountCookieValidator.ValidateAsync;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services
            .AddOptions<IdentityBootstrapOptions>()
            .Bind(configuration.GetSection(IdentityBootstrapOptions.SectionName));

        services.AddHostedService<IdentityInitializer>();
        services.AddOptions<DevelopmentIdentityCaptureOptions>()
            .Bind(configuration.GetSection(DevelopmentIdentityCaptureOptions.SectionName))
            .Validate(o=>o.IsValid,"Invalid owned Development identity capture configuration.")
            .ValidateOnStart();

        if (environment.IsDevelopment())
        {
            services.AddSingleton<IAccountEmailSender, DevelopmentAccountEmailSender>();
        }
        else
        {
            services.AddSingleton<IAccountEmailSender, UnconfiguredAccountEmailSender>();
        }

        return services;
    }
}
