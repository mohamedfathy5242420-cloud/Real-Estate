using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Infrastructure.Persistence.Queries;
using BrokerHub.Infrastructure.Persistence.Repositories;
using BrokerHub.Infrastructure.Persistence.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BrokerHub.Infrastructure.Persistence;

internal static class PersistenceDependencyInjection
{
    internal static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BrokerHubDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'BrokerHubDatabase' is required.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));

        services.AddScoped<IDeveloperRepository, DeveloperRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICrmCustomerRepository, CrmCustomerRepository>();
        services.AddScoped<ICrmCustomerQueries, CrmCustomerQueries>();
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<ICustomerRequestScope, CustomerRequestScope>();
        services.AddScoped<IRequestUnitRepository, RequestUnitRepository>();
        services.AddScoped<IViewingRepository, ViewingRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IBookingQueries, BookingQueries>();
        services.AddScoped<IDealRepository, DealRepository>();
        services.AddScoped<IDealQueries, DealQueries>();
        services.AddScoped<IBookingExpiryStore, BookingExpiryStore>();
        services.AddScoped<IViewingCustomerQueries, ViewingCustomerQueries>();
        services.AddScoped<IViewingCoordinationRepository, ViewingCoordinationRepository>();
        services.AddScoped<IViewingLifecycleRepository, ViewingLifecycleRepository>();
        services.AddScoped<IViewingExpiryStore, ViewingExpiryStore>();
        services.AddScoped<IViewingStaffQueries, ViewingStaffQueries>();
        services.AddScoped<ICrmFollowUpRepository, CrmFollowUpRepository>();
        services.AddScoped<ICrmFollowUpQueries, CrmFollowUpQueries>();
        services.AddScoped<ILeadQueries, LeadQueries>();
        services.AddScoped<ICommandExecutionStore, CommandExecutionStore>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEmployeeQueries, EmployeeQueries>();
        services.AddScoped<ICustomerLinkRepository, CustomerLinkRepository>();
        services.AddScoped<ICustomerLinkQueries, CustomerLinkQueries>();
        services.AddScoped<ICustomerQueryService, CustomerQueryService>();
        services.AddScoped<IDeveloperQueryService, DeveloperQueryService>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectQueryService, ProjectQueryService>();
        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<IUnitPublicationRepository, UnitPublicationRepository>();
        services.AddScoped<IUnitQueryService, UnitQueryService>();
        services.AddScoped<IPublicCatalogQueries, PublicCatalogQueries>();
        services.AddScoped<IUnitImageRepository, UnitImageRepository>();
        services.AddScoped<IUnitImageQueryService, UnitImageQueryService>();
        services.AddScoped<IUnitMarketConfirmationRepository, UnitMarketConfirmationRepository>();
        services.AddScoped<IMarketingAgreementRepository, MarketingAgreementRepository>();
        services.AddScoped<IMarketingAgreementQueryService, MarketingAgreementQueryService>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ISalesReportQueries, SalesReportQueries>();
        services.AddScoped<IOutboxDeliveryStore, OutboxDeliveryStore>();
        services.AddScoped<INotificationInboxStore, NotificationInboxStore>();
        services.AddScoped<IUserNotificationQueries, UserNotificationQueries>();
        services.AddScoped<INotificationReadStore, NotificationReadStore>();
        services.AddScoped<INotificationCaptureStore, NotificationCaptureStore>();

        return services;
    }
}
