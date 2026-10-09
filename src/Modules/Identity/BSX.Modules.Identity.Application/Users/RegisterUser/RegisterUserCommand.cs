using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Users.RegisterUser;

/// <summary>Registers a new user.</summary>
/// <param name="Email">The email address.</param>
/// <param name="Name">The display name.</param>
/// <param name="Password">An optional initial password.</param>
public sealed record RegisterUserCommand(string Email, string Name, string? Password) : ICommand<RegisterUserResponse>;
