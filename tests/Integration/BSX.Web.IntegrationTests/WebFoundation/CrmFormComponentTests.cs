using System.ComponentModel.DataAnnotations;
using BSX.Web.Components.Pages.App.Crm;
using BSX.Web.Crm;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class CrmFormComponentTests
{
    private readonly ServiceProvider _services;

    public CrmFormComponentTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _services = services.BuildServiceProvider();
    }

    [Fact]
    public async Task OrganizationForm_ShouldRenderDetailsAndConditionalAddress()
    {
        string withoutAddress = await RenderAsync<OrganizationFormFields>(new Dictionary<string, object?>
        {
            [nameof(OrganizationFormFields.Model)] = new OrganizationFormModel
            {
                Name = "Acme Group",
                TaxIdentifier = "TAX-1",
            },
        });

        withoutAddress.Should().Contain("Organization name");
        withoutAddress.Should().Contain("Include a primary address");
        withoutAddress.Should().NotContain("Address line 1");

        var model = new OrganizationFormModel
        {
            Name = "Acme Group",
            IncludeAddress = true,
        };

        string withAddress = await RenderAsync<OrganizationFormFields>(new Dictionary<string, object?>
        {
            [nameof(OrganizationFormFields.Model)] = model,
        });

        withAddress.Should().Contain("for=\"organization-line1\"");
        withAddress.Should().Contain("for=\"organization-city\"");
        withAddress.Should().Contain("for=\"organization-country\"");
    }

    [Fact]
    public async Task ContactForm_ShouldShowPrimaryOnlyInAddMode()
    {
        string add = await RenderAsync<ContactFormFields>(new Dictionary<string, object?>
        {
            [nameof(ContactFormFields.Model)] = new ContactFormModel { Name = "Jane Doe" },
            [nameof(ContactFormFields.ShowPrimary)] = true,
        });

        add.Should().Contain("for=\"contact-name\"");
        add.Should().Contain("for=\"contact-email\"");
        add.Should().Contain("for=\"contact-phone\"");
        add.Should().Contain("Make this the primary contact");

        string edit = await RenderAsync<ContactFormFields>(new Dictionary<string, object?>
        {
            [nameof(ContactFormFields.Model)] = new ContactFormModel { Name = "Jane Doe" },
            [nameof(ContactFormFields.ShowPrimary)] = false,
        });

        edit.Should().NotContain("Make this the primary contact");
    }

    [Fact]
    public async Task AddressForm_ShouldRenderAllAddressTypesAndFields()
    {
        string html = await RenderAsync<AddressFormFields>(new Dictionary<string, object?>
        {
            [nameof(AddressFormFields.Model)] = new AddressFormModel(),
        });

        html.Should().Contain("Billing");
        html.Should().Contain("Shipping");
        html.Should().Contain("Other");
        html.Should().Contain("for=\"address-line1\"");
        html.Should().Contain("for=\"address-city\"");
        html.Should().Contain("for=\"address-country\"");
    }

    [Fact]
    public void ContactValidation_ShouldRequireReachabilityAndValidPhone()
    {
        var unreachable = new ContactFormModel { Name = "Jane Doe" };
        IReadOnlyList<ValidationResult> unreachableErrors = Validate(unreachable);
        unreachableErrors.Should().Contain(result =>
            result.MemberNames.Contains(nameof(ContactFormModel.Email))
            && result.MemberNames.Contains(nameof(ContactFormModel.PhoneNumber)));

        var shortPhone = new ContactFormModel { Name = "Jane Doe", PhoneNumber = "12" };
        IReadOnlyList<ValidationResult> phoneErrors = Validate(shortPhone);
        phoneErrors.Should().Contain(result => result.MemberNames.Contains(nameof(ContactFormModel.PhoneNumber)));

        var valid = new ContactFormModel { Name = "Jane Doe", Email = "jane@example.com" };
        Validate(valid).Should().BeEmpty();
    }

    [Fact]
    public void AddressValidation_ShouldRequireCoreFieldsAndKnownType()
    {
        var invalid = new AddressFormModel { Type = "Bogus" };
        IReadOnlyList<ValidationResult> errors = Validate(invalid);

        errors.Should().Contain(result => result.MemberNames.Contains(nameof(AddressFormModel.Type)));
        errors.Should().Contain(result => result.MemberNames.Contains(nameof(AddressFormModel.Line1)));
        errors.Should().Contain(result => result.MemberNames.Contains(nameof(AddressFormModel.City)));
        errors.Should().Contain(result => result.MemberNames.Contains(nameof(AddressFormModel.Country)));

        var valid = new AddressFormModel
        {
            Type = "Shipping",
            Line1 = "1 Main St",
            City = "Springfield",
            Country = "US",
        };

        Validate(valid).Should().BeEmpty();
        valid.ToAddressInput().Line1.Should().Be("1 Main St");
    }

    [Fact]
    public void OrganizationValidation_ShouldRequireAddressFieldsOnlyWhenIncluded()
    {
        var withoutAddress = new OrganizationFormModel { Name = "Acme Group" };
        Validate(withoutAddress).Should().BeEmpty();
        withoutAddress.ToAddressInput().Should().BeNull();

        var withAddress = new OrganizationFormModel
        {
            Name = "Acme Group",
            IncludeAddress = true,
        };

        IReadOnlyList<ValidationResult> errors = Validate(withAddress);
        errors.Should().Contain(result => result.MemberNames.Contains(nameof(OrganizationFormModel.AddressLine1)));
        errors.Should().Contain(result => result.MemberNames.Contains(nameof(OrganizationFormModel.City)));
        errors.Should().Contain(result => result.MemberNames.Contains(nameof(OrganizationFormModel.Country)));
    }

    private static IReadOnlyList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private async Task<string> RenderAsync<TComponent>(Dictionary<string, object?>? parameters = null)
        where TComponent : IComponent
    {
        ILoggerFactory loggerFactory = _services.GetRequiredService<ILoggerFactory>();
        await using var renderer = new HtmlRenderer(_services, loggerFactory);

        var hostParameters = new Dictionary<string, object?>
        {
            [nameof(FormHost.ComponentType)] = typeof(TComponent),
            [nameof(FormHost.ComponentParameters)] = parameters ?? new Dictionary<string, object?>(),
        };

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<FormHost>(
                ParameterView.FromDictionary(hostParameters));

            return root.ToHtmlString();
        });
    }

    private sealed class FormHost : ComponentBase
    {
        private readonly EditContext _editContext = new(new object());

        [Parameter, EditorRequired]
        public Type ComponentType { get; set; } = null!;

        [Parameter]
        public Dictionary<string, object?> ComponentParameters { get; set; } = [];

#pragma warning disable ASP0006 // Dynamic test host parameters require runtime sequence numbers.
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<EditContext>>(0);
            builder.AddAttribute(1, nameof(CascadingValue<EditContext>.Value), _editContext);
            builder.AddAttribute(2, nameof(CascadingValue<EditContext>.ChildContent), (RenderFragment)(child =>
            {
                child.OpenComponent(0, ComponentType);
                int sequence = 1;

                foreach ((string name, object? value) in ComponentParameters)
                {
                    child.AddAttribute(sequence++, name, value);
                }

                child.CloseComponent();
            }));
            builder.CloseComponent();
        }
#pragma warning restore ASP0006
    }
}
