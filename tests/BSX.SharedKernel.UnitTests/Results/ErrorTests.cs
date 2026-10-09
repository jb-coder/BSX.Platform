using BSX.SharedKernel.Results;

namespace BSX.SharedKernel.UnitTests.Results;

public sealed class ErrorTests
{
    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.Forbidden)]
    [InlineData(ErrorType.Unexpected)]
    [InlineData(ErrorType.Failure)]
    public void Should_AssignExpectedType_WhenUsingFactory(ErrorType expectedType)
    {
        Error error = expectedType switch
        {
            ErrorType.Validation => Error.Validation("code", "message"),
            ErrorType.NotFound => Error.NotFound("code", "message"),
            ErrorType.Conflict => Error.Conflict("code", "message"),
            ErrorType.Unauthorized => Error.Unauthorized("code", "message"),
            ErrorType.Forbidden => Error.Forbidden("code", "message"),
            ErrorType.Unexpected => Error.Unexpected("code", "message"),
            _ => Error.Failure("code", "message"),
        };

        error.Type.Should().Be(expectedType);
        error.Code.Should().Be("code");
        error.Message.Should().Be("message");
    }

    [Fact]
    public void Should_HaveEmptyValues_ForNone()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
    }
}
