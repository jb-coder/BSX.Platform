using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Sessions.RevokeSession;

/// <summary>Revokes one of the current user's sessions.</summary>
/// <param name="SessionId">The session identifier.</param>
public sealed record RevokeSessionCommand(Guid SessionId) : ICommand;
