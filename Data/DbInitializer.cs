using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using sc.Models;

namespace sc.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH(N'dbo.Books', N'DiscountAmount') IS NULL
                ALTER TABLE [dbo].[Books] ADD [DiscountAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Books_DiscountAmount] DEFAULT 0;
            """);
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.ReturnRequests', N'U') IS NULL
            CREATE TABLE [dbo].[ReturnRequests] ([Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [OrderId] int NOT NULL, [UserId] nvarchar(450) NOT NULL, [RequestType] nvarchar(30) NOT NULL, [Reason] nvarchar(1000) NOT NULL, [Status] nvarchar(50) NOT NULL, [CreatedAt] datetime2 NOT NULL, [ResolvedAt] datetime2 NULL, CONSTRAINT [FK_ReturnRequests_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE);
            IF OBJECT_ID(N'dbo.CustomerFeedbacks', N'U') IS NULL
            CREATE TABLE [dbo].[CustomerFeedbacks] ([Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [UserId] nvarchar(450) NOT NULL, [Subject] nvarchar(200) NOT NULL, [Message] nvarchar(2000) NOT NULL, [Status] nvarchar(50) NOT NULL, [CreatedAt] datetime2 NOT NULL);
            """);
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE [dbo].[Orders] SET [Status] = N'Đang xử lý' WHERE [Status] = N'Mới';
            UPDATE [dbo].[Orders] SET [Status] = N'Đã đóng gói - chờ vận chuyển' WHERE [Status] = N'Đã xác nhận';
            UPDATE [dbo].[Orders] SET [Status] = N'Đang vận chuyển' WHERE [Status] = N'Đang giao';
            UPDATE [dbo].[Orders] SET [DeliveryMethod] = N'Ship' WHERE [DeliveryMethod] <> N'Ship';
            """);
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
            CREATE TABLE [dbo].[Categories] ([Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [Name] nvarchar(200) NOT NULL, [IsActive] bit NOT NULL);
            IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
            CREATE TABLE [dbo].[Orders] ([Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [UserId] nvarchar(450) NULL, [ReceiverName] nvarchar(200) NOT NULL, [Phone] nvarchar(50) NOT NULL, [Address] nvarchar(500) NOT NULL, [DeliveryMethod] nvarchar(30) NOT NULL, [Status] nvarchar(50) NOT NULL, [Total] decimal(18,2) NOT NULL, [CreatedAt] datetime2 NOT NULL);
            IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
            CREATE TABLE [dbo].[OrderItems] ([Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [OrderId] int NOT NULL, [BookId] int NOT NULL, [BookTitle] nvarchar(300) NOT NULL, [Quantity] int NOT NULL, [UnitPrice] decimal(18,2) NOT NULL, CONSTRAINT [FK_OrderItems_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE);
            """);
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH(N'dbo.ReturnRequests', N'CustomerEmail') IS NULL ALTER TABLE [dbo].[ReturnRequests] ADD [CustomerEmail] nvarchar(256) NOT NULL CONSTRAINT [DF_ReturnRequests_CustomerEmail] DEFAULT N'';
            IF COL_LENGTH(N'dbo.ReturnRequests', N'CustomerPhone') IS NULL ALTER TABLE [dbo].[ReturnRequests] ADD [CustomerPhone] nvarchar(50) NOT NULL CONSTRAINT [DF_ReturnRequests_CustomerPhone] DEFAULT N'';
            IF COL_LENGTH(N'dbo.CustomerFeedbacks', N'CustomerEmail') IS NULL ALTER TABLE [dbo].[CustomerFeedbacks] ADD [CustomerEmail] nvarchar(256) NOT NULL CONSTRAINT [DF_CustomerFeedbacks_CustomerEmail] DEFAULT N'';
            """);
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.Wishlists', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Wishlists] (
                    [UserId] nvarchar(450) NOT NULL,
                    [BookId] int NOT NULL,
                    [AddedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_Wishlists] PRIMARY KEY ([UserId], [BookId]),
                    CONSTRAINT [FK_Wishlists_Books_BookId] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Books] ([Id]) ON DELETE CASCADE
                );
            END
            """);
        await db.Books.Where(book => string.IsNullOrWhiteSpace(book.Title)).ExecuteDeleteAsync();
        if (!await db.Books.AnyAsync())
        {
            db.Books.AddRange(StoreData.Books.Select(book => new Book
            {
                Title = book.Title,
                Author = book.Author,
                Category = book.Category,
                Price = book.Price,
                DiscountPercent = book.DiscountPercent,
                Stock = book.Stock,
                Rating = book.Rating,
                CoverImage = book.CoverImage,
                Description = book.Description
            }));
            await db.SaveChangesAsync();
        }
        else
        {
            var existingTitles = (await db.Books.Select(book => book.Title).ToListAsync()).ToHashSet();
            var newBooks = StoreData.Books.Where(book => !existingTitles.Contains(book.Title)).Select(book => new Book
            {
                Title = book.Title, Author = book.Author, Category = book.Category, Price = book.Price,
                DiscountPercent = book.DiscountPercent, Stock = book.Stock, Rating = book.Rating,
                CoverImage = book.CoverImage, Description = book.Description
            });
            db.Books.AddRange(newBooks);
            await db.SaveChangesAsync();
        }
        var oldCategoryMap = new Dictionary<string, string>
        {
            ["Kỹ năng & Kinh doanh"] = "Kĩ năng & Kinh doanh",
            ["Tâm lý & Tâm linh"] = "Văn học & Tiểu thuyết",
            ["Lịch sử & Khoa học"] = "Sách khoa học",
            ["Thiếu nhi & Truyện tranh"] = "Sách thiếu nhi",
            ["Ngoại ngữ & Sách ngoại văn"] = "Sách Ngoại Ngữ"
        };
        foreach (var mapping in oldCategoryMap)
            await db.Books.Where(book => book.Category == mapping.Key).ExecuteUpdateAsync(setters => setters.SetProperty(book => book.Category, mapping.Value));
        var categories = await db.Categories.ToListAsync();
        foreach (var category in categories.Where(category => !StoreData.Categories.Contains(category.Name)))
            db.Categories.Remove(category);
        foreach (var category in categories.Where(category => StoreData.Categories.Contains(category.Name)))
            category.IsActive = true;
        foreach (var name in StoreData.Categories.Where(name => !categories.Any(category => category.Name == name)))
            db.Categories.Add(new Category { Name = name, IsActive = true });
        await db.SaveChangesAsync();
        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(StoreData.Categories.Select(name => new Category { Name = name }));
            await db.SaveChangesAsync();
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Customer" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        await CreateUserAsync(userManager, "admin@latrang.vn", "Admin123!", "Quản trị viên", "Admin", "0900000000");
        await CreateUserAsync(userManager, "admin1@gmail.com", "12345678", "Quản trị viên 1", "Admin", "0900000001");
        await CreateUserAsync(userManager, "admin2@gmail.com", "12345678", "Quản trị viên 2", "Admin", "0900000002");
        await CreateUserAsync(userManager, "admin3@gmail.com", "12345678", "Quản trị viên 3", "Admin", "0900000003");
        await CreateUserAsync(userManager, "customer@latrang.vn", "Customer123!", "Khách hàng mẫu", "Customer", "0910000000");
        await CreateUserAsync(userManager, "khachhangmau1@gmail.com", "12345678", "Khách hàng mẫu 1", "Customer", "0910000001");
        await CreateUserAsync(userManager, "khachhangmau2@gmail.com", "12345678", "Khách hàng mẫu 2", "Customer", "0910000002");
        await CreateUserAsync(userManager, "khachhangmau3@gmail.com", "12345678", "Khách hàng mẫu 3", "Customer", "0910000003");
    }

    private static async Task CreateUserAsync(
        UserManager<AppUser> userManager, string email, string password, string fullName, string role, string phone)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, FullName = fullName, PhoneNumber = phone, PhoneNumberConfirmed = true };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
        else
        {
            user.FullName = fullName;
            user.PhoneNumber = phone;
            user.PhoneNumberConfirmed = true;
            await userManager.UpdateAsync(user);
        }
        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);
    }
}
