namespace BrokerHub.Domain.Entities;

public sealed class EmployeeRevision
{
    private EmployeeRevision() { }
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public long Version { get; private set; }
    public string DisplayName { get; private set; } = "";
    public bool IsActive { get; private set; }
    public DateTime AtUtc { get; private set; }
    public Guid ActorAccountId { get; private set; }
    internal static EmployeeRevision Capture(Employee employee, DateTime atUtc, Guid actor) => new()
    { Id = Guid.NewGuid(), EmployeeId = employee.Id, Version = employee.Version, DisplayName = employee.DisplayName,
        IsActive = employee.IsActive, AtUtc = atUtc, ActorAccountId = actor };
}
