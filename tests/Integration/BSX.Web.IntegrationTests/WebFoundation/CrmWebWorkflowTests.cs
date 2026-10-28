using System.Security.Claims;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Addresses.AddAddress;
using BSX.Modules.Crm.Application.Addresses.RemoveAddress;
using BSX.Modules.Crm.Application.Addresses.SetDefaultAddress;
using BSX.Modules.Crm.Application.Addresses.UpdateAddress;
using BSX.Modules.Crm.Application.Common;
using BSX.Modules.Crm.Application.Contacts.AddContact;
using BSX.Modules.Crm.Application.Contacts.RemoveContact;
using BSX.Modules.Crm.Application.Contacts.SetPrimaryContact;
using BSX.Modules.Crm.Application.Contacts.UpdateContact;
using BSX.Modules.Crm.Application.Customers.AssignOrganization;
using BSX.Modules.Crm.Application.Customers.ClearOrganization;
using BSX.Modules.Crm.Application.Customers.CreateCustomer;
using BSX.Modules.Crm.Application.Customers.GetCustomerById;
using BSX.Modules.Crm.Application.Customers.UpdateCustomer;
using BSX.Modules.Crm.Application.Dtos;
using BSX.Modules.Crm.Application.Organizations.CreateOrganization;
using BSX.Modules.Crm.Application.Organizations.GetOrganizationById;
using BSX.Modules.Crm.Application.Organizations.UpdateOrganization;
using BSX.SharedKernel.Results;
using BSX.Web.Authentication;
using BSX.Web.Crm;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class CrmWebWorkflowTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public CrmWebWorkflowTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task OrganizationLifecycle_ShouldCreateUpdateAndReadThroughTheWebApplicationContext()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = CreateSender(scope);
        string name = $"Org-{Guid.NewGuid():N}";

        Result<Guid> created = await sender.SendAsync(new CreateOrganizationCommand(
            name,
            "TAX-ORG-1",
            new AddressInput("5 Corporate Way", null, "Springfield", null, null, "US")));

        created.IsSuccess.Should().BeTrue();

        Result<OrganizationDto> detail = await sender.SendAsync(
            new GetOrganizationByIdQuery(created.Value));

        detail.IsSuccess.Should().BeTrue();
        detail.Value.Name.Should().Be(name);
        detail.Value.Address.Should().NotBeNull();
        detail.Value.TenantId.Should().Be(_factory.TenantId);

        Result updated = await sender.SendAsync(new UpdateOrganizationCommand(
            created.Value,
            "Renamed Holdings",
            "TAX-ORG-2",
            Address: null));

        updated.IsSuccess.Should().BeTrue();

        OrganizationDto refreshed = (await sender.SendAsync(
            new GetOrganizationByIdQuery(created.Value))).Value;
        refreshed.Name.Should().Be("Renamed Holdings");
        refreshed.TaxIdentifier.Should().Be("TAX-ORG-2");
        refreshed.Address.Should().BeNull();
    }

    [Fact]
    public async Task OrganizationAssignment_ShouldAssignAndClearTheCustomerRelationship()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = CreateSender(scope);
        Guid customerId = await CreateCustomerAsync(sender);
        Result<Guid> organization = await sender.SendAsync(new CreateOrganizationCommand(
            $"Org-{Guid.NewGuid():N}",
            TaxIdentifier: null,
            Address: null));

        Result assigned = await sender.SendAsync(new AssignOrganizationCommand(customerId, organization.Value));
        assigned.IsSuccess.Should().BeTrue();

        CustomerDto afterAssign = (await sender.SendAsync(new GetCustomerByIdQuery(customerId))).Value;
        afterAssign.OrganizationId.Should().Be(organization.Value);

        Result cleared = await sender.SendAsync(new ClearOrganizationCommand(customerId));
        cleared.IsSuccess.Should().BeTrue();

        CustomerDto afterClear = (await sender.SendAsync(new GetCustomerByIdQuery(customerId))).Value;
        afterClear.OrganizationId.Should().BeNull();
    }

    [Fact]
    public async Task ContactLifecycle_ShouldAddSetPrimaryUpdateAndRemove()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = CreateSender(scope);
        Guid customerId = await CreateCustomerAsync(sender);

        (await sender.SendAsync(new AddContactCommand(
            customerId, "Jane Doe", "jane@example.com", "+1 555 0100", IsPrimary: true))).IsSuccess.Should().BeTrue();
        (await sender.SendAsync(new AddContactCommand(
            customerId, "John Doe", "john@example.com", null, IsPrimary: false))).IsSuccess.Should().BeTrue();

        CustomerDto withContacts = (await sender.SendAsync(new GetCustomerByIdQuery(customerId))).Value;
        Guid firstId = withContacts.Contacts.Single(contact => contact.Email == "jane@example.com").Id;
        Guid secondId = withContacts.Contacts.Single(contact => contact.Email == "john@example.com").Id;
        withContacts.Contacts.Single(contact => contact.Id == firstId).IsPrimary.Should().BeTrue();

        (await sender.SendAsync(new SetPrimaryContactCommand(customerId, secondId))).IsSuccess.Should().BeTrue();
        (await sender.SendAsync(new UpdateContactCommand(
            customerId, firstId, "Jane Smith", "jane.smith@example.com", null))).IsSuccess.Should().BeTrue();
        (await sender.SendAsync(new RemoveContactCommand(customerId, secondId))).IsSuccess.Should().BeTrue();

        CustomerDto final = (await sender.SendAsync(new GetCustomerByIdQuery(customerId))).Value;
        ContactDto contact = final.Contacts.Should().ContainSingle().Subject;
        contact.Name.Should().Be("Jane Smith");
        contact.Email.Should().Be("jane.smith@example.com");
        contact.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task AddressLifecycle_ShouldValidateDuplicatesSetDefaultUpdateAndRemove()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = CreateSender(scope);
        Guid customerId = await CreateCustomerAsync(sender);
        var billing = new AddressInput("1 Main St", null, "Springfield", null, null, "US");

        (await sender.SendAsync(new AddAddressCommand(customerId, billing, CrmAddressType.Billing)))
            .IsSuccess.Should().BeTrue();

        Result duplicate = await sender.SendAsync(
            new AddAddressCommand(customerId, billing, CrmAddressType.Billing));
        duplicate.IsFailure.Should().BeTrue();
        duplicate.Error.Code.Should().Be("CRM.Customer.AddressDuplicate");

        (await sender.SendAsync(new AddAddressCommand(
            customerId,
            new AddressInput("2 Oak St", null, "Springfield", null, null, "US"),
            CrmAddressType.Shipping))).IsSuccess.Should().BeTrue();

        CustomerDto withAddresses = (await sender.SendAsync(new GetCustomerByIdQuery(customerId))).Value;
        Guid billingId = withAddresses.Addresses.Single(address => address.Type == "Billing").Id;
        Guid shippingId = withAddresses.Addresses.Single(address => address.Type == "Shipping").Id;

        (await sender.SendAsync(new SetDefaultAddressCommand(customerId, shippingId))).IsSuccess.Should().BeTrue();
        (await sender.SendAsync(new UpdateAddressCommand(
            customerId,
            billingId,
            new AddressInput("9 New St", null, "Springfield", null, null, "US"),
            CrmAddressType.Other))).IsSuccess.Should().BeTrue();
        (await sender.SendAsync(new RemoveAddressCommand(customerId, shippingId))).IsSuccess.Should().BeTrue();

        CustomerDto final = (await sender.SendAsync(new GetCustomerByIdQuery(customerId))).Value;
        AddressDto address = final.Addresses.Should().ContainSingle().Subject;
        address.Line1.Should().Be("9 New St");
        address.Type.Should().Be("Other");
        address.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task CustomerUpdate_ShouldSurfaceConcurrencyConflictThroughTheWebContext()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = CreateSender(scope);
        Guid customerId = await CreateCustomerAsync(sender);

        Result result = await CrmWebResults.ExecuteAsync(() => sender.SendAsync(new UpdateCustomerCommand(
            customerId,
            ForcedConcurrencySaveChangesInterceptor.TriggerName,
            TaxIdentifier: null)));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CRM.ConcurrencyConflict");
    }

    private ISender CreateSender(IServiceScope scope)
    {
        var currentUser = scope.ServiceProvider.GetRequiredService<WebCurrentUser>();
        currentUser.SetPrincipal(CreatePrincipal());
        return scope.ServiceProvider.GetRequiredService<ISender>();
    }

    private async Task<Guid> CreateCustomerAsync(ISender sender)
    {
        Result<Guid> created = await sender.SendAsync(new CreateCustomerCommand(
            CrmTestData.NewCode(),
            "Workflow Customer",
            TaxIdentifier: null));

        created.IsSuccess.Should().BeTrue();
        return created.Value;
    }

    private ClaimsPrincipal CreatePrincipal()
    {
        var claims = new List<Claim>
        {
            new(WebAuthDefaults.UserIdClaim, _factory.AdminUserId.ToString()),
            new(WebAuthDefaults.EmailClaim, IdentityApiFactory.AdminEmail),
            new(WebAuthDefaults.NameClaim, "Administrator"),
            new(WebAuthDefaults.TenantIdClaim, _factory.TenantId.ToString()),
        };

        claims.AddRange(IdentityApiFactory.AdminPermissions.Select(
            permission => new Claim(WebAuthDefaults.PermissionClaim, permission)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }
}
