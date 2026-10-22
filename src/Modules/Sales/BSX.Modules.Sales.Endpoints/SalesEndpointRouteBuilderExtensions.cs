using BSX.BuildingBlocks.Cqrs;
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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BSX.Modules.Sales.Endpoints;

/// <summary>Maps the Sales HTTP endpoints (thin transport adapters over the CQRS handlers).</summary>
public static class SalesEndpointRouteBuilderExtensions
{
    /// <summary>Maps all Sales endpoints under <c>/sales</c>. Authentication is required by default.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapSalesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapGroup("/sales")
            .WithTags("Sales")
            .RequireAuthorization()
            .AddEndpointFilter<SalesConcurrencyConflictFilter>()
            .AddEndpointFilter<SalesCrmReferenceExceptionFilter>();

        MapLeads(group);
        MapOpportunities(group);

        return endpoints;
    }

    private static void MapLeads(RouteGroupBuilder group)
    {
        group.MapGet("/leads", async (
            ISender sender,
            CancellationToken cancellationToken,
            int page = 1,
            int size = 20,
            string? search = null) =>
            SalesResults.From(await sender.SendAsync(new ListLeadsQuery(page, size, search), cancellationToken)));

        group.MapGet("/leads/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            SalesResults.From(await sender.SendAsync(new GetLeadByIdQuery(id), cancellationToken)));

        group.MapPost("/leads", async (
            CreateLeadRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            SalesResults.FromCreated(
                await sender.SendAsync(
                    new CreateLeadCommand(request.Title, request.CustomerId, request.OrganizationId, request.Notes),
                    cancellationToken),
                "/sales/leads"));

        group.MapPut("/leads/{id:guid}", async (
            Guid id,
            UpdateLeadRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            SalesResults.From(await sender.SendAsync(
                new UpdateLeadCommand(id, request.Title, request.CustomerId, request.OrganizationId, request.Notes),
                cancellationToken)));

        group.MapPost("/leads/{id:guid}/contacted", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
            SalesResults.From(await sender.SendAsync(new MarkLeadContactedCommand(id), cancellationToken)));

        group.MapPost("/leads/{id:guid}/qualify", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
            SalesResults.From(await sender.SendAsync(new QualifyLeadCommand(id), cancellationToken)));

        group.MapPost("/leads/{id:guid}/disqualify", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
            SalesResults.From(await sender.SendAsync(new DisqualifyLeadCommand(id), cancellationToken)));
    }

    private static void MapOpportunities(RouteGroupBuilder group)
    {
        group.MapGet("/opportunities", async (
            ISender sender,
            CancellationToken cancellationToken,
            int page = 1,
            int size = 20,
            string? search = null) =>
            SalesResults.From(await sender.SendAsync(new ListOpportunitiesQuery(page, size, search), cancellationToken)));

        group.MapGet("/opportunities/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            SalesResults.From(await sender.SendAsync(new GetOpportunityByIdQuery(id), cancellationToken)));

        group.MapPost("/opportunities", async (
            CreateOpportunityRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            SalesResults.FromCreated(
                await sender.SendAsync(
                    new CreateOpportunityFromLeadCommand(
                        request.LeadId,
                        request.CustomerId,
                        request.Title,
                        request.Amount,
                        request.Currency,
                        request.Probability,
                        request.ExpectedCloseDate),
                    cancellationToken),
                "/sales/opportunities"));

        group.MapPut("/opportunities/{id:guid}", async (
            Guid id,
            UpdateOpportunityRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            SalesResults.From(await sender.SendAsync(
                new UpdateOpportunityCommand(
                    id,
                    request.CustomerId,
                    request.OrganizationId,
                    request.Title,
                    request.Amount,
                    request.Currency,
                    request.Probability,
                    request.ExpectedCloseDate),
                cancellationToken)));

        group.MapPost("/opportunities/{id:guid}/advance", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
            SalesResults.From(await sender.SendAsync(new AdvanceOpportunityStageCommand(id), cancellationToken)));

        group.MapPost("/opportunities/{id:guid}/won", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
            SalesResults.From(await sender.SendAsync(new MarkOpportunityWonCommand(id), cancellationToken)));

        group.MapPost("/opportunities/{id:guid}/lost", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
            SalesResults.From(await sender.SendAsync(new MarkOpportunityLostCommand(id), cancellationToken)));
    }

    /// <summary>Create lead request.</summary>
    public sealed record CreateLeadRequest(
        string Title,
        Guid? CustomerId,
        Guid? OrganizationId,
        string? Notes);

    /// <summary>Update lead request.</summary>
    public sealed record UpdateLeadRequest(
        string Title,
        Guid? CustomerId,
        Guid? OrganizationId,
        string? Notes);

    /// <summary>Create opportunity request.</summary>
    public sealed record CreateOpportunityRequest(
        Guid LeadId,
        Guid CustomerId,
        string Title,
        decimal Amount,
        string Currency,
        int Probability,
        DateOnly ExpectedCloseDate);

    /// <summary>Update opportunity request.</summary>
    public sealed record UpdateOpportunityRequest(
        Guid CustomerId,
        Guid? OrganizationId,
        string Title,
        decimal Amount,
        string Currency,
        int Probability,
        DateOnly ExpectedCloseDate);
}
