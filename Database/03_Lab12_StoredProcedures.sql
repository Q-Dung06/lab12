/* Lab12: migration va stored procedure CRUD cho QLSinhVien. Co the chay lai an toan. */
USE QLSinhVien;
GO

IF COL_LENGTH(N'dbo.SinhVien', N'IsActive') IS NULL
BEGIN
    ALTER TABLE dbo.SinhVien ADD IsActive BIT NOT NULL
        CONSTRAINT DF_SinhVien_IsActive DEFAULT (1) WITH VALUES;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SinhVien') AND name=N'UX_SinhVien_Email')
BEGIN
    CREATE UNIQUE INDEX UX_SinhVien_Email ON dbo.SinhVien(Email) WHERE Email IS NOT NULL;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SinhVien_GetAll
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT sv.MaSV, sv.HoTen, sv.NgaySinh, sv.GioiTinh, sv.MaKhoa,
           k.TenKhoa, sv.DiemTB, sv.Email, sv.IsActive
    FROM dbo.SinhVien AS sv
    INNER JOIN dbo.Khoa AS k ON k.MaKhoa=sv.MaKhoa
    WHERE @IncludeInactive=1 OR sv.IsActive=1
    ORDER BY sv.MaSV;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SinhVien_Search
    @TuKhoa NVARCHAR(100) = NULL,
    @MaKhoa VARCHAR(10) = NULL,
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET @TuKhoa = NULLIF(LTRIM(RTRIM(@TuKhoa)), N'');
    SET @MaKhoa = NULLIF(LTRIM(RTRIM(@MaKhoa)), '');
    SELECT sv.MaSV, sv.HoTen, sv.NgaySinh, sv.GioiTinh, sv.MaKhoa,
           k.TenKhoa, sv.DiemTB, sv.Email, sv.IsActive
    FROM dbo.SinhVien AS sv
    INNER JOIN dbo.Khoa AS k ON k.MaKhoa=sv.MaKhoa
    WHERE (@IncludeInactive=1 OR sv.IsActive=1)
      AND (@TuKhoa IS NULL OR sv.MaSV LIKE '%' + @TuKhoa + '%' OR sv.HoTen LIKE N'%' + @TuKhoa + N'%')
      AND (@MaKhoa IS NULL OR sv.MaKhoa=@MaKhoa)
    ORDER BY sv.MaSV;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SinhVien_Insert
    @MaSV VARCHAR(15), @HoTen NVARCHAR(100), @NgaySinh DATE,
    @GioiTinh BIT, @MaKhoa VARCHAR(10), @DiemTB DECIMAL(4,2),
    @Email VARCHAR(100)=NULL, @RowsAffected INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.SinhVien WHERE MaSV=@MaSV)
        THROW 50001, N'Mã sinh viên đã tồn tại.', 1;
    INSERT INTO dbo.SinhVien(MaSV,HoTen,NgaySinh,GioiTinh,MaKhoa,DiemTB,Email,IsActive)
    VALUES(@MaSV,@HoTen,@NgaySinh,@GioiTinh,@MaKhoa,@DiemTB,@Email,1);
    SET @RowsAffected=@@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SinhVien_Update
    @MaSV VARCHAR(15), @HoTen NVARCHAR(100), @NgaySinh DATE,
    @GioiTinh BIT, @MaKhoa VARCHAR(10), @DiemTB DECIMAL(4,2),
    @Email VARCHAR(100)=NULL, @RowsAffected INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.SinhVien
    SET HoTen=@HoTen, NgaySinh=@NgaySinh, GioiTinh=@GioiTinh,
        MaKhoa=@MaKhoa, DiemTB=@DiemTB, Email=@Email
    WHERE MaSV=@MaSV AND IsActive=1;
    SET @RowsAffected=@@ROWCOUNT;
    IF @RowsAffected=0 THROW 50002, N'Không tìm thấy sinh viên đang hoạt động.', 1;
END;
GO

/* Bài 9: xóa mềm, giữ khóa và lịch sử để có thể khôi phục. */
CREATE OR ALTER PROCEDURE dbo.sp_SinhVien_Delete
    @MaSV VARCHAR(15), @RowsAffected INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.SinhVien SET IsActive=0 WHERE MaSV=@MaSV AND IsActive=1;
    SET @RowsAffected=@@ROWCOUNT;
    IF @RowsAffected=0 THROW 50002, N'Không tìm thấy sinh viên đang hoạt động.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SinhVien_Restore
    @MaSV VARCHAR(15), @RowsAffected INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.SinhVien SET IsActive=1 WHERE MaSV=@MaSV AND IsActive=0;
    SET @RowsAffected=@@ROWCOUNT;
    IF @RowsAffected=0 THROW 50002, N'Không tìm thấy sinh viên đã xóa.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Khoa_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT k.MaKhoa,k.TenKhoa,COUNT(sv.MaSV) AS SoSinhVien
    FROM dbo.Khoa AS k LEFT JOIN dbo.SinhVien AS sv ON sv.MaKhoa=k.MaKhoa
    GROUP BY k.MaKhoa,k.TenKhoa ORDER BY k.MaKhoa;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Khoa_Insert
    @MaKhoa VARCHAR(10), @TenKhoa NVARCHAR(100), @RowsAffected INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM dbo.Khoa WHERE MaKhoa=@MaKhoa)
        THROW 50011, N'Mã khoa đã tồn tại.', 1;
    IF EXISTS(SELECT 1 FROM dbo.Khoa WHERE TenKhoa=@TenKhoa)
        THROW 50012, N'Tên khoa đã tồn tại.', 1;
    INSERT dbo.Khoa(MaKhoa,TenKhoa) VALUES(@MaKhoa,@TenKhoa);
    SET @RowsAffected=@@ROWCOUNT;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Khoa_Update
    @MaKhoa VARCHAR(10), @TenKhoa NVARCHAR(100), @RowsAffected INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Khoa SET TenKhoa=@TenKhoa WHERE MaKhoa=@MaKhoa;
    SET @RowsAffected=@@ROWCOUNT;
    IF @RowsAffected=0 THROW 50013, N'Không tìm thấy khoa.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Khoa_Delete
    @MaKhoa VARCHAR(10), @RowsAffected INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Khoa WHERE MaKhoa=@MaKhoa)
        THROW 50013, N'Không tìm thấy khoa.', 1;
    IF EXISTS(SELECT 1 FROM dbo.SinhVien WHERE MaKhoa=@MaKhoa)
        THROW 50021, N'Không thể xóa khoa đang có sinh viên.', 1;
    DELETE dbo.Khoa WHERE MaKhoa=@MaKhoa;
    SET @RowsAffected=@@ROWCOUNT;
END;
GO
