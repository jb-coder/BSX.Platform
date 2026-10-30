using System.Reflection;
using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.SharedKernel.Results;

namespace BSX.BuildingBlocks.Behaviors;

/// <summary>
/// Enforces authentication (fail-closed) and the <see cref="RequirePermissionAttribute"/> declared
/// on a request (ADR-012). Requests marked <see cref="AllowAnonymousAttribute"/> bypass the
/// authentication gate. Never throws: failures are returned as a failed <see cref="Result"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const string UnauthenticatedCode = "Authorization.Unauthenticated";
    private const string ForbiddenCode = "Authorization.Forbidden";
    private const string SessionExpiredCode = "Authorization.SessionExpired";

    private static readonly bool AllowsAnonymous = typeof(TRequest)
        .GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null;

    private static readonly string[] RequiredPermissions = typeof(TRequest)
        .GetCustomAttributes<RequirePermissionAttribute>(inherit: true)
        .Select(attribute => attribute.Permission)
        .ToArray();

    private readonly IPermissionChecker _permissionChecker;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthorizationRevalidator _revalidator;

    /// <summary>Initializes a new instance of the <see cref="AuthorizationBehavior{TRequest, TResponse}"/> class.</summary>
    /// <param name="permissionChecker">The permission checker.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="revalidator">The principal revalidator.</param>
    public AuthorizationBehavior(
        IPermissionChecker permissionChecker,
        ICurrentUser currentUser,
        IAuthorizationRevalidator revalidator)
    {
        _permissionChecker = permissionChecker;
        _currentUser = currentUser;
        _revalidator = revalidator;
    }

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (AllowsAnonymous)
        {
            return await next().ConfigureAwait(false);
        }

        if (!_currentUser.IsAuthenticated)
        {
            return Failure(Error.Unauthorized(UnauthenticatedCode, "Authentication is required."));
        }

        // Sensitive operations never trust a cached principal: the session, account and membership
        // are checked against the authoritative store before the permission gate (ADR-021).
        AuthorizationRevalidationResult revalidation = await _revalidator
            .RevalidateAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!revalidation.IsValid)
        {
            return Failure(Error.Unauthorized(SessionExpiredCode, "The session is no longer valid."));
        }

        foreach (string permission in RequiredPermissions)
        {
            bool granted = revalidation.IsAuthoritative
                ? revalidation.Permissions.Contains(permission, StringComparer.Ordinal)
                : await _permissionChecker.HasPermissionAsync(permission, cancellationToken).ConfigureAwait(false);

            if (!granted)
            {
                return Failure(Error.Forbidden(ForbiddenCode, $"The permission '{permission}' is required."));
            }
        }

        return await next().ConfigureAwait(false);
    }

    private static TResponse Failure(Error error)
    {
        Type responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            Type valueType = responseType.GetGenericArguments()[0];
            MethodInfo failureMethod = typeof(Result<>)
                .MakeGenericType(valueType)
                .GetMethod(nameof(Result<object>.Failure))!;

            return (TResponse)failureMethod.Invoke(null, [error])!;
        }

        throw new InvalidOperationException(
            $"The authorization behavior requires requests to return '{typeof(Result)}' or '{typeof(Result<>)}'.");
    }
}
