using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Dtos;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.Organizations.GetOrganizationById;

/// <summary>Handles <see cref="GetOrganizationByIdQuery"/>. Projects through the read model; loads no aggregate.</summary>
public sealed class GetOrganizationByIdQueryHandler : IQueryHandler<GetOrganizationByIdQuery, OrganizationDto>
{
    private readonly IOrganizationReadRepository _organizations;
    private readonly CrmTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="GetOrganizationByIdQueryHandler"/> class.</summary>
    public GetOrganizationByIdQueryHandler(
        IOrganizationReadRepository organizations,
        CrmTenantResolver tenantResolver)
    {
        _organizations = organizations;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<OrganizationDto>> HandleAsync(
        GetOrganizationByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request.OrganizationId == Guid.Empty)
        {
            return Result.Failure<OrganizationDto>(CrmApplicationErrors.InvalidOrganizationId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<OrganizationDto>(tenant.Error);
        }

        OrganizationDto? organization = await _organizations
            .GetByIdAsync(tenant.Value.Value, request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        return organization is null
            ? Result.Failure<OrganizationDto>(CrmApplicationErrors.OrganizationNotFound)
            : Result.Success(organization);
    }
}
