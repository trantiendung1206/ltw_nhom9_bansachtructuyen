using Ganss.Xss;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sc.Data;
using sc.Models;

namespace sc.Controllers;

[ApiController]
[Route("api/books")]
[Produces("application/json")]
public sealed class BooksApiController(
    AppDbContext db,
    HtmlSanitizer sanitizer,
    IAntiforgery antiforgery,
    IWebHostEnvironment environment,
    ILogger<BooksApiController> logger) : ControllerBase
{
    [HttpGet("antiforgery")]
    [Authorize(Roles = "Admin")]
    public IActionResult AntiforgeryToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedBookApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedBookApiResponse>> List(
        [FromQuery] string? q,
        [FromQuery] string[]? category,
        [FromQuery] bool promotion = false,
        [FromQuery] string? sort = "new",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var selectedCategories = BookCatalogQuery.NormalizeCategories(category);
        var selectedPage = BookCatalogQuery.NormalizePage(page);
        var selectedPageSize = BookCatalogQuery.NormalizePageSize(pageSize);
        var selectedSort = sort is "price-asc" or "price-desc" or "rating" ? sort : "new";
        var query = BookCatalogQuery.Apply(
            db.Books.AsNoTracking(), q, selectedCategories, promotion, selectedSort);
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)selectedPageSize);
        selectedPage = totalPages == 0 ? 1 : Math.Min(selectedPage, totalPages);

        var books = await query
            .Skip((selectedPage - 1) * selectedPageSize)
            .Take(selectedPageSize)
            .ToListAsync();
        var response = new PagedBookApiResponse(
            books.Select(book => new BookApiItem(
                book.Id,
                book.Title,
                book.Author,
                book.Category,
                book.ISBN,
                book.Price,
                book.SalePrice,
                book.DiscountPercent,
                book.DiscountAmount,
                book.Stock,
                book.Rating,
                book.ReviewCount,
                book.CoverImage,
                sanitizer.Sanitize(book.Description ?? ""))).ToList(),
            selectedPage,
            selectedPageSize,
            totalCount,
            totalPages);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BookApiItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookApiItem>> Details(int id)
    {
        var book = await db.Books.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && item.Title != "");
        if (book is null)
            return NotFound();

        return Ok(new BookApiItem(
            book.Id,
            book.Title,
            book.Author,
            book.Category,
            book.ISBN,
            book.Price,
            book.SalePrice,
            book.DiscountPercent,
            book.DiscountAmount,
            book.Stock,
            book.Rating,
            book.ReviewCount,
            book.CoverImage,
            sanitizer.Sanitize(book.Description ?? "")));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(BookApiItem), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookApiItem>> Create([FromBody] BookApiWriteModel input)
    {
        await ValidateWriteModelAsync(input);
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var book = new Book();
        ApplyWriteModel(input, book);
        db.Books.Add(book);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Details), new { id = book.Id }, ToApiItem(book));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(BookApiItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookApiItem>> Update(int id, [FromBody] BookApiWriteModel input)
    {
        var book = await db.Books.FindAsync(id);
        if (book is null)
            return NotFound();

        await ValidateWriteModelAsync(input);
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        ApplyWriteModel(input, book);
        await db.SaveChangesAsync();
        return Ok(ToApiItem(book));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await db.Books.FindAsync(id);
        if (book is null)
            return NotFound();

        var hasHistory = await db.OrderItems.AnyAsync(item => item.BookId == id) ||
            await db.BookReviews.AnyAsync(review => review.BookId == id);
        if (hasHistory)
            return Conflict(new ProblemDetails
            {
                Title = "Không thể xóa sách đã có lịch sử",
                Detail = "Sách đã được sử dụng trong đơn hàng hoặc đánh giá."
            });

        var coverPath = book.CoverImage;
        db.Books.Remove(book);
        await db.SaveChangesAsync();
        DeleteManagedCover(coverPath);
        return NoContent();
    }

    private async Task ValidateWriteModelAsync(BookApiWriteModel input)
    {
        if (!await db.Categories.AnyAsync(category =>
                category.IsActive && category.Name == input.Category))
            ModelState.AddModelError(nameof(input.Category), "Danh mục không tồn tại hoặc không hoạt động.");

        if (!IsSafeCoverReference(input.CoverImage))
            ModelState.AddModelError(nameof(input.CoverImage), "Ảnh bìa phải là URL HTTP(S) hoặc đường dẫn nội bộ hợp lệ.");

        input.Description = sanitizer.Sanitize(input.Description ?? "");
        if (input.Description.Length > 20000)
            ModelState.AddModelError(nameof(input.Description), "Mô tả không được vượt quá 20.000 ký tự sau khi làm sạch.");
    }

    private static bool IsSafeCoverReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return true;
        if (Uri.TryCreate(reference, UriKind.Absolute, out var uri))
            return uri.Scheme is "http" or "https";
        return reference.StartsWith('/') && !reference.StartsWith("//") &&
            !reference.Contains('\\') && !reference.Contains("..", StringComparison.Ordinal);
    }

    private void DeleteManagedCover(string? cover)
    {
        if (string.IsNullOrWhiteSpace(cover) || !cover.StartsWith("/uploads/books/", StringComparison.Ordinal))
            return;

        var path = Path.Combine(environment.WebRootPath, "uploads", "books", Path.GetFileName(cover));
        if (!System.IO.File.Exists(path))
            return;
        try
        {
            System.IO.File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Could not remove deleted book cover {CoverPath}", cover);
        }
    }

    private static void ApplyWriteModel(BookApiWriteModel input, Book book)
    {
        book.Title = input.Title.Trim();
        book.Author = input.Author.Trim();
        book.Category = input.Category;
        book.ISBN = input.ISBN.Trim();
        book.Price = input.Price;
        book.DiscountPercent = input.DiscountPercent;
        book.DiscountAmount = input.DiscountAmount;
        book.Stock = input.Stock;
        book.CoverImage = input.CoverImage ?? "";
        book.Description = input.Description;
    }

    private BookApiItem ToApiItem(Book book) => new(
        book.Id,
        book.Title,
        book.Author,
        book.Category,
        book.ISBN,
        book.Price,
        book.SalePrice,
        book.DiscountPercent,
        book.DiscountAmount,
        book.Stock,
        book.Rating,
        book.ReviewCount,
        book.CoverImage,
        sanitizer.Sanitize(book.Description ?? ""));
}
