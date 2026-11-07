/*
    GroupNine - khởi tạo database SQL Server

    Script này chỉ tạo database rỗng. Schema không được khai báo thủ công ở đây:
    ứng dụng ASP.NET Core sẽ gọi DbInitializer khi khởi động để chạy
    EnsureCreated, tạo các bảng Identity/nghiệp vụ, cập nhật schema cũ và seed
    dữ liệu mẫu. Giữ cách này đồng bộ với AppDbContext và mã khởi tạo hiện tại.

    Chạy bằng SQL Server Management Studio hoặc sqlcmd với tài khoản có quyền
    tạo database.
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
    N'Đã tạo database; chạy ứng dụng để tạo schema và dữ liệu mẫu' AS [Status];
GO
