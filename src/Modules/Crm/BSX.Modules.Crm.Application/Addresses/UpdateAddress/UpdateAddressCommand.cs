using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Dtos;
using BSX.Modules.Crm.Domain.Enums;

namespace BSX.Modules.Crm.Application.Addresses.UpdateAddress;

/// <summary>Updates a customer address value and purpose.</summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="AddressId">The address identifier.</param>
/// <param name="Address">The new address input.</param>
/// <param name="Type">The new address purpose.</param>
[RequirePermission("crm.customers.update")]
public sealed record UpdateAddressCommand(
    Guid CustomerId,
    Guid AddressId,
    AddressInput Address,
    AddressType Type) : ICommand;
