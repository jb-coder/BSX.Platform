using System.Reflection;
using BSX.BuildingBlocks.Authorization;
using BSX.Modules.Sales.Application.Common;
using BSX.Modules.Sales.Application.Leads.CreateLead;
using BSX.Modules.Sales.Application.Leads.DisqualifyLead;
using BSX.Modules.Sales.Application.Leads.GetLeadById;
using BSX.Modules.Sales.Application.Leads.ListLeads;
using BSX.Modules.Sales.Application.Leads.MarkLeadContacted;
using BSX.Modules.Sales.Application.Leads.QualifyLead;
using BSX.Modules.Sales.Application.Leads.UpdateLead;
using BSX.Modules.Sales.Application.Opportunities.AdvanceOpportunityStage;
using BSX.Modules.Sales.Application.Opportunities.CreateOpportunityFromLead;
using BSX.Modules.Sales.Application.Opportunities.GetOpportunityById;
using BSX.Modules.Sales.Application.Opportunities.ListOpportunities;
using BSX.Modules.Sales.Application.Opportunities.MarkOpportunityLost;
using BSX.Modules.Sales.Application.Opportunities.MarkOpportunityWon;
using BSX.Modules.Sales.Application.Opportunities.UpdateOpportunity;
using BSX.Modules.Sales.Contracts.Permissions;

namespace BSX.Modules.Sales.Application.UnitTests.Permissions;

public sealed class SalesPermissionTests
{
    private static readonly IReadOnlyDictionary<Type, string> Expected = new Dictionary<Type, string>
    {
        [typeof(CreateLeadCommand)] = SalesPermissions.LeadsCreate,
        [typeof(UpdateLeadCommand)] = SalesPermissions.LeadsUpdate,
        [typeof(MarkLeadContactedCommand)] = SalesPermissions.LeadsUpdate,
        [typeof(QualifyLeadCommand)] = SalesPermissions.LeadsUpdate,
        [typeof(DisqualifyLeadCommand)] = SalesPermissions.LeadsUpdate,
        [typeof(GetLeadByIdQuery)] = SalesPermissions.LeadsRead,
        [typeof(ListLeadsQuery)] = SalesPermissions.LeadsRead,
        [typeof(CreateOpportunityFromLeadCommand)] = SalesPermissions.OpportunitiesCreate,
        [typeof(UpdateOpportunityCommand)] = SalesPermissions.OpportunitiesUpdate,
        [typeof(AdvanceOpportunityStageCommand)] = SalesPermissions.OpportunitiesUpdate,
        [typeof(MarkOpportunityWonCommand)] = SalesPermissions.OpportunitiesUpdate,
        [typeof(MarkOpportunityLostCommand)] = SalesPermissions.OpportunitiesUpdate,
        [typeof(GetOpportunityByIdQuery)] = SalesPermissions.OpportunitiesRead,
        [typeof(ListOpportunitiesQuery)] = SalesPermissions.OpportunitiesRead,
    };

    [Fact]
    public void EverySalesRequest_ShouldDeclareExactlyOnePermission()
    {
        foreach ((Type request, string expected) in Expected)
        {
            RequirePermissionAttribute[] attributes = request
                .GetCustomAttributes<RequirePermissionAttribute>(inherit: true)
                .ToArray();

            attributes.Should().ContainSingle($"{request.Name} must declare exactly one permission.");
            attributes[0].Permission.Should().Be(expected);
        }
    }

    [Fact]
    public void PermissionCodes_ShouldBeStableAndUnique()
    {
        string[] codes =
        [
            SalesPermissions.LeadsRead,
            SalesPermissions.LeadsCreate,
            SalesPermissions.LeadsUpdate,
            SalesPermissions.OpportunitiesRead,
            SalesPermissions.OpportunitiesCreate,
            SalesPermissions.OpportunitiesUpdate,
        ];

        codes.Should().OnlyHaveUniqueItems();
        codes.Should().OnlyContain(code => code.StartsWith("sales.", StringComparison.Ordinal));
        codes.Should().BeEquivalentTo(Expected.Values.Distinct());
    }

    [Fact]
    public void PermissionCatalog_ShouldDeclareEveryStableCode()
    {
        string[] codes =
        [
            SalesPermissions.LeadsRead,
            SalesPermissions.LeadsCreate,
            SalesPermissions.LeadsUpdate,
            SalesPermissions.OpportunitiesRead,
            SalesPermissions.OpportunitiesCreate,
            SalesPermissions.OpportunitiesUpdate,
        ];

        var catalog = new SalesPermissionCatalog();

        catalog.Permissions.Select(permission => permission.Code)
            .Should().BeEquivalentTo(codes);

        catalog.Permissions.Should().OnlyContain(permission => !string.IsNullOrWhiteSpace(permission.Description));
    }
}
