namespace eShop.Catalog.API.Model;

/// <summary>
/// DTO for search autocomplete suggestions.
/// </summary>
/// <param name="Id">The catalog item ID.</param>
/// <param name="Name">The product name.</param>
/// <param name="Category">The category/type name.</param>
public record SearchSuggestion(int Id, string Name, string Category);
