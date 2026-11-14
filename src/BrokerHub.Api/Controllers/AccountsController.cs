using System.Text;
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
[Route("api/accounts")]
public sealed class AccountsController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext dbContext,
    IAccountEmailSender emailSender,
    IOptions<IdentityLinkOptions> linkOptions,
    TimeProvider timeProvider) : ControllerBase
{
    private const string GenericEmailMessage =
        "If the account is eligible, an email has been prepared.";

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        RegisterCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        var registration = await executionStrategy.ExecuteAsync(async () =>
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

            var roleResult = await userManager.AddToRoleAsync(account, AppRoles.Customer);
            if (!roleResult.Succeeded)
            {
                return new AccountCreationResult(null, roleResult.ToValidationErrors());
            }

            await transaction.CommitAsync(cancellationToken);
            return new AccountCreationResult(account, null);
        });

        if (registration.Errors is not null)
        {
            return BadRequest(new ValidationProblemDetails(registration.Errors));
        }

        await SendConfirmationAsync(registration.Account!, cancellationToken);

        return Accepted(new AcceptedMessageResponse(GenericEmailMessage));
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request)
    {
        var account = await userManager.FindByIdAsync(request.AccountId.ToString());
        if (account is null)
        {
            return BadRequest();
        }

        var token = DecodeToken(request.Token);
        if (token is null)
        {
            return BadRequest();
        }

        var result = await userManager.ConfirmEmailAsync(account, token);
        return result.Succeeded
            ? NoContent()
            : BadRequest(new ValidationProblemDetails(result.ToValidationErrors()));
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendConfirmation(
        EmailRequest request,
        CancellationToken cancellationToken)
    {
        var account = await userManager.FindByEmailAsync(request.Email.Trim());
        if (account is not null && !await userManager.IsEmailConfirmedAsync(account))
        {
            await SendConfirmationAsync(account, cancellationToken);
        }

        return Accepted(new AcceptedMessageResponse(GenericEmailMessage));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var account = await userManager.FindByEmailAsync(request.Email.Trim());
        if (account is null || !account.IsActive)
        {
            return Unauthorized();
        }

        var result = await signInManager.PasswordSignInAsync(
            account,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.IsNotAllowed)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Email confirmation required.");
        }

        if (result.IsLockedOut)
        {
            return Problem(statusCode: StatusCodes.Status423Locked, title: "Account is locked.");
        }

        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        return Ok(await CreateResponseAsync(account));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var account = await userManager.GetUserAsync(User);
        return account is null ? Unauthorized() : Ok(await CreateResponseAsync(account));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        EmailRequest request,
        CancellationToken cancellationToken)
    {
        var account = await userManager.FindByEmailAsync(request.Email.Trim());
        if (account is not null && await userManager.IsEmailConfirmedAsync(account))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(account);
            var link = BuildFrontendLink(
                "reset-password",
                ("email", account.Email!),
                ("token", EncodeToken(token)));
            await emailSender.SendPasswordResetLinkAsync(account.Email!, link, cancellationToken);
        }

        return Accepted(new AcceptedMessageResponse(GenericEmailMessage));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var account = await userManager.FindByEmailAsync(request.Email.Trim());
        var token = DecodeToken(request.Token);
        if (account is null || token is null)
        {
            return BadRequest();
        }

        var result = await userManager.ResetPasswordAsync(account, token, request.NewPassword);
        return result.Succeeded
            ? NoContent()
            : BadRequest(new ValidationProblemDetails(result.ToValidationErrors()));
    }

    private async Task SendConfirmationAsync(
        ApplicationUser account,
        CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(account);
        var link = BuildFrontendLink(
            "confirm-email",
            ("accountId", account.Id.ToString()),
            ("token", EncodeToken(token)));
        await emailSender.SendConfirmationLinkAsync(account.Email!, link, cancellationToken);
    }

    private string BuildFrontendLink(string path, params (string Name, string Value)[] parameters)
    {
        var baseUrl = linkOptions.Value.FrontendBaseUrl.TrimEnd('/');
        var query = parameters.ToDictionary(item => item.Name, item => (string?)item.Value);
        return QueryHelpers.AddQueryString($"{baseUrl}/{path}", query);
    }

    private async Task<AccountResponse> CreateResponseAsync(ApplicationUser account)
    {
        var roles = await userManager.GetRolesAsync(account);
        return new AccountResponse(account.Id, account.Email!, roles.ToArray());
    }

    private static string EncodeToken(string token) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static string? DecodeToken(string encodedToken)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private sealed record AccountCreationResult(
        ApplicationUser? Account,
        Dictionary<string, string[]>? Errors);
}
