namespace BrokerHub.Domain.Enums;

// Bounded transport diagnostics, never arbitrary exception text or business permissions.
public enum OutboxFailureKind
{
    None, TransportUnavailable, NotConfirmed, ConfirmUncertain, InvalidEnvelope, Rejected, LeaseExpired
}
