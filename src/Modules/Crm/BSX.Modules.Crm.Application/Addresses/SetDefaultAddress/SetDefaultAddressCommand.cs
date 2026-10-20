using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Crm.Application.Addresses.SetDefaultAddress;

/// <summary>Marks a customer address as the default address.</summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="AddressId">The address identifier.</param>
[RequirePermission("crm.customers.update")]
public sealed record SetDefaultAddressCommand(Guid CustomerId, Guid AddressId) : ICommand;
