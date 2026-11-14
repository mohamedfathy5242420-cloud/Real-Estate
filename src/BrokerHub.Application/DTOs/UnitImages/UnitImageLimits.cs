namespace BrokerHub.Application.DTOs.UnitImages;
// Technical resource limits, not Business policy.
public static class UnitImageLimits
{
    public const long MaximumInputBytes = 5 * 1024 * 1024;
    public const long MaximumRequestBytes = 6 * 1024 * 1024;
    public const long MaximumStoredBytes = 20 * 1024 * 1024;
    public const int MaximumDimension = 4096;
    public const long MaximumPixels = 16_000_000;
}
