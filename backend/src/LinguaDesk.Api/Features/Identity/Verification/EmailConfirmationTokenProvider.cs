using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Features.Identity.Verification;

public sealed class LinguaDeskEmailConfirmationTokenProvider<TUser>(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<LinguaDeskEmailConfirmationTokenProviderOptions> options,
    ILogger<DataProtectorTokenProvider<TUser>> logger)
    : DataProtectorTokenProvider<TUser>(dataProtectionProvider, options, logger)
    where TUser : class;

public sealed class LinguaDeskEmailConfirmationTokenProviderOptions : DataProtectionTokenProviderOptions;

public static class LinguaDeskEmailConfirmationTokenPolicy
{
    public const string ProviderName = "LinguaDeskEmailConfirmation";
    public static readonly TimeSpan TokenLifespan = TimeSpan.FromHours(24);
}
