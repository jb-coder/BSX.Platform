using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.Organizations.UpdateOrganization;

/// <summary>Handles <see cref="UpdateOrganizationCommand"/>.</summary>
public sealed class UpdateOrganizationCommandHandler : ICommandHandler<UpdateOrganizationCommand>
{
    private readonly IOrganizationRepository _organizations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CrmTenantResolver _tenantResolver;

    /// <summary>Initializes a new instance of the <see cref="UpdateOrganizationCommandHandler"/> class.</summary>
    public UpdateOrganizationCommandHandler(
        IOrganizationRepository organizations,
        IUnitOfWork unitOfWork,
        CrmTenantResolver tenantResolver)
    {
        _organizations = organizations;
        _unitOfWork = unitOfWork;
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(UpdateOrganizationCommand request, CancellationToken cancellationToken = default)
    {
        if (request.OrganizationId == Guid.Empty)
        {
            return Result.Failure(CrmApplicationErrors.InvalidOrganizationId);
        }

        Result<TenantId> tenant = _tenantResolver.Resolve();
        if (tenant.IsFailure)
        {
            return Result.Failure(tenant.Error);
        }

        Result<CustomerName> name = CustomerName.Create(request.Name);
        if (name.IsFailure)
        {
            return Result.Failure(name.Error);
        }

        TaxIdentifier? taxIdentifier = null;
        if (!string.IsNullOrWhiteSpace(request.TaxIdentifier))
        {
            Result<TaxIdentifier> tax = TaxIdentifier.Create(request.TaxIdentifier);
            if (tax.IsFailure)
            {
                return Result.Failure(tax.Error);
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
                return Result.Failure(addressResult.Error);
            }

            address = addressResult.Value;
        }

        Organization? organization = await _organizations
            .GetByIdAsync(tenant.Value, OrganizationId.From(request.OrganizationId), cancellationToken)
            .ConfigureAwait(false);

        if (organization is null)
        {
            return Result.Failure(CrmApplicationErrors.OrganizationNotFound);
        }

        Result renameResult = organization.Rename(name.Value);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        Result taxResult = organization.ChangeTaxIdentifier(taxIdentifier);
        if (taxResult.IsFailure)
        {
            return taxResult;
        }

        Result addressChange = organization.ChangeAddress(address);
        if (addressChange.IsFailure)
        {
            return addressChange;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
