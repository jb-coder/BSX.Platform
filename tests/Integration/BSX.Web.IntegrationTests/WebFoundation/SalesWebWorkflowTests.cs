using System.Security.Claims;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Customers.CreateCustomer;
using BSX.Modules.Crm.Application.Customers.ListCustomers;
using BSX.Modules.Crm.Application.Organizations.CreateOrganization;
using BSX.Modules.Crm.Application.Organizations.ListOrganizations;
using BSX.Modules.Sales.Application.Dtos;
using BSX.Modules.Sales.Application.Leads.CreateLead;
using BSX.Modules.Sales.Application.Leads.GetLeadById;
using BSX.Modules.Sales.Application.Leads.MarkLeadContacted;
using BSX.Modules.Sales.Application.Leads.QualifyLead;
using BSX.Modules.Sales.Application.Leads.UpdateLead;
using BSX.Modules.Sales.Application.Opportunities.AdvanceOpportunityStage;
using BSX.Modules.Sales.Application.Opportunities.CreateOpportunityFromLead;
using BSX.Modules.Sales.Application.Opportunities.GetOpportunityById;
using BSX.Modules.Sales.Application.Opportunities.MarkOpportunityLost;
using BSX.Modules.Sales.Application.Opportunities.MarkOpportunityWon;
using BSX.SharedKernel.Results;
using BSX.Web.Authentication;
using BSX.Web.Sales;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class SalesWebWorkflowTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public SalesWebWorkflowTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task LeadLifecycle_ShouldContactQualifyAndEnforceTerminalEditing()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = CreateSender(scope);
        Guid leadId = await CreateLeadAsync(sender);

        LeadDto created = (await sender.SendAsync(new GetLeadByIdQuery(leadId))).Value;
        created.Status.Should().Be("New");

        (await sender.SendAsync(new MarkLeadContactedCommand(leadId))).IsSuccess.Should().BeTrue();
        (await sender.SendAsync(new QualifyLeadCommand(leadId))).IsSuccess.Should().BeTrue();

        LeadDto qualified = (await sender.SendAsync(new GetLeadByIdQuery(leadId))).Value;
        qualified.Status.Should().Be("Qualified");

        Result notesOnly = await sender.SendAsync(new UpdateLeadCommand(
            leadId,
            qualified.Title,
            qualified.CustomerId,
            qualified.OrganizationId,
            "Qualified notes"));

        notesOnly.IsSuccess.Should().BeTrue();

        Result detailsChange = await sender.SendAsync(new UpdateLeadCommand(
            leadId,
            "Changed after qualification",
            qualified.CustomerId,
            qualified.OrganizationId,
            "Qualified notes"));

        detailsChange.IsFailure.Should().BeTrue();
        detailsChange.Error.Code.Should().Be("SALES.Lead.NotEditable");
    }

    [Fact]
    public async Task OpportunityLifecycle_ShouldAdvanceAndCloseAsWonAndLost()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = CreateSender(scope);
        Guid customerId = await CreateCustomerAsync(sender);
        Guid organizationId = await CreateOrganizationAsync(sender);
        Guid qualifiedLeadId = await CreateQualifiedLeadAsync(sender, customerId, organizationId);

        Result<Guid> wonOpportunity = await sender.SendAsync(new CreateOpportunityFromLeadCommand(
            qualifiedLeadId,
            customerId,
            "Won opportunity",
            12500.50m,
            "USD",
            40,
            DateOnly.FromDateTime(DateTime.Today.AddDays(30))));

        wonOpportunity.IsSuccess.Should().BeTrue();

        for (int index = 0; index < 3; index++)
        {
            (await sender.SendAsync(new AdvanceOpportunityStageCommand(wonOpportunity.Value)))
                .IsSuccess.Should().BeTrue();
        }

        (await sender.SendAsync(new MarkOpportunityWonCommand(wonOpportunity.Value))).IsSuccess.Should().BeTrue();

        OpportunityDto won = (await sender.SendAsync(new GetOpportunityByIdQuery(wonOpportunity.Value))).Value;
        won.Stage.Should().Be("Won");
        won.Probability.Should().Be(100);
        won.CustomerId.Should().Be(customerId);
        won.OrganizationId.Should().Be(organizationId);

        Guid secondLeadId = await CreateQualifiedLeadAsync(sender, customerId, organizationId: null);
        Result<Guid> lostOpportunity = await sender.SendAsync(new CreateOpportunityFromLeadCommand(
            secondLeadId,
            customerId,
            "Lost opportunity",
            1000m,
            "USD",
            25,
            DateOnly.FromDateTime(DateTime.Today.AddDays(20))));

        (await sender.SendAsync(new MarkOpportunityLostCommand(lostOpportunity.Value))).IsSuccess.Should().BeTrue();

        OpportunityDto lost = (await sender.SendAsync(new GetOpportunityByIdQuery(lostOpportunity.Value))).Value;
        lost.Stage.Should().Be("Lost");
        lost.Probability.Should().Be(0);
    }

    [Fact]
    public async Task CrmLookups_ShouldResolveReferencesAndRejectMissingOnes()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = CreateSender(scope);
        Guid customerId = await CreateCustomerAsync(sender);
        Guid organizationId = await CreateOrganizationAsync(sender);

        BSX.Modules.Crm.Application.Dtos.PagedResult<BSX.Modules.Crm.Application.Dtos.CustomerSummaryDto> customers =
            (await sender.SendAsync(new ListCustomersQuery(1, 10, "SalesWorkflow"))).Value;
        customers.Items.Should().Contain(customer => customer.Id == customerId);

        BSX.Modules.Crm.Application.Dtos.PagedResult<BSX.Modules.Crm.Application.Dtos.OrganizationDto> organizations =
            (await sender.SendAsync(new ListOrganizationsQuery(1, 10, "SalesWorkflow"))).Value;
        organizations.Items.Should().Contain(organization => organization.Id == organizationId);

        Guid leadId = await CreateLeadAsync(sender, customerId, organizationId);
        LeadDto lead = (await sender.SendAsync(new GetLeadByIdQuery(leadId))).Value;
        lead.CustomerId.Should().Be(customerId);
        lead.OrganizationId.Should().Be(organizationId);

        Result<Guid> invalidReference = await SalesWebResults.ExecuteAsync(() => sender.SendAsync(
            new CreateLeadCommand("Invalid CRM reference", Guid.NewGuid(), null, null)));

        invalidReference.IsFailure.Should().BeTrue();
        invalidReference.Error.Code.Should().Be("SALES.Customer.NotFound");
    }

    [Fact]
    public async Task LeadConcurrency_ShouldMapToStableConflictResult()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = CreateSender(scope);
        Guid leadId = await CreateLeadAsync(sender);

        Result result = await SalesWebResults.ExecuteAsync(() => sender.SendAsync(
            new UpdateLeadCommand(
                leadId,
                ForcedSalesConcurrencySaveChangesInterceptor.TriggerTitle,
                CustomerId: null,
                OrganizationId: null,
                Notes: null)));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SALES.ConcurrencyConflict");
    }

    private ISender CreateSender(IServiceScope scope)
    {
        var currentUser = scope.ServiceProvider.GetRequiredService<WebCurrentUser>();
        currentUser.SetPrincipal(CreatePrincipal());
        return scope.ServiceProvider.GetRequiredService<ISender>();
    }

    private async Task<Guid> CreateLeadAsync(ISender sender, Guid? customerId = null, Guid? organizationId = null)
    {
        Result<Guid> created = await sender.SendAsync(new CreateLeadCommand(
            SalesTestData.NewTitle("SalesWorkflow"),
            customerId,
            organizationId,
            "Seeded workflow lead"));

        created.IsSuccess.Should().BeTrue();
        return created.Value;
    }

    private async Task<Guid> CreateQualifiedLeadAsync(ISender sender, Guid customerId, Guid? organizationId)
    {
        Guid id = await CreateLeadAsync(sender, customerId, organizationId);
        (await sender.SendAsync(new MarkLeadContactedCommand(id))).IsSuccess.Should().BeTrue();
        (await sender.SendAsync(new QualifyLeadCommand(id))).IsSuccess.Should().BeTrue();
        return id;
    }

    private static async Task<Guid> CreateCustomerAsync(ISender sender)
    {
        Result<Guid> result = await sender.SendAsync(new CreateCustomerCommand(
            CrmTestData.NewCode(),
            "SalesWorkflow Customer",
            TaxIdentifier: null));

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static async Task<Guid> CreateOrganizationAsync(ISender sender)
    {
        Result<Guid> result = await sender.SendAsync(new CreateOrganizationCommand(
            $"SalesWorkflow Org {Guid.NewGuid():N}",
            TaxIdentifier: null,
            Address: null));

        result.IsSuccess.Should().BeTrue();
        return result.Value;
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
