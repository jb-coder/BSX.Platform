using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.Organizations.CreateOrganization;

/// <summary>Handles <see cref="CreateOrganizationCommand"/>.</summary>
public sealed class CreateOrganizationCommandHandler : ICommandHandler<CreateOrganizationCommand, Guid>
{
    private readonly IOrganizationRepository _organizations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CrmTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="CreateOrganizationCommandHandler"/> class.</summary>
    public CreateOrganizationCommandHandler(
        IOrganizationRepository organizations,
        IUnitOfWork unitOfWork,
        CrmTenantResolver tenantResolver)
    {
        _organizations = organizations;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> HandleAsync(CreateOrganizationCommand request, CancellationToken cancellationToken = default)
    {
        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure<Guid>(tenant.Error);
        }

        Result<CustomerName> name = CustomerName.Create(request.Name);
        if (name.IsFailure)
        {
            return Result.Failure<Guid>(name.Error);
        }

        TaxIdentifier? taxIdentifier = null;
        if (!string.IsNullOrWhiteSpace(request.TaxIdentifier))
        {
            Result<TaxIdentifier> tax = TaxIdentifier.Create(request.TaxIdentifier);
            if (tax.IsFailure)
            {
                return Result.Failure<Guid>(tax.Error);
            }

            taxIdentifier = tax.Value;
        }

        Address? address = null;
        if (request.Address is { } input)
        {
            Result<Address> addressResult = Address.Create(
                input.Line1,
                input.Line2,
                input.City,
                input.Region,
                input.PostalCode,
                input.Country);

            if (addressResult.IsFailure)
            {
                return Result.Failure<Guid>(addressResult.Error);
            }

            address = addressResult.Value;
        }

        Result<Organization> created = Organization.Create(
            OrganizationId.New(),
            tenant.Value,
            name.Value,
            taxIdentifier,
            address);

        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        await _organizations.AddAsync(created.Value, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(created.Value.Id.Value);
    }
}
