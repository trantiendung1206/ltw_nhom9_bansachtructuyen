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
            IF COL_LENGTH(N'dbo.Books', N'ISBN') IS NULL
                ALTER TABLE [dbo].[Books] ADD [ISBN] nvarchar(20) NOT NULL CONSTRAINT [DF_Books_ISBN] DEFAULT N'';
            IF COL_LENGTH(N'dbo.Books', N'ReviewCount') IS NULL
                ALTER TABLE [dbo].[Books] ADD [ReviewCount] int NOT NULL CONSTRAINT [DF_Books_ReviewCount] DEFAULT 0;
            IF OBJECT_ID(N'dbo.DF_Books_CoverImage', N'D') IS NULL
                ALTER TABLE [dbo].[Books] ADD CONSTRAINT [DF_Books_CoverImage] DEFAULT N'' FOR [CoverImage];
            IF OBJECT_ID(N'dbo.DF_Books_Description', N'D') IS NULL
                ALTER TABLE [dbo].[Books] ADD CONSTRAINT [DF_Books_Description] DEFAULT N'' FOR [Description];
            """);
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.ReturnRequests', N'U') IS NULL
            CREATE TABLE [dbo].[ReturnRequests] ([Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [OrderId] int NOT NULL, [UserId] nvarchar(450) NOT NULL, [RequestType] nvarchar(30) NOT NULL, [Reason] nvarchar(1000) NOT NULL, [Status] nvarchar(50) NOT NULL, [RejectionReason] nvarchar(1000) NOT NULL CONSTRAINT [DF_ReturnRequests_RejectionReason] DEFAULT N'', [CreatedAt] datetime2 NOT NULL, [ResolvedAt] datetime2 NULL, CONSTRAINT [FK_ReturnRequests_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE);
            IF OBJECT_ID(N'dbo.CustomerFeedbacks', N'U') IS NULL
            CREATE TABLE [dbo].[CustomerFeedbacks] ([Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [UserId] nvarchar(450) NOT NULL, [Subject] nvarchar(200) NOT NULL, [Message] nvarchar(2000) NOT NULL, [CustomerEmail] nvarchar(256) NOT NULL CONSTRAINT [DF_CustomerFeedbacks_CustomerEmail] DEFAULT N'', [Status] nvarchar(50) NOT NULL, [CreatedAt] datetime2 NOT NULL);
            IF OBJECT_ID(N'dbo.BookReviews', N'U') IS NULL
            CREATE TABLE [dbo].[BookReviews] ([Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [BookId] int NOT NULL, [OrderId] int NOT NULL, [UserId] nvarchar(450) NOT NULL, [Rating] int NOT NULL, [CreatedAt] datetime2 NOT NULL, CONSTRAINT [UQ_BookReviews_Book_Order_User] UNIQUE ([BookId], [OrderId], [UserId]), CONSTRAINT [FK_BookReviews_Books] FOREIGN KEY ([BookId]) REFERENCES [dbo].[Books]([Id]) ON DELETE CASCADE, CONSTRAINT [FK_BookReviews_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE);
            """);
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
            CREATE TABLE [dbo].[Categories] ([Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [Name] nvarchar(200) NOT NULL, [IsActive] bit NOT NULL);
            IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
            CREATE TABLE [dbo].[Orders] ([Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [UserId] nvarchar(450) NULL, [ReceiverName] nvarchar(200) NOT NULL, [Phone] nvarchar(50) NOT NULL, [Address] nvarchar(500) NOT NULL, [Status] nvarchar(50) NOT NULL, [CancellationReason] nvarchar(1000) NOT NULL CONSTRAINT [DF_Orders_CancellationReason] DEFAULT N'', [Total] decimal(18,2) NOT NULL, [RefundedAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Orders_RefundedAmount] DEFAULT 0, [CreatedAt] datetime2 NOT NULL);
            IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
            CREATE TABLE [dbo].[OrderItems] ([Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [OrderId] int NOT NULL, [BookId] int NOT NULL, [BookTitle] nvarchar(300) NOT NULL, [Quantity] int NOT NULL, [UnitPrice] decimal(18,2) NOT NULL, CONSTRAINT [FK_OrderItems_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE);
            IF OBJECT_ID(N'dbo.OrderStatusHistories', N'U') IS NULL
            CREATE TABLE [dbo].[OrderStatusHistories] ([Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY, [OrderId] int NOT NULL, [OccurredAt] datetime2 NOT NULL, [Action] nvarchar(200) NOT NULL, CONSTRAINT [FK_OrderStatusHistories_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE);
            """);
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH(N'dbo.Orders', N'CancellationReason') IS NULL
                ALTER TABLE [dbo].[Orders] ADD [CancellationReason] nvarchar(1000) NOT NULL CONSTRAINT [DF_Orders_CancellationReason] DEFAULT N'';
            IF COL_LENGTH(N'dbo.Orders', N'RefundedAmount') IS NULL
                ALTER TABLE [dbo].[Orders] ADD [RefundedAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Orders_RefundedAmount] DEFAULT 0;
            IF COL_LENGTH(N'dbo.Orders', N'CompletedAt') IS NULL
                ALTER TABLE [dbo].[Orders] ADD [CompletedAt] datetime2 NULL;
            IF COL_LENGTH(N'dbo.Orders', N'DeliveryMethod') IS NOT NULL
            BEGIN
                DECLARE @deliveryMethodDefault sysname;
                SELECT @deliveryMethodDefault = dc.name
                FROM sys.default_constraints dc
                INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Orders')
                  AND c.name = N'DeliveryMethod';
                IF @deliveryMethodDefault IS NOT NULL
                    EXEC(N'ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [' + @deliveryMethodDefault + N']');
                ALTER TABLE [dbo].[Orders] DROP COLUMN [DeliveryMethod];
            END;
            """);
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH(N'dbo.ReturnRequests', N'CustomerEmail') IS NULL ALTER TABLE [dbo].[ReturnRequests] ADD [CustomerEmail] nvarchar(256) NOT NULL CONSTRAINT [DF_ReturnRequests_CustomerEmail] DEFAULT N'';
            IF COL_LENGTH(N'dbo.ReturnRequests', N'CustomerPhone') IS NULL ALTER TABLE [dbo].[ReturnRequests] ADD [CustomerPhone] nvarchar(50) NOT NULL CONSTRAINT [DF_ReturnRequests_CustomerPhone] DEFAULT N'';
            IF COL_LENGTH(N'dbo.CustomerFeedbacks', N'CustomerEmail') IS NULL ALTER TABLE [dbo].[CustomerFeedbacks] ADD [CustomerEmail] nvarchar(256) NOT NULL CONSTRAINT [DF_CustomerFeedbacks_CustomerEmail] DEFAULT N'';
            IF COL_LENGTH(N'dbo.ReturnRequests', N'RejectionReason') IS NULL ALTER TABLE [dbo].[ReturnRequests] ADD [RejectionReason] nvarchar(1000) NOT NULL CONSTRAINT [DF_ReturnRequests_RejectionReason] DEFAULT N'';
            """);
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.Wishlists', N'U') IS NOT NULL
                DROP TABLE [dbo].[Wishlists];
            """);
        await ResetApplicationDataOnceAsync(db);
        foreach (var order in await db.Orders.Where(order => !db.OrderStatusHistories.Any(history => history.OrderId == order.Id)).ToListAsync())
            db.OrderStatusHistories.Add(new OrderStatusHistory { OrderId = order.Id, OccurredAt = order.CreatedAt, Action = "Đã đặt hàng" });
        await db.SaveChangesAsync();
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
                ISBN = book.ISBN,
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
                ISBN = book.ISBN, CoverImage = book.CoverImage, Description = book.Description
            });
            db.Books.AddRange(newBooks);
            await db.SaveChangesAsync();
        }
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE book
            SET [ReviewCount] = (
                    SELECT COUNT(*) FROM [dbo].[BookReviews] r WHERE r.[BookId] = book.[Id]),
                [Rating] = COALESCE((
                    SELECT AVG(CAST(r.[Rating] AS float))
                    FROM [dbo].[BookReviews] r
                    WHERE r.[BookId] = book.[Id]), 0)
            FROM [dbo].[Books] book;
            """);
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
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
            }

        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        await RemoveObsoleteDemoUsersAsync(userManager);
        await CreateUserAsync(userManager, "admin1@gmail.com", "Admin123456", "Quản trị viên 1", "Admin", "0900000001");
        await CreateUserAsync(userManager, "admin2@gmail.com", "Admin123456", "Quản trị viên 2", "Admin", "0900000002");
        await CreateUserAsync(userManager, "khach1@gmail.com", "Khach123456", "Khách hàng mẫu", "Customer", "0910000001");
    }

    private static async Task RemoveObsoleteDemoUsersAsync(UserManager<AppUser> userManager)
    {
        var obsoleteEmails = new[]
        {
            "admin@latrang.vn", "customer@latrang.vn",
            "admin@groupnine.vn", "admin3@gmail.com",
            "customer@groupnine.vn",
            "khachhangmau1@gmail.com", "khachhangmau2@gmail.com", "khachhangmau3@gmail.com"
        };
        foreach (var email in obsoleteEmails)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
                continue;

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }

        var expectedAdmins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "admin1@gmail.com", "admin2@gmail.com"
        };
        foreach (var admin in await userManager.GetUsersInRoleAsync("Admin"))
        {
            if (admin.Email is not null && expectedAdmins.Contains(admin.Email))
                continue;

            var result = await userManager.RemoveFromRoleAsync(admin, "Admin");
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }

    private static async Task ResetApplicationDataOnceAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.AppDataReset', N'U') IS NULL
                CREATE TABLE [dbo].[AppDataReset] ([Id] int NOT NULL PRIMARY KEY, [AppliedAt] datetime2 NOT NULL);
            """);

        await using var transaction = await db.Database.BeginTransactionAsync();
        var resetApplied = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM [dbo].[AppDataReset] WHERE [Id] = {0}", 1).SingleAsync();
        if (resetApplied == 0)
        {
            await db.Database.ExecuteSqlRawAsync("""
                DELETE FROM [dbo].[BookReviews];
                DELETE FROM [dbo].[ReturnRequests];
                DELETE FROM [dbo].[CustomerFeedbacks];
                DELETE FROM [dbo].[OrderItems];
                DELETE FROM [dbo].[OrderStatusHistories];
                DELETE FROM [dbo].[Orders];
                DELETE FROM [dbo].[Books];
                DELETE FROM [dbo].[Categories];
                DELETE FROM [dbo].[AspNetUserClaims];
                DELETE FROM [dbo].[AspNetUserLogins];
                DELETE FROM [dbo].[AspNetUserTokens];
                DELETE FROM [dbo].[AspNetUserRoles];
                DELETE FROM [dbo].[AspNetRoleClaims];
                DELETE FROM [dbo].[AspNetUsers];
                DELETE FROM [dbo].[AspNetRoles];
                INSERT INTO [dbo].[AppDataReset] ([Id], [AppliedAt]) VALUES (1, SYSUTCDATETIME());
                """);
        }
        await transaction.CommitAsync();
    }

    private static async Task CreateUserAsync(
        UserManager<AppUser> userManager, string email, string password, string fullName, string role, string phone)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                PhoneNumber = phone,
                PhoneNumberConfirmed = true
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
        else
        {
            user.FullName = fullName;
            user.PhoneNumber = phone;
            user.PhoneNumberConfirmed = true;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }

        foreach (var currentRole in await userManager.GetRolesAsync(user))
        {
            if (currentRole == role)
                continue;

            var removeResult = await userManager.RemoveFromRoleAsync(user, currentRole);
            if (!removeResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", removeResult.Errors.Select(error => error.Description)));
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var result = await userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
