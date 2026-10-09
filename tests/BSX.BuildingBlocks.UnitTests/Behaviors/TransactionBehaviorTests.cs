using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.DependencyInjection;
using BSX.BuildingBlocks.Persistence;
using BSX.BuildingBlocks.UnitTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.BuildingBlocks.UnitTests.Behaviors;

public sealed class TransactionBehaviorTests
{
    [Fact]
    public async Task Should_SaveAndDispatch_WhenCommandSucceeds()
    {
        (ISender sender, FakeUnitOfWork unitOfWork) = CreateSender();

        var result = await sender.SendAsync(new CreateThingCommand("thing"));

        result.IsSuccess.Should().BeTrue();
        unitOfWork.SaveAndDispatchCount.Should().Be(1);
        unitOfWork.LastTransaction!.Committed.Should().BeTrue();
        unitOfWork.LastTransaction.RolledBack.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Rollback_WhenCommandFails()
    {
        (ISender sender, FakeUnitOfWork unitOfWork) = CreateSender();

        var result = await sender.SendAsync(new FailingCommand());

        result.IsFailure.Should().BeTrue();
        unitOfWork.SaveAndDispatchCount.Should().Be(0);
        unitOfWork.LastTransaction!.RolledBack.Should().BeTrue();
    }

    [Fact]
    public async Task Should_NotOpenTransaction_ForQuery()
    {
        (ISender sender, FakeUnitOfWork unitOfWork) = CreateSender();

        var result = await sender.SendAsync(new PingQuery(21));

        result.IsSuccess.Should().BeTrue();
        unitOfWork.LastTransaction.Should().BeNull();
        unitOfWork.SaveAndDispatchCount.Should().Be(0);
    }

    private static (ISender Sender, FakeUnitOfWork UnitOfWork) CreateSender()
    {
        var unitOfWork = new FakeUnitOfWork();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddBuildingBlocks(typeof(TransactionBehaviorTests).Assembly);

        return (services.BuildServiceProvider().GetRequiredService<ISender>(), unitOfWork);
    }
}
