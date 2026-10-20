using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Enums;
using BSX.Modules.Crm.Domain.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.UnitTests.Customers;

public sealed class CustomerAddressTests
{
    [Fact]
    public void Should_MakeFirstAddressDefault_WhenAdded()
    {
        Customer customer = TestData.CreateCustomer();
        CustomerAddressId addressId = CustomerAddressId.New();

        var result = customer.AddAddress(addressId, TestData.Address(), AddressType.Billing);

        result.IsSuccess.Should().BeTrue();
        customer.Addresses.Should().ContainSingle();
        customer.Addresses.Single().Id.Should().Be(addressId);
        customer.Addresses.Single().IsDefault.Should().BeTrue();
        customer.Addresses.Single().Type.Should().Be(AddressType.Billing);
    }

    [Fact]
    public void Should_KeepSingleDefaultAddress_WhenAddingAnotherDefault()
    {
        Customer customer = TestData.CreateCustomer();
        customer.AddAddress(CustomerAddressId.New(), TestData.Address("1 Main St"), AddressType.Billing);
        CustomerAddressId secondId = CustomerAddressId.New();

        customer.AddAddress(secondId, TestData.Address("2 Oak St"), AddressType.Shipping, isDefault: true);

        customer.Addresses.Single(address => address.IsDefault).Id.Should().Be(secondId);
    }

    [Fact]
    public void Should_AllowSameAddress_WhenTypeDiffers()
    {
        Customer customer = TestData.CreateCustomer();
        Address address = TestData.Address();
        customer.AddAddress(CustomerAddressId.New(), address, AddressType.Billing);

        var result = customer.AddAddress(CustomerAddressId.New(), address, AddressType.Shipping);

        result.IsSuccess.Should().BeTrue();
        customer.Addresses.Should().HaveCount(2);
    }

    [Fact]
    public void Should_Fail_WhenSameAddressAndTypeIsDuplicated()
    {
        Customer customer = TestData.CreateCustomer();
        Address address = TestData.Address();
        customer.AddAddress(CustomerAddressId.New(), address, AddressType.Billing);

        var result = customer.AddAddress(CustomerAddressId.New(), address, AddressType.Billing);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.AddressDuplicate);
    }

    [Fact]
    public void Should_PromoteRemainingAddress_WhenDefaultRemoved()
    {
        Customer customer = TestData.CreateCustomer();
        CustomerAddressId firstId = CustomerAddressId.New();
        CustomerAddressId secondId = CustomerAddressId.New();
        customer.AddAddress(firstId, TestData.Address("1 Main St"), AddressType.Billing);
        customer.AddAddress(secondId, TestData.Address("2 Oak St"), AddressType.Shipping, isDefault: true);

        customer.RemoveAddress(secondId).IsSuccess.Should().BeTrue();

        customer.Addresses.Should().ContainSingle();
        customer.Addresses.Single().Id.Should().Be(firstId);
        customer.Addresses.Single().IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenRemovingUnknownAddress()
    {
        Customer customer = TestData.CreateCustomer();

        var result = customer.RemoveAddress(CustomerAddressId.New());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.AddressNotFound);
    }

    [Fact]
    public void Should_SetDefaultAddress_WhenRequested()
    {
        Customer customer = TestData.CreateCustomer();
        CustomerAddressId firstId = CustomerAddressId.New();
        CustomerAddressId secondId = CustomerAddressId.New();
        customer.AddAddress(firstId, TestData.Address("1 Main St"), AddressType.Billing);
        customer.AddAddress(secondId, TestData.Address("2 Oak St"), AddressType.Shipping);

        customer.SetDefaultAddress(secondId).IsSuccess.Should().BeTrue();

        customer.Addresses.Single(address => address.Id == secondId).IsDefault.Should().BeTrue();
        customer.Addresses.Single(address => address.Id == firstId).IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Should_ChangeAddressType_WhenRequested()
    {
        Customer customer = TestData.CreateCustomer();
        CustomerAddressId addressId = CustomerAddressId.New();
        customer.AddAddress(addressId, TestData.Address(), AddressType.Billing);

        customer.ChangeAddressType(addressId, AddressType.Shipping).IsSuccess.Should().BeTrue();

        customer.Addresses.Single().Type.Should().Be(AddressType.Shipping);
    }

    [Fact]
    public void Should_Fail_WhenChangingAddressTypeToDuplicate()
    {
        Customer customer = TestData.CreateCustomer();
        Address address = TestData.Address();
        customer.AddAddress(CustomerAddressId.New(), address, AddressType.Billing);
        CustomerAddressId shippingId = CustomerAddressId.New();
        customer.AddAddress(shippingId, address, AddressType.Shipping);

        var result = customer.ChangeAddressType(shippingId, AddressType.Billing);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.AddressDuplicate);
    }

    [Fact]
    public void Should_Fail_WhenChangingUnknownAddressType()
    {
        Customer customer = TestData.CreateCustomer();

        var result = customer.ChangeAddressType(CustomerAddressId.New(), AddressType.Shipping);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.AddressNotFound);
    }

    [Fact]
    public void Should_EnforceAddressLimit()
    {
        Customer customer = TestData.CreateCustomer();

        for (int index = 0; index < Customer.MaxAddresses; index++)
        {
            customer.AddAddress(
                CustomerAddressId.New(),
                TestData.Address($"{index + 1} Main St"),
                AddressType.Other).IsSuccess.Should().BeTrue();
        }

        var result = customer.AddAddress(
            CustomerAddressId.New(),
            TestData.Address("Overflow St"),
            AddressType.Other);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.AddressLimitReached);
    }
}
