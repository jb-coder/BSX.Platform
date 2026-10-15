using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

[RequirePermission("crm.customers.read")]
public sealed record RequirePermissionCommand : ICommand;
