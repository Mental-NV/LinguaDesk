namespace LinguaDesk.Api.Infrastructure.Security;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public string? DataProtectionKeysPath { get; init; }

    public string[] ForwardedHeaderTrustedNetworks { get; init; } = [];
}
