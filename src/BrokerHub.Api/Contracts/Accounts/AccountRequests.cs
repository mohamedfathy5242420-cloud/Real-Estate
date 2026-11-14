using System.ComponentModel.DataAnnotations;

namespace BrokerHub.Api.Contracts.Accounts;

public sealed record RegisterCustomerRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, Phone] string PhoneNumber);

public sealed record ConfirmEmailRequest(Guid AccountId, [Required] string Token);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    bool RememberMe = false);

public sealed record EmailRequest([Required, EmailAddress] string Email);

public sealed record ResetPasswordRequest(
    [Required, EmailAddress] string Email,
    [Required] string Token,
    [Required, MinLength(8)] string NewPassword);

public sealed record CreateStaffAccountRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, Phone] string PhoneNumber,
    [Required] string Role);
