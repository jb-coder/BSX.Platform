using BSX.SharedKernel.Results;

namespace BSX.SharedKernel.UnitTests.Results;

public sealed class ResultTests
{
    [Fact]
    public void Should_BeSuccess_WhenCreatedAsSuccess()
    {
        Result result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Should_BeFailure_WhenCreatedWithError()
    {
        var error = Error.NotFound("Customer.NotFound", "Customer was not found.");

        Result result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Should_CarryValue_WhenGenericResultIsSuccess()
    {
        Result<int> result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Should_Throw_WhenAccessingValueOfFailedResult()
    {
        Result<int> result = Result.Failure<int>(Error.Failure("Failed", "Something failed."));

        Action act = () => _ = result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Should_Throw_WhenSuccessCarriesError()
    {
        Action act = () => new InvalidResult(true, Error.Failure("Failed", "Something failed."));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Should_Throw_WhenFailureDoesNotCarryError()
    {
        Action act = () => new InvalidResult(false, Error.None);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Should_ConvertValue_ToSuccessfulResult()
    {
        Result<string> result = "hello";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    private sealed class InvalidResult(bool isSuccess, Error error) : Result(isSuccess, error);
}
