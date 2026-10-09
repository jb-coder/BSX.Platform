namespace BSX.Modules.Identity.Application.Users.RegisterUser;

/// <summary>The result of registering a user.</summary>
/// <param name="UserId">The new user identifier.</param>
public sealed record RegisterUserResponse(Guid UserId);
