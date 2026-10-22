using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Common;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Modules.Crm.Application.Addresses.AddAddress;

/// <summary>Adds an address to a customer.</summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="Address">The address input.</param>
/// <param name="Type">The address purpose.</param>
[RequirePermission("crm.customers.update")]
public sealed record AddAddressCommand(Guid CustomerId, AddressInput Address, CrmAddressType Type) : ICommand;
