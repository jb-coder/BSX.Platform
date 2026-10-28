namespace BSX.Web.Navigation;

/// <summary>A permission-aware navigation destination.</summary>
/// <param name="Label">The visible label.</param>
/// <param name="Href">The application-relative URL.</param>
/// <param name="Permission">The permission required to see the item, if any.</param>
/// <param name="Description">A short description for the page subtitle.</param>
/// <param name="Icon">The icon key used by the shell.</param>
public sealed record NavigationItem(
    string Label,
    string Href,
    string? Permission,
    string Description,
    string Icon);

/// <summary>A navigation group.</summary>
/// <param name="Label">The section label.</param>
/// <param name="Items">The items in the section.</param>
public sealed record NavigationSection(string Label, IReadOnlyList<NavigationItem> Items);
