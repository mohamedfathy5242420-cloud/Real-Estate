namespace BrokerHub.Domain.Entities;

// Actual external fact, independent of the immutable commission entitlement.
public sealed class CommissionCollection
{
    public const int SourceMaxLength=200,ReferenceMaxLength=500,NotesMaxLength=2000;
    private CommissionCollection() { }
    public Guid Id { get; private set; }
    public Guid DealId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid UnitId { get; private set; }
    public long RequestedVersion { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; }="";
    public decimal EntitledAmount { get; private set; }
    public DateTime SaleOccurredAtUtc { get; private set; }
    public DateTime CollectedAtUtc { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
    public Guid RecorderAccountId { get; private set; }
    public string Source { get; private set; }="";
    public string Reference { get; private set; }="";
    public string? Notes { get; private set; }
    public long Version { get; private set; }=1;

    public static bool CanRecordAt(Deal deal,DateTime? latest,DateTime now)=>now>=deal.AdoptedAtUtc&&!(now<latest);
    public static CommissionCollection Record(Guid id,Deal deal,long version,decimal alreadyCollected,DateTime? latest,
        decimal amount,string currency,DateTime collected,DateTime recorded,Guid actor,string source,string reference,string? notes)
    {
        ArgumentNullException.ThrowIfNull(deal);
        if(deal.Id==Guid.Empty||version<=0||deal.Version!=version||!CanRecordAt(deal,latest,recorded)||
            alreadyCollected<0m||alreadyCollected>deal.Amount||amount>deal.Amount-alreadyCollected)
            throw new InvalidOperationException("Current immutable entitlement and protected remaining recorded balance required.");
        if(id==Guid.Empty||actor==Guid.Empty||!BookingDeposit.ValidMoney(amount,currency)||currency!=deal.CurrencyCode||
            collected==default||recorded==default||collected.Kind!=DateTimeKind.Utc||recorded.Kind!=DateTimeKind.Utc||
            collected<deal.SaleOccurredAtUtc||collected>recorded||string.IsNullOrWhiteSpace(source)||source.Length>SourceMaxLength||
            string.IsNullOrWhiteSpace(reference)||reference.Length>ReferenceMaxLength||notes?.Length>NotesMaxLength)
            throw new ArgumentException("Actual same-currency post-sale collection with bounded evidence and UTC audit required.");
        return new(){Id=id,DealId=deal.Id,CustomerId=deal.CustomerId,UnitId=deal.UnitId,RequestedVersion=version,
            Amount=amount,CurrencyCode=currency,EntitledAmount=deal.Amount,
            SaleOccurredAtUtc=DateTime.SpecifyKind(deal.SaleOccurredAtUtc,DateTimeKind.Utc),CollectedAtUtc=collected,
            RecordedAtUtc=recorded,RecorderAccountId=actor,Source=source.Trim(),Reference=reference.Trim(),
            Notes=string.IsNullOrWhiteSpace(notes)?null:notes.Trim()};
    }
}
