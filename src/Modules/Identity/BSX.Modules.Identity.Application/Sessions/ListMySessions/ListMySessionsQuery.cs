using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;

namespace BSX.Modules.Identity.Application.Sessions.ListMySessions;

/// <summary>Lists the sessions of the current user.</summary>
public sealed record ListMySessionsQuery : IQuery<IReadOnlyList<SessionDto>>;
