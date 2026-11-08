namespace sc.Models;

public sealed class BookCatalogViewModel
{
    public List<Book> Books { get; init; } = [];
    public string? Query { get; init; }
    public string[] Categories { get; init; } = [];
    public bool Promotion { get; init; }
    public string Sort { get; init; } = "new";
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed class AdminBookListViewModel
{
    public List<Book> Books { get; init; } = [];
    public string? Query { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record BookApiItem(
    int Id,
    string Title,
    string Author,
    string Category,
    string ISBN,
    decimal Price,
    decimal SalePrice,
    int DiscountPercent,
    decimal DiscountAmount,
    int Stock,
    double Rating,
    int ReviewCount,
    string CoverImage,
    string Description);

public sealed record PagedBookApiResponse(
    IReadOnlyList<BookApiItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public static class BookCatalogQuery
{
    public static IQueryable<Book> Apply(
        IQueryable<Book> books,
        string? query,
        IReadOnlyCollection<string>? categories,
        bool promotion,
        string? sort)
    {
        books = books.Where(book => book.Title != "");

        if (!string.IsNullOrWhiteSpace(query))
        {
            var search = query.Trim();
            books = books.Where(book => book.Title.Contains(search) || book.Author.Contains(search));
        }

        if (categories is { Count: > 0 })
            books = books.Where(book => categories.Contains(book.Category));

        if (promotion)
            books = books.Where(book => book.DiscountAmount > 0 || book.DiscountPercent > 0);

        return sort switch
        {
            "price-asc" => books
                .OrderBy(book => book.DiscountAmount > 0
                    ? (book.Price > book.DiscountAmount ? book.Price - book.DiscountAmount : 0m)
                    : (book.Price > book.Price * book.DiscountPercent / 100
                        ? book.Price - book.Price * book.DiscountPercent / 100
                        : 0m))
                .ThenByDescending(book => book.Id),
            "price-desc" => books
                .OrderByDescending(book => book.DiscountAmount > 0
                    ? (book.Price > book.DiscountAmount ? book.Price - book.DiscountAmount : 0m)
                    : (book.Price > book.Price * book.DiscountPercent / 100
                        ? book.Price - book.Price * book.DiscountPercent / 100
                        : 0m))
                .ThenByDescending(book => book.Id),
            "rating" => books.OrderByDescending(book => book.Rating).ThenByDescending(book => book.Id),
            _ => books.OrderByDescending(book => book.Id)
        };
    }

    public static int NormalizePage(int page) => Math.Max(page, 1);

    public static int NormalizePageSize(int pageSize, int maximum = 48) =>
        Math.Clamp(pageSize, 1, maximum);

    public static string[] NormalizeCategories(IEnumerable<string>? categories) =>
        categories?
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];
}
