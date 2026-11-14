using BrokerHub.Application.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BrokerHub.Infrastructure.Identity;

internal sealed class IdentityInitializer(
    IServiceScopeFactory scopeFactory,
    IOptions<IdentityBootstrapOptions> bootstrapOptions,
    TimeProvider timeProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                EnsureSucceeded(result, $"create role '{roleName}'");
            }
        }

        var options = bootstrapOptions.Value;
        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException(
                "IdentityBootstrap requires Email and Password when Enabled is true.");
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var normalizedEmail = options.Email.Trim();
        var account = await userManager.FindByEmailAsync(normalizedEmail);

        if (account is null)
        {
            account = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = normalizedEmail,
                Email = normalizedEmail,
                EmailConfirmed = true,
                CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
            };

            EnsureSucceeded(
                await userManager.CreateAsync(account, options.Password),
                "create bootstrap administrator");
            EnsureSucceeded(
                await userManager.AddToRoleAsync(account, AppRoles.SystemAdministrator),
                "assign bootstrap administrator role");

            return;
        }

        if (!await userManager.IsInRoleAsync(account, AppRoles.SystemAdministrator))
        {
            throw new InvalidOperationException(
                "Identity bootstrap email belongs to an existing non-administrator account. " +
                "Refusing privilege escalation.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(", ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException($"Identity initialization failed to {operation}: {errors}");
    }
}
