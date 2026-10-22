using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Addresses.AddAddress;
using BSX.Modules.Crm.Application.Addresses.RemoveAddress;
using BSX.Modules.Crm.Application.Addresses.SetDefaultAddress;
using BSX.Modules.Crm.Application.Addresses.UpdateAddress;
using BSX.Modules.Crm.Application.Contacts.AddContact;
using BSX.Modules.Crm.Application.Contacts.RemoveContact;
using BSX.Modules.Crm.Application.Contacts.SetPrimaryContact;
using BSX.Modules.Crm.Application.Contacts.UpdateContact;
using BSX.Modules.Crm.Application.Customers.ActivateCustomer;
using BSX.Modules.Crm.Application.Customers.AssignOrganization;
using BSX.Modules.Crm.Application.Customers.ClearOrganization;
using BSX.Modules.Crm.Application.Customers.CreateCustomer;
using BSX.Modules.Crm.Application.Customers.DeactivateCustomer;
using BSX.Modules.Crm.Application.Customers.GetCustomerById;
using BSX.Modules.Crm.Application.Customers.ListCustomers;
using BSX.Modules.Crm.Application.Customers.UpdateCustomer;
using BSX.Modules.Crm.Application.Dtos;
using BSX.Modules.Crm.Application.Organizations.CreateOrganization;
using BSX.Modules.Crm.Application.Organizations.GetOrganizationById;
using BSX.Modules.Crm.Application.Organizations.ListOrganizations;
using BSX.Modules.Crm.Application.Organizations.UpdateOrganization;
using BSX.Modules.Crm.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BSX.Modules.Crm.Endpoints;

/// <summary>Maps the CRM HTTP endpoints (thin transport adapters over the CQRS handlers).</summary>
public static class CrmEndpointRouteBuilderExtensions
{
    /// <summary>Maps all CRM endpoints under <c>/crm</c>. Authentication is required by default.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapCrmEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapGroup("/crm")
            .WithTags("Crm")
            .RequireAuthorization()
            .AddEndpointFilter<CrmConcurrencyConflictFilter>();

        MapCustomers(group);
        MapContacts(group);
        MapAddresses(group);
        MapOrganizations(group);

