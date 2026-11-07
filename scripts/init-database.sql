/*
    Lật Trang - khởi tạo SQL Server
    Chạy bằng SQL Server Management Studio với tài khoản có quyền tạo database.
    Sau đó chạy ứng dụng; DbInitializer sẽ tạo schema Identity, sách, đơn hàng,
    wishlist, đổi trả, góp ý và dữ liệu mẫu bằng EnsureCreated.
*/

IF DB_ID(N'LatTrangBookStore') IS NULL
BEGIN
    CREATE DATABASE [LatTrangBookStore];
END
GO

USE [LatTrangBookStore];
GO

SELECT
    DB_NAME() AS [DatabaseName],
    N'Đã sẵn sàng cho ứng dụng ASP.NET Core' AS [Status];
GO
