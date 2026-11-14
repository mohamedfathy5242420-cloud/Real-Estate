using System.Net.Mail;

namespace BrokerHub.Domain.Entities;

public sealed class Customer
{
    public const int FullNameMaxLength = 200;
    public const int PhoneMaxLength = 32;
    public const int EmailMaxLength = 320;
    public const int PreferredContactMethodMaxLength = 100;
    public const int RequirementsMaxLength = 2000;

    private Customer() { }

    public Guid Id { get; private set; }
    public Guid? AccountId { get; private set; }
    public Guid? AssignedEmployeeId { get; private set; }
    public string FullName { get; private set; } = "";
    public string Phone { get; private set; } = "";
    public string? Email { get; private set; }
    public string? PreferredContactMethod { get; private set; }
    public string? Requirements { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid CreatedByAccountId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public long Version { get; private set; }

    // Nullable AccountId supports an unlinked CRM foundation. It grants no staff API permission.
    // Reviewed linking is a distinct Manager workflow, never inferred from contact details.
    public static Customer Create(Guid id, Guid? accountId, string fullName, string phone, string? email,
        string? preferredContactMethod, string? requirements, DateTime createdAtUtc, Guid actor)
    {
        if (id == Guid.Empty || accountId == Guid.Empty || actor == Guid.Empty)
            throw new ArgumentException("Nonempty identities required.", nameof(id));
        var customer = new Customer { Id = id, AccountId = accountId, CreatedAtUtc = Utc(createdAtUtc),
            CreatedByAccountId = actor, Version = 1 };
        customer.SetDetails(fullName, phone, email, preferredContactMethod, requirements);
        return customer;
    }

    public void UpdateProfile(string fullName, string phone, string? email, string? preferredContactMethod,
        string? requirements, long expectedVersion, DateTime atUtc, Guid actor)
    {
        if (expectedVersion != Version || Version == long.MaxValue)
            throw new InvalidOperationException("Customer version changed or is exhausted.");
        if (actor == Guid.Empty) throw new ArgumentException("Actor is required.", nameof(actor));
        atUtc = Utc(atUtc);
        if (atUtc < CreatedAtUtc) throw new ArgumentException("Audit time precedes creation.", nameof(atUtc));
        // Validate everything before changing this aggregate, including optional email shape.
        var name = Required(fullName, FullNameMaxLength, nameof(fullName));
        var normalizedPhone = Required(phone, PhoneMaxLength, nameof(phone));
        var normalizedEmail = EmailValue(email);
        var method = Optional(preferredContactMethod, PreferredContactMethodMaxLength, nameof(preferredContactMethod));
        var needs = Optional(requirements, RequirementsMaxLength, nameof(requirements));
        FullName = name; Phone = normalizedPhone; Email = normalizedEmail;
        PreferredContactMethod = method; Requirements = needs;
        Version++; UpdatedAtUtc = atUtc; UpdatedByAccountId = actor;
    }

    public void LinkReviewedAccount(Guid accountId, long expectedVersion, DateTime atUtc, Guid actor)
    {
        if (AccountId.HasValue || expectedVersion != Version || Version == long.MaxValue)
            throw new InvalidOperationException("Customer is linked or its version changed.");
        if (accountId == Guid.Empty || actor == Guid.Empty) throw new ArgumentException("Identities are required.");
        atUtc = Utc(atUtc);
        if (atUtc < CreatedAtUtc) throw new ArgumentException("Audit time precedes creation.");
        AccountId = accountId; Version++; UpdatedAtUtc = atUtc; UpdatedByAccountId = actor;
    }

    public void AssignTo(Guid employeeId, long expectedVersion, DateTime atUtc, Guid actor)
    {
        if (Version != expectedVersion || Version == long.MaxValue || AssignedEmployeeId == employeeId)
            throw new InvalidOperationException("Customer assignment or version changed.");
        if (employeeId == Guid.Empty || actor == Guid.Empty) throw new ArgumentException("Assignment identities required.");
        atUtc = Utc(atUtc);
        if (atUtc < CreatedAtUtc || (UpdatedAtUtc.HasValue && atUtc < UpdatedAtUtc.Value))
            throw new ArgumentException("Assignment audit precedes prior mutation.");
        AssignedEmployeeId = employeeId; Version++; UpdatedAtUtc = atUtc; UpdatedByAccountId = actor;
    }

    private void SetDetails(string name, string phone, string? email, string? method, string? needs)
    {
        FullName = Required(name, FullNameMaxLength, "fullName");
        Phone = Required(phone, PhoneMaxLength, nameof(phone));
        Email = EmailValue(email);
        PreferredContactMethod = Optional(method, PreferredContactMethodMaxLength, "preferredContactMethod");
        Requirements = Optional(needs, RequirementsMaxLength, "requirements");
    }

    private static string Required(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
            throw new ArgumentException($"A nonblank value of at most {max} characters is required.", field);
        return value.Trim();
    }
    private static string? Optional(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Required(value, max, field);
    }
    private static string? EmailValue(string? value)
    {
        var email = Optional(value, EmailMaxLength, "email");
        if (email is not null && (!MailAddress.TryCreate(email, out var address) || address.Address != email))
            throw new ArgumentException("A contact email address is required when supplied.", "email");
        return email;
    }
    private static DateTime Utc(DateTime value)
    {
        if (value == default || value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("An explicit UTC audit time is required.", nameof(value));
        return value;
    }
}
