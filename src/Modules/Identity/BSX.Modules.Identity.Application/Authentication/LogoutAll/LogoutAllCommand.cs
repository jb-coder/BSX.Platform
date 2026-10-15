using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Authentication.LogoutAll;

/// <summary>Revokes all sessions of the current user.</summary>
public sealed record LogoutAllCommand : ICommand;
