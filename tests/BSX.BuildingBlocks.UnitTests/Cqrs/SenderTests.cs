using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.DependencyInjection;
using BSX.BuildingBlocks.Persistence;
using BSX.BuildingBlocks.UnitTests.Fixtures;
using BSX.SharedKernel.Results;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.BuildingBlocks.UnitTests.Cqrs;

public sealed class SenderTests
{
    [Fact]
    public async Task Should_DispatchQuery_ToItsHandler()
    {
        ISender sender = CreateSender();

        Result<int> result = await sender.SendAsync(new PingQuery(21));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task Should_DispatchCommand_ReturningValue()
    {
        ISender sender = CreateSender();

        Result<Guid> result = await sender.SendAsync(new CreateThingCommand("thing"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_ReturnHandlerFailure_WithoutThrowing()
    {
        ISender sender = CreateSender();

        Result result = await sender.SendAsync(new FailingCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Should_ShortCircuitPipeline_WhenValidationFails()
    {
        ValidatedCommandHandler.Reset();
        ISender sender = CreateSender();

        Result result = await sender.SendAsync(new ValidatedCommand(string.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        ValidatedCommandHandler.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_InvokeHandler_WhenValidationPasses()
    {
        ValidatedCommandHandler.Reset();
        ISender sender = CreateSender();

        Result result = await sender.SendAsync(new ValidatedCommand("valid"));

        result.IsSuccess.Should().BeTrue();
        ValidatedCommandHandler.InvocationCount.Should().Be(1);
    }

    private static ISender CreateSender()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddBuildingBlocks(typeof(SenderTests).Assembly);
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }
}
