using BSX.Modules.Crm.Domain.Errors;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Domain.ValueObjects;

/// <summary>A postal address value object.</summary>
public sealed class Address : ValueObject
{
    /// <summary>Maximum length for a street line.</summary>
    public const int MaxLineLength = 200;

    /// <summary>Maximum length for a city or region.</summary>
    public const int MaxAreaLength = 100;

    /// <summary>Maximum length for a postal code.</summary>
    public const int MaxPostalCodeLength = 20;

    /// <summary>Maximum length for a country.</summary>
    public const int MaxCountryLength = 100;

    private Address(string line1, string? line2, string city, string? region, string? postalCode, string country)
    {
        Line1 = line1;
        Line2 = line2;
        City = city;
        Region = region;
        PostalCode = postalCode;
        Country = country;
    }

    /// <summary>Gets the first street line.</summary>
    public string Line1 { get; }

    /// <summary>Gets the optional second street line.</summary>
    public string? Line2 { get; }

    /// <summary>Gets the city.</summary>
    public string City { get; }

    /// <summary>Gets the optional region, state or province.</summary>
    public string? Region { get; }

    /// <summary>Gets the optional postal code.</summary>
    public string? PostalCode { get; }

    /// <summary>Gets the country.</summary>
    public string Country { get; }

    /// <summary>Creates a validated postal address.</summary>
    /// <param name="line1">The first street line.</param>
    /// <param name="line2">The optional second street line.</param>
    /// <param name="city">The city.</param>
    /// <param name="region">The optional region, state or province.</param>
    /// <param name="postalCode">The optional postal code.</param>
    /// <param name="country">The country.</param>
    public static Result<Address> Create(
        string? line1,
        string? line2,
        string? city,
        string? region,
        string? postalCode,
        string? country)
    {
        string? normalizedLine1 = Normalize(line1);
        string? normalizedLine2 = Normalize(line2);
        string? normalizedCity = Normalize(city);
        string? normalizedRegion = Normalize(region);
        string? normalizedPostalCode = Normalize(postalCode);
        string? normalizedCountry = Normalize(country);

        if (normalizedLine1 is null
            || normalizedCity is null
            || normalizedCountry is null
            || normalizedLine1.Length > MaxLineLength
            || normalizedLine2?.Length > MaxLineLength
            || normalizedCity.Length > MaxAreaLength
            || normalizedRegion?.Length > MaxAreaLength
            || normalizedPostalCode?.Length > MaxPostalCodeLength
            || normalizedCountry.Length > MaxCountryLength)
        {
            return Result.Failure<Address>(CrmErrors.AddressInvalid);
        }

        return Result.Success(new Address(
            normalizedLine1,
            normalizedLine2,
            normalizedCity,
            normalizedRegion,
            normalizedPostalCode,
            normalizedCountry));
    }

    /// <inheritdoc />
    public override string ToString()
        => string.Join(", ", new[] { Line1, Line2, City, Region, PostalCode, Country }.Where(part => part is not null));

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Line1;
        yield return Line2;
        yield return City;
        yield return Region;
        yield return PostalCode;
        yield return Country;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
