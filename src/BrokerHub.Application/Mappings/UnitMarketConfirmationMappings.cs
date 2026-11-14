using System.Linq.Expressions;
using BrokerHub.Application.DTOs.UnitMarketConfirmations;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Application.Mappings;
public static class UnitMarketConfirmationMappings
{
    public static Expression<Func<UnitMarketConfirmation, UnitMarketConfirmationDto>> Projection => c =>
        new(c.Id, c.UnitId, DateTime.SpecifyKind(c.ConfirmedAtUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(c.RecordedAtUtc, DateTimeKind.Utc), c.RecordedByAccountId,
            c.Source, c.SourceReference, c.Notes, c.ConfirmedPrice, c.Currency, c.AvailabilityStatus);
    public static UnitMarketConfirmationDto ToDto(this UnitMarketConfirmation c) =>
        new(c.Id, c.UnitId, DateTime.SpecifyKind(c.ConfirmedAtUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(c.RecordedAtUtc, DateTimeKind.Utc), c.RecordedByAccountId,
            c.Source, c.SourceReference, c.Notes, c.ConfirmedPrice, c.Currency, c.AvailabilityStatus);
}
