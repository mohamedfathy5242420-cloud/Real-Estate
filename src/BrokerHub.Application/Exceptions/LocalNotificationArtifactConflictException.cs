namespace BrokerHub.Application.Exceptions;

public sealed class LocalNotificationArtifactConflictException:Exception
{
    public LocalNotificationArtifactConflictException():base("Owned local capture conflicts with existing content or path.") { }
}
