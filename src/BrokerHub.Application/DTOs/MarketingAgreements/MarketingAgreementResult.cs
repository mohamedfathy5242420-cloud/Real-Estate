namespace BrokerHub.Application.DTOs.MarketingAgreements;

public enum MarketingAgreementStatus { Success, ValidationFailed, NotFound, Conflict, Forbidden }
public sealed record MarketingAgreementResult<T>(MarketingAgreementStatus Status, T? Value, IReadOnlyDictionary<string, string[]> Errors)
{
    public static MarketingAgreementResult<T> Success(T value) => new(MarketingAgreementStatus.Success, value, new Dictionary<string,string[]>());
    public static MarketingAgreementResult<T> Failure(MarketingAgreementStatus status, string field, string message) =>
        new(status, default, new Dictionary<string,string[]>(StringComparer.Ordinal) { [field] = [message] });
    public static MarketingAgreementResult<T> Forbidden() => Failure(MarketingAgreementStatus.Forbidden, "Authorization", "SystemAdministrator permission required.");
}
