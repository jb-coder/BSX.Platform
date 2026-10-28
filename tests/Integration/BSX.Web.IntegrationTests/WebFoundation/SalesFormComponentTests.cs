using System.ComponentModel.DataAnnotations;
using BSX.BuildingBlocks.Cqrs;
using BSX.Web.Components.Pages.App.Sales;
using BSX.Web.Sales;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class SalesFormComponentTests
{
    private readonly ServiceProvider _services;

    public SalesFormComponentTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISender, NoopSender>();
        _services = services.BuildServiceProvider();
    }

    [Fact]
    public async Task LeadForm_ShouldRenderDetailsAndNotes()
    {
        string html = await RenderAsync<LeadFormFields>(new Dictionary<string, object?>
        {
            [nameof(LeadFormFields.Model)] = new LeadFormModel { Title = "Renewal lead", Notes = "Call next week." },
            [nameof(LeadFormFields.CanReadCustomers)] = false,
            [nameof(LeadFormFields.CanReadOrganizations)] = false,
        });

        html.Should().Contain("for=\"lead-title\"");
        html.Should().Contain("for=\"lead-notes\"");
        html.Should().Contain("Up to 2,000 characters");
        html.Should().Contain("CRM read permission is required");
    }

    [Fact]
    public async Task LeadForm_ShouldLockDetailsForTerminalLead()
    {
        string html = await RenderAsync<LeadFormFields>(new Dictionary<string, object?>
        {
            [nameof(LeadFormFields.Model)] = new LeadFormModel { Title = "Qualified lead" },
            [nameof(LeadFormFields.DetailsEditable)] = false,
            [nameof(LeadFormFields.CanReadCustomers)] = false,
            [nameof(LeadFormFields.CanReadOrganizations)] = false,
        });

        html.Should().Contain("This lead is terminal. Only notes can be changed.");
        html.Should().Contain("disabled");
    }

    [Fact]
    public async Task OpportunityForm_ShouldRenderForecastInputs()
    {
        string html = await RenderAsync<OpportunityFormFields>(new Dictionary<string, object?>
        {
            [nameof(OpportunityFormFields.Model)] = new OpportunityFormModel
            {
                Title = "Enterprise renewal",
                Amount = 1000m,
                Probability = 25,
                Currency = "USD",
                ExpectedCloseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            },
            [nameof(OpportunityFormFields.CanReadCustomers)] = false,
            [nameof(OpportunityFormFields.CanReadOrganizations)] = false,
        });

        html.Should().Contain("for=\"opportunity-title\"");
        html.Should().Contain("for=\"opportunity-amount\"");
        html.Should().Contain("for=\"opportunity-currency\"");
        html.Should().Contain("for=\"opportunity-probability\"");
        html.Should().Contain("for=\"opportunity-close-date\"");
        html.Should().Contain("Weighted forecast");
        html.Should().Contain("250.00 USD");
    }

    [Fact]
    public async Task StageTimeline_ShouldExposeCurrentAndClosedStages()
    {
        string discovery = await RenderAsync<OpportunityStageTimeline>(new Dictionary<string, object?>
        {
            [nameof(OpportunityStageTimeline.Stage)] = "Discovery",
        });

        discovery.Should().Contain("aria-label=\"Opportunity stage timeline\"");
        discovery.Should().Contain("aria-current=\"step\"");
        discovery.Should().Contain("Discovery");

        string negotiation = await RenderAsync<OpportunityStageTimeline>(new Dictionary<string, object?>
        {
            [nameof(OpportunityStageTimeline.Stage)] = "Negotiation",
        });
        negotiation.Should().Contain("is-completed");
        negotiation.Should().Contain("Negotiation");

        string won = await RenderAsync<OpportunityStageTimeline>(new Dictionary<string, object?>
        {
            [nameof(OpportunityStageTimeline.Stage)] = "Won",
        });
        won.Should().Contain("is-won");

        string lost = await RenderAsync<OpportunityStageTimeline>(new Dictionary<string, object?>
        {
            [nameof(OpportunityStageTimeline.Stage)] = "Lost",
        });
        lost.Should().Contain("is-lost");
    }

    [Fact]
    public void OpportunityValidation_ShouldRequireCustomerAndAValidForecast()
    {
        var missingCustomer = new OpportunityFormModel
        {
            Title = "Opportunity",
            Amount = 100m,
            Currency = "USD",
            Probability = 50,
            ExpectedCloseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
        };

        Validate(missingCustomer).Should().Contain(result =>
            result.MemberNames.Contains(nameof(OpportunityFormModel.CustomerId)));

        var invalidMoney = new OpportunityFormModel
        {
            Title = "Opportunity",
            CustomerId = Guid.NewGuid(),
            Amount = 1.234m,
            Currency = "USD",
            Probability = 50,
            ExpectedCloseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
        };

        Validate(invalidMoney).Should().Contain(result =>
            result.MemberNames.Contains(nameof(OpportunityFormModel.Amount)));

        var invalidForecast = new OpportunityFormModel
        {
            Title = "Opportunity",
            CustomerId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "US",
            Probability = 101,
            ExpectedCloseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
        };

        IReadOnlyList<ValidationResult> errors = Validate(invalidForecast);
        errors.Should().Contain(result => result.MemberNames.Contains(nameof(OpportunityFormModel.Currency)));
        errors.Should().Contain(result => result.MemberNames.Contains(nameof(OpportunityFormModel.Probability)));

        var invalidDate = new OpportunityFormModel
        {
            Title = "Opportunity",
            CustomerId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Probability = 50,
            ExpectedCloseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
        };

        Validate(invalidDate).Should().Contain(result =>
            result.MemberNames.Contains(nameof(OpportunityFormModel.ExpectedCloseDate)));
    }

    private static IReadOnlyList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private async Task<string> RenderAsync<TComponent>(Dictionary<string, object?> parameters)
        where TComponent : IComponent
    {
        ILoggerFactory loggerFactory = _services.GetRequiredService<ILoggerFactory>();
        await using var renderer = new HtmlRenderer(_services, loggerFactory);

        var hostParameters = new Dictionary<string, object?>
        {
            [nameof(FormHost.ComponentType)] = typeof(TComponent),
            [nameof(FormHost.ComponentParameters)] = parameters,
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

    private sealed class NoopSender : ISender
    {
        public Task<TResponse> SendAsync<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("The test renderer does not dispatch requests.");
    }
}
