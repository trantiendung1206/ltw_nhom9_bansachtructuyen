/*
    GroupNine - khởi tạo database SQL Server

    Script này chỉ tạo database rỗng. Schema không được khai báo thủ công ở đây:
    ứng dụng ASP.NET Core sẽ gọi DbInitializer khi khởi động để chạy
    EnsureCreated, tạo các bảng Identity/nghiệp vụ, cập nhật schema cũ và seed
    dữ liệu mẫu. Giữ cách này đồng bộ với AppDbContext và mã khởi tạo hiện tại.

    Chạy bằng SQL Server Management Studio hoặc sqlcmd với tài khoản có quyền
    tạo database.
*/

DECLARE @DatabaseExists bit = CASE WHEN DB_ID(N'GroupNine') IS NULL THEN 0 ELSE 1 END;
IF @DatabaseExists = 0
BEGIN
    CREATE DATABASE [GroupNine];
END
GO

USE [GroupNine];
GO

SELECT
    DB_NAME() AS [DatabaseName],
    N'Database đã sẵn sàng; chạy ứng dụng để tạo schema và dữ liệu mẫu' AS [Status];
GO
