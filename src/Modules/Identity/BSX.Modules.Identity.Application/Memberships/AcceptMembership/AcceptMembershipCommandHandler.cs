using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Memberships.AcceptMembership;

/// <summary>Handles <see cref="AcceptMembershipCommand"/>.</summary>
public sealed class AcceptMembershipCommandHandler : ICommandHandler<AcceptMembershipCommand>
{
    private readonly ITenantMembershipRepository _memberships;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="AcceptMembershipCommandHandler"/> class.</summary>
    public AcceptMembershipCommandHandler(
        ITenantMembershipRepository memberships,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _memberships = memberships;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(AcceptMembershipCommand request, CancellationToken cancellationToken = default)
    {
        if (request.MembershipId == Guid.Empty)
        {
            return Result.Failure(IdentityApplicationErrors.InvalidMembershipId);
        }

        TenantMembership? membership = await _memberships
            .GetByIdAsync(MembershipId.From(request.MembershipId), cancellationToken)
            .ConfigureAwait(false);

        if (membership is null)
        {
            return Result.Failure(IdentityApplicationErrors.MembershipNotFound);
        }

        Result result = membership.Accept(_timeProvider.GetUtcNow());

        if (result.IsFailure)
        {
            return result;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
