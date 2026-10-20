using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Crm.Application.Addresses.RemoveAddress;

/// <summary>Removes an address from a customer.</summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="AddressId">The address identifier.</param>
[RequirePermission("crm.customers.update")]
public sealed record RemoveAddressCommand(Guid CustomerId, Guid AddressId) : ICommand;
