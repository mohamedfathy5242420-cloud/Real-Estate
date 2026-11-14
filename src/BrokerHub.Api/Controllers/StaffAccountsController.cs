using System.Text;
using BrokerHub.Api.Authorization;
using BrokerHub.Api.Configuration;
using BrokerHub.Api.Contracts.Accounts;
using BrokerHub.Api.Identity;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Infrastructure.Identity;
using BrokerHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BrokerHub.Api.Controllers;

[ApiController]
[Route("api/staff-accounts")]
[Authorize(Policy = AuthorizationPolicies.SystemAdministrator)]
public sealed class StaffAccountsController(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IAccountEmailSender emailSender,
    IOptions<IdentityLinkOptions> linkOptions,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateStaffAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!AppRoles.StaffProvisioningRoles.Contains(request.Role, StringComparer.Ordinal))
        {
            ModelState.AddModelError(nameof(request.Role), "Unsupported staff role.");
            return ValidationProblem(ModelState);
        }

        var email = request.Email.Trim();
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        var creation = await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var account = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                PhoneNumber = request.PhoneNumber.Trim(),
                CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
            };

            var createResult = await userManager.CreateAsync(account, request.Password);
            if (!createResult.Succeeded)
            {
                return new AccountCreationResult(null, createResult.ToValidationErrors());
            }

            var roleResult = await userManager.AddToRoleAsync(account, request.Role);
            if (!roleResult.Succeeded)
            {
                return new AccountCreationResult(null, roleResult.ToValidationErrors());
            }

            await transaction.CommitAsync(cancellationToken);
            return new AccountCreationResult(account, null);
        });

        if (creation.Errors is not null)
        {
            return BadRequest(new ValidationProblemDetails(creation.Errors));
        }

        var account = creation.Account!;

        var token = await userManager.GenerateEmailConfirmationTokenAsync(account);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var baseUrl = linkOptions.Value.FrontendBaseUrl.TrimEnd('/');
        var link = QueryHelpers.AddQueryString(
            $"{baseUrl}/confirm-email",
            new Dictionary<string, string?>
            {
                ["accountId"] = account.Id.ToString(),
                ["token"] = encodedToken
            });
        await emailSender.SendConfirmationLinkAsync(email, link, cancellationToken);

        return StatusCode(201, new { account.Id, account.Email, request.Role });
    }

    private sealed record AccountCreationResult(
        ApplicationUser? Account,
        Dictionary<string, string[]>? Errors);
}
