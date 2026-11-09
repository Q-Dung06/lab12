/* Lab12: rowversion, audit lịch sử và CRUD học phần/đăng ký. Chạy lại an toàn. */
USE QLSinhVien;
GO

IF COL_LENGTH(N'dbo.SinhVien', N'RowVersion') IS NULL
    ALTER TABLE dbo.SinhVien ADD RowVersion ROWVERSION;
GO

IF OBJECT_ID(N'dbo.LichSuChuyenKhoa', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichSuChuyenKhoa
    (
        MaLichSu BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LichSuChuyenKhoa PRIMARY KEY,
        MaSV VARCHAR(15) NOT NULL,
        MaKhoaCu VARCHAR(10) NOT NULL,
        MaKhoaMoi VARCHAR(10) NOT NULL,
        NgayChuyen DATETIME2(0) NOT NULL CONSTRAINT DF_LichSuChuyenKhoa_Ngay DEFAULT (SYSDATETIME()),
        LyDo NVARCHAR(200) NOT NULL
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SinhVien_GetAll @IncludeInactive BIT=0 AS
BEGIN
    SET NOCOUNT ON;
    SELECT sv.MaSV,sv.HoTen,sv.NgaySinh,sv.GioiTinh,sv.MaKhoa,k.TenKhoa,sv.DiemTB,sv.Email,sv.IsActive,sv.RowVersion
    FROM dbo.SinhVien sv JOIN dbo.Khoa k ON k.MaKhoa=sv.MaKhoa
    WHERE @IncludeInactive=1 OR sv.IsActive=1 ORDER BY sv.MaSV;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SinhVien_Search
    @TuKhoa NVARCHAR(100)=NULL,@MaKhoa VARCHAR(10)=NULL,@IncludeInactive BIT=0
AS
BEGIN
    SET NOCOUNT ON;
    SET @TuKhoa=NULLIF(LTRIM(RTRIM(@TuKhoa)),N''); SET @MaKhoa=NULLIF(LTRIM(RTRIM(@MaKhoa)),'');
    SELECT sv.MaSV,sv.HoTen,sv.NgaySinh,sv.GioiTinh,sv.MaKhoa,k.TenKhoa,sv.DiemTB,sv.Email,sv.IsActive,sv.RowVersion
    FROM dbo.SinhVien sv JOIN dbo.Khoa k ON k.MaKhoa=sv.MaKhoa
    WHERE (@IncludeInactive=1 OR sv.IsActive=1)
      AND (@TuKhoa IS NULL OR sv.MaSV LIKE '%'+@TuKhoa+'%' OR sv.HoTen LIKE N'%'+@TuKhoa+N'%')
      AND (@MaKhoa IS NULL OR sv.MaKhoa=@MaKhoa)
    ORDER BY sv.MaSV;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SinhVien_Update
    @MaSV VARCHAR(15),@HoTen NVARCHAR(100),@NgaySinh DATE,@GioiTinh BIT,
    @MaKhoa VARCHAR(10),@DiemTB DECIMAL(4,2),@Email VARCHAR(100)=NULL,
    @OriginalRowVersion BINARY(8),@RowsAffected INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.SinhVien SET HoTen=@HoTen,NgaySinh=@NgaySinh,GioiTinh=@GioiTinh,
        MaKhoa=@MaKhoa,DiemTB=@DiemTB,Email=@Email
    WHERE MaSV=@MaSV AND IsActive=1 AND RowVersion=@OriginalRowVersion;
    SET @RowsAffected=@@ROWCOUNT;
    IF @RowsAffected=0
    BEGIN
        IF EXISTS(SELECT 1 FROM dbo.SinhVien WHERE MaSV=@MaSV AND IsActive=1)
            THROW 50031,N'Sinh viên đã được người khác cập nhật. Tải lại dữ liệu trước khi sửa.',1;
        THROW 50002,N'Không tìm thấy sinh viên đang hoạt động.',1;
    END;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_HocPhan_GetAll AS
BEGIN SET NOCOUNT ON; SELECT MaHP,TenHP,SoTinChi FROM dbo.HocPhan ORDER BY MaHP; END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_HocPhan_Insert @MaHP VARCHAR(10),@TenHP NVARCHAR(120),@SoTinChi INT,@RowsAffected INT OUTPUT AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM dbo.HocPhan WHERE MaHP=@MaHP) THROW 50041,N'Mã học phần đã tồn tại.',1;
 INSERT dbo.HocPhan(MaHP,TenHP,SoTinChi) VALUES(@MaHP,@TenHP,@SoTinChi); SET @RowsAffected=@@ROWCOUNT;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_HocPhan_Update @MaHP VARCHAR(10),@TenHP NVARCHAR(120),@SoTinChi INT,@RowsAffected INT OUTPUT AS
BEGIN
 SET NOCOUNT ON;
 UPDATE dbo.HocPhan SET TenHP=@TenHP,SoTinChi=@SoTinChi WHERE MaHP=@MaHP; SET @RowsAffected=@@ROWCOUNT;
 IF @RowsAffected=0 THROW 50042,N'Không tìm thấy học phần.',1;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_HocPhan_Delete @MaHP VARCHAR(10),@RowsAffected INT OUTPUT AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM dbo.DangKy WHERE MaHP=@MaHP) THROW 50043,N'Không thể xóa học phần đang có đăng ký.',1;
 DELETE dbo.HocPhan WHERE MaHP=@MaHP; SET @RowsAffected=@@ROWCOUNT;
 IF @RowsAffected=0 THROW 50042,N'Không tìm thấy học phần.',1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_DangKy_GetByStudent @MaSV VARCHAR(15) AS
BEGIN
 SET NOCOUNT ON;
 SELECT dk.MaSV,dk.MaHP,hp.TenHP,hp.SoTinChi,dk.HocKy,dk.Diem,dk.NgayDangKy
 FROM dbo.DangKy dk JOIN dbo.HocPhan hp ON hp.MaHP=dk.MaHP
 WHERE dk.MaSV=@MaSV ORDER BY dk.HocKy,hp.TenHP;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_DangKy_Insert @MaSV VARCHAR(15),@MaHP VARCHAR(10),@HocKy INT,@Diem DECIMAL(4,2)=NULL,@RowsAffected INT OUTPUT AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM dbo.DangKy WHERE MaSV=@MaSV AND MaHP=@MaHP) THROW 50051,N'Sinh viên đã đăng ký học phần này.',1;
 INSERT dbo.DangKy(MaSV,MaHP,HocKy,Diem) VALUES(@MaSV,@MaHP,@HocKy,@Diem); SET @RowsAffected=@@ROWCOUNT;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_DangKy_Update @MaSV VARCHAR(15),@MaHP VARCHAR(10),@HocKy INT,@Diem DECIMAL(4,2)=NULL,@RowsAffected INT OUTPUT AS
BEGIN
 SET NOCOUNT ON;
 UPDATE dbo.DangKy SET HocKy=@HocKy,Diem=@Diem WHERE MaSV=@MaSV AND MaHP=@MaHP; SET @RowsAffected=@@ROWCOUNT;
 IF @RowsAffected=0 THROW 50052,N'Không tìm thấy đăng ký.',1;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_DangKy_Delete @MaSV VARCHAR(15),@MaHP VARCHAR(10),@RowsAffected INT OUTPUT AS
BEGIN
 SET NOCOUNT ON; DELETE dbo.DangKy WHERE MaSV=@MaSV AND MaHP=@MaHP; SET @RowsAffected=@@ROWCOUNT;
 IF @RowsAffected=0 THROW 50052,N'Không tìm thấy đăng ký.',1;
END;
GO
