using System.Security.Claims;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Customers.CreateCustomer;
using BSX.Modules.Crm.Application.Customers.GetCustomerById;
using BSX.Web.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.Web.IntegrationTests.WebFoundation;

public sealed class WebCurrentUserTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public WebCurrentUserTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CircuitPrincipal_ShouldAuthorizeAndScopeCrmApplicationRequests()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var currentUser = scope.ServiceProvider.GetRequiredService<WebCurrentUser>();
        currentUser.SetPrincipal(CreatePrincipal());
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        string code = CrmTestData.NewCode();

        var created = await sender.SendAsync(
            new CreateCustomerCommand(code, "Circuit Customer", "TAX-CIRCUIT"));

        created.IsSuccess.Should().BeTrue();
        created.Value.Should().NotBe(Guid.Empty);

        var customer = await sender.SendAsync(new GetCustomerByIdQuery(created.Value));

        customer.IsSuccess.Should().BeTrue();
        customer.Value.Code.Should().Be(code.ToUpperInvariant());
        customer.Value.TenantId.Should().Be(_factory.TenantId);
    }

    private ClaimsPrincipal CreatePrincipal()
    {
        var claims = new List<Claim>
        {
            new(WebAuthDefaults.UserIdClaim, _factory.AdminUserId.ToString()),
            new(WebAuthDefaults.EmailClaim, IdentityApiFactory.AdminEmail),
            new(WebAuthDefaults.NameClaim, "Administrator"),
            new(WebAuthDefaults.TenantIdClaim, _factory.TenantId.ToString()),
        };

        claims.AddRange(IdentityApiFactory.AdminPermissions.Select(
            permission => new Claim(WebAuthDefaults.PermissionClaim, permission)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }
}
