namespace BrokerHub.Application.DTOs.UnitImages;

public enum UnitImageStatus { Success, ValidationFailed, NotFound, Conflict, Forbidden, Unavailable }
public sealed record UnitImageResult<T>(UnitImageStatus Status, T? Value, string Field, string Message)
{
    public static UnitImageResult<T> Success(T value) => new(UnitImageStatus.Success, value, "", "");
    public static UnitImageResult<T> Failure(UnitImageStatus status, string field, string message) => new(status, default, field, message);
}
