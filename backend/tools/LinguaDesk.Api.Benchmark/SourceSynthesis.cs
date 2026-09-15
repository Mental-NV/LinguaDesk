using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace LinguaDesk.Api.Benchmark;

/// <summary>
/// Deterministic synthesis of exact-length synthetic source texts plus
/// canonical Unicode-scalar counting and hashing. Rehearsal sources are
/// synthetic fixture text only; reports carry hashes and lengths, never text.
/// </summary>
public static class SourceSynthesis
{
    public static int CountScalars(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var count = 0;
        var span = value.AsSpan();
        while (!span.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(span, out _, out var consumed) != OperationStatus.Done)
            {
                throw new InvalidOperationException("The source text is not well-formed Unicode.");
            }

            count++;
            span = span[consumed..];
        }

        return count;
    }

    public static string Synthesize(string baseText, int targetScalars)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseText);
        if (targetScalars <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetScalars), "The target length must be positive.");
        }

        var builder = new StringBuilder();
        while (CountScalars(builder.ToString()) < targetScalars)
        {
            builder.Append(baseText);
        }

        return TruncateToScalars(builder.ToString(), targetScalars);
    }

    public static string TruncateToScalars(string value, int targetScalars)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (targetScalars < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetScalars), "The target length must not be negative.");
        }

        var span = value.AsSpan();
        var index = 0;
        var kept = 0;
        while (kept < targetScalars && index < span.Length)
        {
            if (Rune.DecodeFromUtf16(span[index..], out _, out var consumed) != OperationStatus.Done)
            {
                throw new InvalidOperationException("The source text is not well-formed Unicode.");
            }

            index += consumed;
            kept++;
        }

        return value[..index];
    }

    public static string Sha256Hex(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}
