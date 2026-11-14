using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BrokerHub.Domain.Entities;
namespace BrokerHub.Api.Contracts.Bookings;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CancelBookingRequest([Required,Range(1,long.MaxValue)] long? Version,
    [MaxLength(Booking.CancellationReasonMaxLength)] string? Reason = null);
