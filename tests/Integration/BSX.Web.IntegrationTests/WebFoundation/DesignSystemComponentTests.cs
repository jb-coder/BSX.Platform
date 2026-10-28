using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BSX.Web.Components.DesignSystem;
using BSX.Web.Notifications;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class DesignSystemComponentTests
{
    private readonly ServiceProvider _services;

    public DesignSystemComponentTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IToastService, ToastService>();
        _services = services.BuildServiceProvider();
    }

    [Fact]
    public async Task PageHeader_ShouldRenderTitleAndSubtitle()
    {
        string html = await RenderAsync<PageHeader>(new Dictionary<string, object?>
        {
            [nameof(PageHeader.Title)] = "Customers",
            [nameof(PageHeader.Subtitle)] = "Customer records",
        });

        html.Should().Contain("<h1>Customers</h1>");
        html.Should().Contain("Customer records");
    }

    [Fact]
    public async Task EmptyState_ShouldExposeStatusAndTitle()
    {
        string html = await RenderAsync<EmptyState>(new Dictionary<string, object?>
        {
            [nameof(EmptyState.Title)] = "No customers",
            [nameof(EmptyState.Description)] = "Add your first customer.",
        });

        html.Should().Contain("role=\"status\"");
        html.Should().Contain("<h2>No customers</h2>");
    }

    [Fact]
    public async Task LoadingState_ShouldExposeLiveStatus()
    {
        string html = await RenderAsync<LoadingState>(new Dictionary<string, object?>
        {
            [nameof(LoadingState.Label)] = "Loading customers...",
        });

        html.Should().Contain("role=\"status\"");
        html.Should().Contain("aria-live=\"polite\"");
        html.Should().Contain("Loading customers...");
    }

    [Fact]
    public async Task SearchBox_ShouldHaveAccessibleLabelAndSearchInput()
    {
        string html = await RenderAsync<SearchBox>(new Dictionary<string, object?>
        {
            [nameof(SearchBox.Label)] = "Search customers",
            [nameof(SearchBox.Placeholder)] = "Name or code",
            [nameof(SearchBox.Name)] = "search",
            [nameof(SearchBox.ShowClearButton)] = false,
        });

        html.Should().Contain("<label class=\"visually-hidden\"");
        html.Should().Contain("Search customers");
        html.Should().Contain("type=\"search\"");
        html.Should().Contain("placeholder=\"Name or code\"");
        html.Should().Contain("name=\"search\"");
        html.Should().NotContain("search-box-clear");
    }

    [Fact]
    public async Task DataTable_ShouldHaveCaptionAndRows()
    {
        string html = await RenderAsync<DataTable<string>>(new Dictionary<string, object?>
        {
            [nameof(DataTable<string>.Items)] = new[] { "one", "two" },
            [nameof(DataTable<string>.Caption)] = "Customer results",
        });

        html.Should().Contain("<table class=\"data-table\">");
        html.Should().Contain("<caption class=\"visually-hidden\">Customer results</caption>");
    }

    [Fact]
    public async Task Pagination_ShouldExposeTotalPagesAndDisabledState()
    {
        string html = await RenderAsync<Pagination>(new Dictionary<string, object?>
        {
            [nameof(Pagination.Page)] = 1,
            [nameof(Pagination.PageSize)] = 20,
            [nameof(Pagination.TotalCount)] = 60,
        });

        html.Should().Contain("aria-label=\"Pagination\"");
        html.Should().Contain("Page 1 of 3");
        html.Should().Contain("disabled");
    }

    [Fact]
    public async Task Pagination_ShouldRenderLinks_WhenPageUrlFactoryIsSupplied()
    {
        string html = await RenderAsync<Pagination>(new Dictionary<string, object?>
        {
            [nameof(Pagination.Page)] = 2,
            [nameof(Pagination.PageSize)] = 20,
            [nameof(Pagination.TotalCount)] = 60,
            [nameof(Pagination.PageHrefFactory)] = (Func<int, string>)(page => $"/app/crm/customers?page={page}"),
        });

        html.Should().Contain("href=\"/app/crm/customers?page=1\"");
        html.Should().Contain("href=\"/app/crm/customers?page=3\"");
        html.Should().NotContain("<button");
    }

    [Fact]
    public async Task ConfirmDialog_ShouldHaveAccessibleDialogSemantics()
    {
        string html = await RenderAsync<ConfirmDialog>(new Dictionary<string, object?>
        {
            [nameof(ConfirmDialog.Open)] = true,
            [nameof(ConfirmDialog.Title)] = "Delete customer",
            [nameof(ConfirmDialog.Message)] = "This cannot be undone.",
            [nameof(ConfirmDialog.Busy)] = true,
        });

        html.Should().Contain("role=\"dialog\"");
        html.Should().Contain("aria-modal=\"true\"");
        html.Should().Contain("aria-busy=\"true\"");
        html.Should().Contain("Working...");
        html.Should().Contain("disabled");
        html.Should().Contain("Delete customer");
    }

    [Fact]
    public async Task ToastHost_ShouldRenderNotificationsWithStatusRoles()
    {
        IToastService toasts = _services.GetRequiredService<IToastService>();
        toasts.Success("Saved", "The record was saved.");

        string html = await RenderAsync<ToastHost>();

        html.Should().Contain("toast-host");
        html.Should().Contain("role=\"status\"");
        html.Should().Contain("Saved");
    }

    private async Task<string> RenderAsync<TComponent>(Dictionary<string, object?>? parameters = null)
        where TComponent : IComponent
    {
        ILoggerFactory loggerFactory = _services.GetRequiredService<ILoggerFactory>();
        await using var renderer = new HtmlRenderer(_services, loggerFactory);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = parameters is null
                ? await renderer.RenderComponentAsync<TComponent>()
                : await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters));

            return root.ToHtmlString();
        });
    }
}
