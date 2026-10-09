using BSX.SharedKernel.Results;

namespace BSX.BuildingBlocks.Cqrs;

/// <summary>
/// Handles a command that does not return a value.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}
