using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BrokerHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDeveloperService, DeveloperService>();
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();
        services.AddScoped<ICustomerAssignmentService, CustomerAssignmentService>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IViewingRequestService, ViewingRequestService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingResponseService, BookingResponseService>();
        services.AddScoped<IBookingExpiryStager, BookingExpiryStager>();
        services.AddScoped<IBookingExpiryService, BookingExpiryService>();
        services.AddScoped<IBookingExtensionService, BookingExtensionService>();
        services.AddScoped<IBookingCancellationService, BookingCancellationService>();
        services.AddScoped<IBookingDepositService, BookingDepositService>();
        services.AddScoped<IBookingRefundService, BookingRefundService>();
        services.AddScoped<IDealService, DealService>();
        services.AddScoped<ICommissionCollectionService, CommissionCollectionService>();
        services.AddScoped<ISalesReportService, SalesReportService>();
        services.AddScoped<IOutboxDispatcher, OutboxDispatcher>();
        services.AddScoped<IUserNotificationService, UserNotificationService>();
        services.AddScoped<IViewingCoordinationService, ViewingCoordinationService>();
        services.AddScoped<IViewingAlternativeService, ViewingAlternativeService>();
        services.AddScoped<IViewingChangeService, ViewingChangeService>();
        services.AddScoped<IViewingExpiryService, ViewingExpiryService>();
        services.AddScoped<IViewingOutcomeService, ViewingOutcomeService>();
        services.AddScoped<ICrmFollowUpService, CrmFollowUpService>();
        services.AddScoped<ILeadOpportunityService, LeadOpportunityService>();
        services.AddScoped<ICommandExecutor, CommandExecutor>();
        services.AddScoped<IEmployeeProfileService, EmployeeProfileService>();
        services.AddScoped<ICustomerLinkService, CustomerLinkService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<IPublicCatalogService, PublicCatalogService>();
        services.AddScoped<IUnitPublicationService, UnitPublicationService>();
        services.AddScoped<IUnitImageService, UnitImageService>();
        services.AddScoped<IUnitMarketConfirmationService, UnitMarketConfirmationService>();
        services.AddScoped<IMarketingAgreementService, MarketingAgreementService>();

        return services;
    }
}
