using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Web.Crm;

/// <summary>Presentation helpers shared by organization screens.</summary>
public static class OrganizationDisplay
{
    /// <summary>Returns a compact address location for a list row.</summary>
    /// <param name="address">The organization primary address, if any.</param>
    public static string Location(AddressDto? address)
        => address is null
            ? "No primary address"
            : string.Join(", ", new[] { address.City, address.Country }.Where(part => !string.IsNullOrWhiteSpace(part)));

    /// <summary>Returns the address lines in display order.</summary>
    /// <param name="address">The address.</param>
    public static IReadOnlyList<string> AddressLines(AddressDto address)
    {
        ArgumentNullException.ThrowIfNull(address);

        var lines = new List<string> { address.Line1 };

        if (!string.IsNullOrWhiteSpace(address.Line2))
        {
            lines.Add(address.Line2);
        }

        lines.Add(string.IsNullOrWhiteSpace(address.Region)
            ? address.City
            : $"{address.City}, {address.Region}");

        if (!string.IsNullOrWhiteSpace(address.PostalCode))
        {
            lines.Add(address.PostalCode);
        }

        lines.Add(address.Country);
        return lines;
    }
}
