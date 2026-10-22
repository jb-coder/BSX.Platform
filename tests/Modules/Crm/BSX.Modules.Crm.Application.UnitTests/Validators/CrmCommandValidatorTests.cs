using BSX.Modules.Crm.Application.Addresses.AddAddress;
using BSX.Modules.Crm.Application.Common;
using BSX.Modules.Crm.Application.Contacts.AddContact;
using BSX.Modules.Crm.Application.Customers.AssignOrganization;
using BSX.Modules.Crm.Application.Customers.CreateCustomer;
using BSX.Modules.Crm.Application.Customers.UpdateCustomer;
using BSX.Modules.Crm.Application.Organizations.CreateOrganization;
using BSX.Modules.Crm.Application.Organizations.UpdateOrganization;
namespace BSX.Modules.Crm.Application.UnitTests.Validators;

public sealed class CrmCommandValidatorTests
{
    [Fact]
    public async Task CreateCustomer_Should_BeValid_WhenValid()
    {
        var result = await new CreateCustomerCommandValidator()
            .ValidateAsync(new CreateCustomerCommand("CUST-001", "Acme", "TAX-1"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Acme")]
    [InlineData("CUST-001", "")]
    [InlineData("   ", "Acme")]
    [InlineData("CUST-001", "   ")]
    public async Task CreateCustomer_Should_BeInvalid_WhenRequiredFieldsMissing(string code, string name)
    {
        var result = await new CreateCustomerCommandValidator()
            .ValidateAsync(new CreateCustomerCommand(code, name, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCustomer_Should_BeInvalid_WhenIdIsEmpty()
    {
        var result = await new UpdateCustomerCommandValidator()
            .ValidateAsync(new UpdateCustomerCommand(Guid.Empty, "Acme", null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AssignOrganization_Should_BeInvalid_WhenOrganizationIdIsEmpty()
    {
        var result = await new AssignOrganizationCommandValidator()
            .ValidateAsync(new AssignOrganizationCommand(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AddContact_Should_BeInvalid_WhenEmailIsMalformed()
    {
        var result = await new AddContactCommandValidator()
            .ValidateAsync(new AddContactCommand(Guid.NewGuid(), "Jane", "not-an-email", null, false));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AddContact_Should_BeValid_WithoutReachabilityChannel_AsThatIsADomainRule()
    {
        var result = await new AddContactCommandValidator()
            .ValidateAsync(new AddContactCommand(Guid.NewGuid(), "Jane", null, null, false));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AddAddress_Should_BeValid_WhenAddressIsValid()
    {
        var result = await new AddAddressCommandValidator()
            .ValidateAsync(new AddAddressCommand(Guid.NewGuid(), TestData.AddressInput(), CrmAddressType.Billing));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AddAddress_Should_BeInvalid_WhenCityIsMissing()
    {
        var result = await new AddAddressCommandValidator()
            .ValidateAsync(new AddAddressCommand(
                Guid.NewGuid(),
                TestData.AddressInput(city: " "),
                CrmAddressType.Billing));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AddAddress_Should_BeInvalid_WhenTypeIsUndefined()
    {
        var result = await new AddAddressCommandValidator()
            .ValidateAsync(new AddAddressCommand(
                Guid.NewGuid(),
                TestData.AddressInput(),
                (CrmAddressType)999));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOrganization_Should_BeInvalid_WhenAddressIsMalformed()
    {
        var result = await new CreateOrganizationCommandValidator()
            .ValidateAsync(new CreateOrganizationCommand(
                "Holding Group",
                null,
                TestData.AddressInput(line1: " ")));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOrganization_Should_BeValid_WithoutAddress()
    {
        var result = await new CreateOrganizationCommandValidator()
            .ValidateAsync(new CreateOrganizationCommand("Holding Group", null, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateOrganization_Should_BeInvalid_WhenIdIsEmpty()
    {
        var result = await new UpdateOrganizationCommandValidator()
            .ValidateAsync(new UpdateOrganizationCommand(Guid.Empty, "Group", null, null));

        result.IsValid.Should().BeFalse();
    }
}
