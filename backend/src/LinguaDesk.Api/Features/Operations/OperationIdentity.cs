using LinguaDesk.Core;

namespace LinguaDesk.Api.Features.Operations;

public enum OperationIdentityStatus
{
    Valid,
    Malformed,
    FutureSkew,
    Expired,
}

public sealed record OperationIdentityAssessment(
    OperationIdentityStatus Status,
    Guid OperationId,
    DateTimeOffset IdentityTime,
    DateTimeOffset ServerTime);

public static class OperationIdentity
{
    public static OperationIdentityAssessment Assess(string? value, DateTimeOffset serverTime)
    {
        if (!Guid.TryParse(value, out var operationId) || !IsUuidV7(operationId))
        {
            return new(OperationIdentityStatus.Malformed, Guid.Empty, DateTimeOffset.MinValue, serverTime);
        }

        var identityTime = ToIdentityTime(operationId);
        if (identityTime > serverTime.AddSeconds(ProductCatalog.OperationIdentityMaximumFutureSkewSeconds))
        {
            return new(OperationIdentityStatus.FutureSkew, operationId, identityTime, serverTime);
        }

        if (serverTime >= identityTime.AddSeconds(ProductCatalog.OperationIdentityValidForSeconds))
        {
            return new(OperationIdentityStatus.Expired, operationId, identityTime, serverTime);
        }

        return new(OperationIdentityStatus.Valid, operationId, identityTime, serverTime);
    }

    public static bool IsUuidV7(Guid value)
    {
        var text = value.ToString("D");
        return text[14] == '7'
            && text[19] is '8' or '9' or 'a' or 'b' or 'A' or 'B';
    }

    public static DateTimeOffset ToIdentityTime(Guid value)
    {
        var text = value.ToString("D");
        var timestampHex = string.Concat(text.AsSpan(0, 8), text.AsSpan(9, 4));
        return DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(timestampHex, 16));
    }

    public static Guid CreateForTime(DateTimeOffset time)
    {
        Span<byte> entropy = stackalloc byte[10];
        Random.Shared.NextBytes(entropy);
        return CreateForTime(time, entropy);
    }

    public static Guid CreateForTime(DateTimeOffset time, ReadOnlySpan<byte> entropy)
    {
        if (entropy.Length != 10)
        {
            throw new ArgumentException("UUIDv7 test entropy must be exactly 10 bytes.", nameof(entropy));
        }

        var milliseconds = time.ToUnixTimeMilliseconds();
        var timeHigh = unchecked((int)((milliseconds >> 16) & 0xFFFFFFFF));
        var timeLow = unchecked((short)(milliseconds & 0xFFFF));
        var randomAndVersion = unchecked((short)(((entropy[0] << 8) | entropy[1]) & 0x0FFF | 0x7000));
        var clockAndVariant = new[]
        {
            (byte)((entropy[2] & 0x3F) | 0x80),
            entropy[3],
            entropy[4],
            entropy[5],
            entropy[6],
            entropy[7],
            entropy[8],
            entropy[9],
        };
        return new Guid(timeHigh, timeLow, randomAndVersion, clockAndVariant);
    }
}
