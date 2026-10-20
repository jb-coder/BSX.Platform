namespace BSX.Modules.Crm.Application.Dtos;

/// <summary>
/// An address supplied by a caller. Converted to the domain <c>Address</c> value object by the
/// handler; the domain remains the authority on address validity.
/// </summary>
public sealed record AddressInput(
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string? PostalCode,
    string Country);
