using BSX.Modules.Identity.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.Modules.Identity.Infrastructure.Jwt;

/// <summary>Registers the Identity JWT issuance and validation services.</summary>
public static class IdentityJwtServiceCollectionExtensions
{
    /// <summary>Registers JWT issuance/validation with the supplied options (ADR-017).</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the JWT options (issuer, audience, signing keys).</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddIdentityJwt(this IServiceCollection services, Action<JwtOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new JwtOptions();
        configure(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<JwtKeyRing>();
        services.TryAddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.TryAddSingleton<IAccessTokenValidator, JwtAccessTokenValidator>();

        return services;
    }
}
