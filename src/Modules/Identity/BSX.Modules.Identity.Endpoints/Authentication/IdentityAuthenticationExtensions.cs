using BSX.BuildingBlocks.Authorization;
using BSX.Modules.Identity.Infrastructure.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace BSX.Modules.Identity.Endpoints.Authentication;

/// <summary>Wires JWT bearer authentication and maps the principal to platform abstractions.</summary>
public static class IdentityAuthenticationExtensions
{
    /// <summary>Adds JWT bearer authentication using the Identity key ring (ADR-017).</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddIdentityAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddAuthorization();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtOptions, JwtKeyRing>((options, jwt, keyRing) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = keyRing.GetValidationKeys(),
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                };
            });

        services.Replace(ServiceDescriptor.Scoped<ICurrentUser, ClaimsPrincipalCurrentUser>());
        return services;
    }
}
