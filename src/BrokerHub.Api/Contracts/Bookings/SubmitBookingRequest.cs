using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Api.Contracts.Bookings;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SubmitBookingRequest([Required] Guid? UnitId,
    [MaxLength(Booking.RequestReasonMaxLength)] string? RequestReason = null);
