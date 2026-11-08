using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ganss.Xss;
using sc.Data;
using sc.Models;

namespace sc.Controllers;

public class BooksController(AppDbContext db, HtmlSanitizer sanitizer) : Controller
{
    private const int PageSize = 12;

    public async Task<IActionResult> Index(
        string? q,
        string[]? category,
        bool promotion = false,
        string? sort = "new",
        int page = 1)
    {
        var model = await CreateCatalogPageAsync(q, category, promotion, sort, page);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Results(
        string? q,
        string[]? category,
        bool promotion = false,
        string? sort = "new",
        int page = 1)
    {
        var model = await CreateCatalogPageAsync(q, category, promotion, sort, page);
        return PartialView("_BookResults", model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id && b.Title != "");
        if (book is null) return NotFound();
        book.Description = sanitizer.Sanitize(book.Description ?? "");
        return View(book);
    }

    private async Task<BookCatalogViewModel> CreateCatalogPageAsync(
        string? query,
        IEnumerable<string>? categories,
        bool promotion,
        string? sort,
        int page)
    {
        var selectedCategories = BookCatalogQuery.NormalizeCategories(categories);
        var selectedSort = sort is "price-asc" or "price-desc" or "rating" ? sort : "new";
        var selectedPage = BookCatalogQuery.NormalizePage(page);
        var baseQuery = BookCatalogQuery.Apply(
            db.Books.AsNoTracking(), query, selectedCategories, promotion, selectedSort);
        var totalCount = await baseQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        selectedPage = totalPages == 0 ? 1 : Math.Min(selectedPage, totalPages);

        return new BookCatalogViewModel
        {
            Books = await baseQuery
                .Skip((selectedPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync(),
            Query = query?.Trim(),
            Categories = selectedCategories,
            Promotion = promotion,
            Sort = selectedSort,
            Page = selectedPage,
            PageSize = PageSize,
            TotalCount = totalCount
        };
    }
}
