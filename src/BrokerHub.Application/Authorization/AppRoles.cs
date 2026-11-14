namespace BrokerHub.Application.Authorization;

public static class AppRoles
{
    public const string Customer = "Customer";
    public const string SalesEmployee = "SalesEmployee";
    public const string SalesManager = "SalesManager";
    public const string SystemAdministrator = "SystemAdministrator";

    public static IReadOnlyCollection<string> All { get; } =
    [
        Customer,
        SalesEmployee,
        SalesManager,
        SystemAdministrator
    ];

    public static IReadOnlyCollection<string> StaffProvisioningRoles { get; } =
    [
        SalesEmployee,
        SalesManager,
        SystemAdministrator
    ];
}
