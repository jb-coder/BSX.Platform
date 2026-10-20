using BSX.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSX.Web.IntegrationTests;

/// <summary>Host variant with password-reset delivery disabled (production default, D3).</summary>
public sealed class DisabledPasswordResetIdentityApiFactory : IdentityApiFactory
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPasswordResetNotifier>();
            services.AddSingleton<IPasswordResetNotifier, DisabledPasswordResetNotifier>();
        });
    }
}

/// <summary>A notifier that reports delivery as unavailable and never sends.</summary>
internal sealed class DisabledPasswordResetNotifier : IPasswordResetNotifier
{
    /// <inheritdoc />
    public bool IsDeliveryEnabled => false;

    /// <inheritdoc />
    public Task NotifyAsync(string email, string resetToken, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