        return endpoints;
    }

    private static void MapCustomers(RouteGroupBuilder group)
    {
        group.MapGet("/customers", async (
            ISender sender,
            CancellationToken cancellationToken,
            int page = 1,
            int size = 20,
            string? search = null) =>
            CrmResults.From(await sender.SendAsync(new ListCustomersQuery(page, size, search), cancellationToken)));

        group.MapGet("/customers/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            CrmResults.From(await sender.SendAsync(new GetCustomerByIdQuery(id), cancellationToken)));

        group.MapPost("/customers", async (
            CreateCustomerRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            CrmResults.FromCreated(
                await sender.SendAsync(
                    new CreateCustomerCommand(request.Code, request.Name, request.TaxIdentifier),
                    cancellationToken),
                "/crm/customers"));

        group.MapPut("/customers/{id:guid}", async (
            Guid id,
            UpdateCustomerRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            CrmResults.From(await sender.SendAsync(
                new UpdateCustomerCommand(id, request.Name, request.TaxIdentifier),
                cancellationToken)));

        group.MapPost("/customers/{id:guid}/activate", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
            CrmResults.From(await sender.SendAsync(new ActivateCustomerCommand(id), cancellationToken)));

        group.MapPost("/customers/{id:guid}/deactivate", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
            CrmResults.From(await sender.SendAsync(new DeactivateCustomerCommand(id), cancellationToken)));

        group.MapPost("/customers/{id:guid}/organization", async (
            Guid id,
            AssignOrganizationRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            CrmResults.From(await sender.SendAsync(
                new AssignOrganizationCommand(id, request.OrganizationId),
                cancellationToken)));

        group.MapDelete("/customers/{id:guid}/organization", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
            CrmResults.From(await sender.SendAsync(new ClearOrganizationCommand(id), cancellationToken)));
    }

    private static void MapContacts(RouteGroupBuilder group)
    {
        group.MapPost("/customers/{id:guid}/contacts", async (
            Guid id,
            AddContactRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            CrmResults.From(await sender.SendAsync(
                new AddContactCommand(id, request.Name, request.Email, request.PhoneNumber, request.IsPrimary),
                cancellationToken)));

        group.MapPut("/customers/{id:guid}/contacts/{contactId:guid}", async (
            Guid id,
            Guid contactId,
            UpdateContactRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            CrmResults.From(await sender.SendAsync(
                new UpdateContactCommand(id, contactId, request.Name, request.Email, request.PhoneNumber),
                cancellationToken)));

        group.MapDelete("/customers/{id:guid}/contacts/{contactId:guid}", async (
            Guid id,
            Guid contactId,
            ISender sender,
            CancellationToken cancellationToken) =>
            CrmResults.From(await sender.SendAsync(new RemoveContactCommand(id, contactId), cancellationToken)));

        group.MapPost("/customers/{id:guid}/contacts/{contactId:guid}/primary", async (
            Guid id,
            Guid contactId,
            ISender sender,
            CancellationToken cancellationToken) =>
            CrmResults.From(await sender.SendAsync(new SetPrimaryContactCommand(id, contactId), cancellationToken)));
    }

    private static void MapAddresses(RouteGroupBuilder group)
    {
        group.MapPost("/customers/{id:guid}/addresses", async (
            Guid id,
            AddAddressRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (!TryParseAddressType(request.Type, out AddressType type))
            {
                return InvalidAddressType();
            }

            return CrmResults.From(await sender.SendAsync(
                new AddAddressCommand(id, ToAddressInput(request), type),
                cancellationToken));
        });

        group.MapPut("/customers/{id:guid}/addresses/{addressId:guid}", async (
            Guid id,
            Guid addressId,
            UpdateAddressRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (!TryParseAddressType(request.Type, out AddressType type))
            {
                return InvalidAddressType();
            }

            return CrmResults.From(await sender.SendAsync(
                new UpdateAddressCommand(id, addressId, ToAddressInput(request), type),
                cancellationToken));
        });

        group.MapDelete("/customers/{id:guid}/addresses/{addressId:guid}", async (
            Guid id,
            Guid addressId,
            ISender sender,
            CancellationToken cancellationToken) =>
            CrmResults.From(await sender.SendAsync(new RemoveAddressCommand(id, addressId), cancellationToken)));

        group.MapPost("/customers/{id:guid}/addresses/{addressId:guid}/default", async (
            Guid id,
            Guid addressId,
            ISender sender,
            CancellationToken cancellationToken) =>
            CrmResults.From(await sender.SendAsync(new SetDefaultAddressCommand(id, addressId), cancellationToken)));
    }

    private static void MapOrganizations(RouteGroupBuilder group)
    {
        group.MapGet("/organizations", async (
            ISender sender,
            CancellationToken cancellationToken,
            int page = 1,
            int size = 20,
            string? search = null) =>
            CrmResults.From(await sender.SendAsync(new ListOrganizationsQuery(page, size, search), cancellationToken)));

        group.MapGet("/organizations/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            CrmResults.From(await sender.SendAsync(new GetOrganizationByIdQuery(id), cancellationToken)));

        group.MapPost("/organizations", async (
            CreateOrganizationRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            CrmResults.FromCreated(
                await sender.SendAsync(
                    new CreateOrganizationCommand(request.Name, request.TaxIdentifier, ToAddressInput(request.Address)),
                    cancellationToken),
                "/crm/organizations"));

        group.MapPut("/organizations/{id:guid}", async (
            Guid id,
            UpdateOrganizationRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            CrmResults.From(await sender.SendAsync(
                new UpdateOrganizationCommand(id, request.Name, request.TaxIdentifier, ToAddressInput(request.Address)),
                cancellationToken)));
    }

    private static bool TryParseAddressType(string? value, out AddressType type)
    {
        type = default;
        return !string.IsNullOrWhiteSpace(value)
            && Enum.TryParse(value, ignoreCase: true, out type)
            && Enum.IsDefined(type);
    }

    private static IResult InvalidAddressType()
        => TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "CRM.Address.TypeInvalid",
            detail: "The address type is invalid. Expected Billing, Shipping or Other.");

    private static AddressInput? ToAddressInput(AddressRequest? request)
        => request is null
            ? null
            : new AddressInput(
                request.Line1,
                request.Line2,
                request.City,
                request.Region,
                request.PostalCode,
                request.Country);

    private static AddressInput ToAddressInput(AddAddressRequest request)
        => new(request.Line1, request.Line2, request.City, request.Region, request.PostalCode, request.Country);

    private static AddressInput ToAddressInput(UpdateAddressRequest request)
        => new(request.Line1, request.Line2, request.City, request.Region, request.PostalCode, request.Country);

    /// <summary>Create customer request.</summary>
    public sealed record CreateCustomerRequest(string Code, string Name, string? TaxIdentifier);

    /// <summary>Update customer request.</summary>
    public sealed record UpdateCustomerRequest(string Name, string? TaxIdentifier);

    /// <summary>Assign organization request.</summary>
    public sealed record AssignOrganizationRequest(Guid OrganizationId);

    /// <summary>Add contact request.</summary>
    public sealed record AddContactRequest(string Name, string? Email, string? PhoneNumber, bool IsPrimary = false);

    /// <summary>Update contact request.</summary>
    public sealed record UpdateContactRequest(string Name, string? Email, string? PhoneNumber);

    /// <summary>An address supplied by a caller.</summary>
    public sealed record AddressRequest(
        string Line1,
        string? Line2,
        string City,
        string? Region,
        string? PostalCode,
        string Country);

    /// <summary>Add address request.</summary>
    public sealed record AddAddressRequest(
        string Line1,
        string? Line2,
        string City,
        string? Region,
        string? PostalCode,
        string Country,
        string Type);

    /// <summary>Update address request.</summary>
    public sealed record UpdateAddressRequest(
        string Line1,
        string? Line2,
        string City,
        string? Region,
        string? PostalCode,
        string Country,
        string Type);

    /// <summary>Create organization request.</summary>
    public sealed record CreateOrganizationRequest(string Name, string? TaxIdentifier, AddressRequest? Address);

    /// <summary>Update organization request.</summary>
    public sealed record UpdateOrganizationRequest(string Name, string? TaxIdentifier, AddressRequest? Address);
}
