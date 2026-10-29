using BSX.BuildingBlocks.Cqrs;
using BSX.Web.Authentication;
using Microsoft.AspNetCore.Components.Authorization;


namespace BSX.Web.Administration;

/// <summary>
/// Dispatches administration requests in a fresh application scope. Blazor Server scoped services
/// live for the whole circuit; using a request-like scope per operation prevents stale tracked
/// aggregates from leaking across administration actions.
/// </summary>
public sealed class AdministrationDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    /// <summary>Initializes a new instance of the <see cref="AdministrationDispatcher"/> class.</summary>
    public AdministrationDispatcher(
        IServiceScopeFactory scopeFactory,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _scopeFactory = scopeFactory;
        _authenticationStateProvider = authenticationStateProvider;
    }

    /// <summary>Sends a request using a fresh administration scope.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    public async Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        AuthenticationState state = await _authenticationStateProvider
            .GetAuthenticationStateAsync()
            .ConfigureAwait(false);

        using IServiceScope scope = _scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<WebCurrentUser>().SetPrincipal(state.User);

        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
