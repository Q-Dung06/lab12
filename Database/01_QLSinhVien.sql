/*
    Lab12 - Chuẩn bị CSDL QLSinhVien và dữ liệu nền
    Có thể chạy toàn bộ script trên SQL Server 2019/2022 hoặc SQL Server Express.
    Script không xóa database hay dữ liệu có sẵn; mỗi dòng mẫu chỉ được thêm một lần.
*/
SET NOCOUNT ON;
GO

IF DB_ID(N'QLSinhVien') IS NULL
BEGIN
    CREATE DATABASE QLSinhVien;
END;
GO

USE QLSinhVien;
GO

IF OBJECT_ID(N'dbo.Khoa', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Khoa
    (
        MaKhoa VARCHAR(10) NOT NULL CONSTRAINT PK_Khoa PRIMARY KEY,
        TenKhoa NVARCHAR(100) NOT NULL CONSTRAINT UQ_Khoa_TenKhoa UNIQUE
    );
END;
GO

IF OBJECT_ID(N'dbo.SinhVien', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SinhVien
    (
        MaSV VARCHAR(15) NOT NULL CONSTRAINT PK_SinhVien PRIMARY KEY,
        HoTen NVARCHAR(100) NOT NULL,
        NgaySinh DATE NOT NULL,
        GioiTinh BIT NOT NULL,
        MaKhoa VARCHAR(10) NOT NULL,
        DiemTB DECIMAL(4,2) NOT NULL CONSTRAINT DF_SinhVien_DiemTB DEFAULT (0),
        Email VARCHAR(100) NULL,
        CONSTRAINT CK_SinhVien_Diem CHECK (DiemTB BETWEEN 0 AND 10),
        CONSTRAINT FK_SinhVien_Khoa FOREIGN KEY (MaKhoa) REFERENCES dbo.Khoa(MaKhoa)
    );
END;
GO

/* UNIQUE Email: cho phép nhiều giá trị NULL nhưng không cho trùng email đã nhập. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SinhVien') AND name = N'UX_SinhVien_Email')
BEGIN
    CREATE UNIQUE INDEX UX_SinhVien_Email ON dbo.SinhVien(Email) WHERE Email IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.HocPhan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HocPhan
    (
        MaHP VARCHAR(10) NOT NULL CONSTRAINT PK_HocPhan PRIMARY KEY,
        TenHP NVARCHAR(120) NOT NULL,
        SoTinChi INT NOT NULL,
        CONSTRAINT CK_HocPhan_SoTinChi CHECK (SoTinChi BETWEEN 1 AND 6)
    );
END;
GO

IF OBJECT_ID(N'dbo.DangKy', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DangKy
    (
        MaSV VARCHAR(15) NOT NULL,
        MaHP VARCHAR(10) NOT NULL,
        HocKy INT NOT NULL,
        Diem DECIMAL(4,2) NULL,
        NgayDangKy DATE NOT NULL CONSTRAINT DF_DangKy_NgayDangKy DEFAULT (CONVERT(DATE, GETDATE())),
        CONSTRAINT PK_DangKy PRIMARY KEY (MaSV, MaHP),
        CONSTRAINT CK_DangKy_HocKy CHECK (HocKy BETWEEN 1 AND 3),
        CONSTRAINT CK_DangKy_Diem CHECK (Diem IS NULL OR Diem BETWEEN 0 AND 10),
        CONSTRAINT FK_DangKy_SinhVien FOREIGN KEY (MaSV) REFERENCES dbo.SinhVien(MaSV),
        CONSTRAINT FK_DangKy_HocPhan FOREIGN KEY (MaHP) REFERENCES dbo.HocPhan(MaHP)
    );
END;
GO

INSERT INTO dbo.Khoa(MaKhoa, TenKhoa)
SELECT v.MaKhoa, v.TenKhoa
FROM (VALUES
    ('CNTT', N'Công nghệ thông tin'),
    ('OTO', N'Công nghệ kỹ thuật ô tô'),
    ('KT', N'Kinh tế')
) AS v(MaKhoa, TenKhoa)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Khoa AS k WHERE k.MaKhoa = v.MaKhoa);
GO

INSERT INTO dbo.SinhVien(MaSV, HoTen, NgaySinh, GioiTinh, MaKhoa, DiemTB, Email)
SELECT v.MaSV, v.HoTen, v.NgaySinh, v.GioiTinh, v.MaKhoa, v.DiemTB, v.Email
FROM (VALUES
    ('SV001', N'Nguyễn Văn An',     CONVERT(DATE,'2005-02-15'), CONVERT(BIT,1), 'CNTT', CONVERT(DECIMAL(4,2),8.20), 'an@eaut.edu.vn'),
    ('SV002', N'Trần Thị Bình',     CONVERT(DATE,'2005-07-21'), CONVERT(BIT,0), 'CNTT', CONVERT(DECIMAL(4,2),7.40), 'binh@eaut.edu.vn'),
    ('SV003', N'Lê Văn Cường',      CONVERT(DATE,'2004-12-03'), CONVERT(BIT,1), 'OTO',  CONVERT(DECIMAL(4,2),6.80), 'cuong@eaut.edu.vn'),
    ('SV004', N'Phạm Thị Dung',     CONVERT(DATE,'2005-04-18'), CONVERT(BIT,0), 'KT',   CONVERT(DECIMAL(4,2),8.90), 'dung@eaut.edu.vn'),
    ('SV005', N'Hoàng Văn Đức',     CONVERT(DATE,'2004-09-10'), CONVERT(BIT,1), 'CNTT', CONVERT(DECIMAL(4,2),7.75), 'duc@eaut.edu.vn'),
    ('SV006', N'Vũ Thị Giang',      CONVERT(DATE,'2005-11-27'), CONVERT(BIT,0), 'OTO',  CONVERT(DECIMAL(4,2),9.10), 'giang@eaut.edu.vn'),
    ('SV007', N'Đặng Minh Hải',     CONVERT(DATE,'2005-01-08'), CONVERT(BIT,1), 'KT',   CONVERT(DECIMAL(4,2),6.50), 'hai@eaut.edu.vn'),
    ('SV008', N'Bùi Thị Hương',     CONVERT(DATE,'2004-06-30'), CONVERT(BIT,0), 'CNTT', CONVERT(DECIMAL(4,2),8.00), 'huong@eaut.edu.vn'),
    ('SV009', N'Đỗ Quang Khánh',    CONVERT(DATE,'2005-03-14'), CONVERT(BIT,1), 'OTO',  CONVERT(DECIMAL(4,2),7.20), 'khanh@eaut.edu.vn'),
    ('SV010', N'Nguyễn Thị Lan',    CONVERT(DATE,'2005-08-05'), CONVERT(BIT,0), 'KT',   CONVERT(DECIMAL(4,2),8.45), 'lan@eaut.edu.vn')
) AS v(MaSV, HoTen, NgaySinh, GioiTinh, MaKhoa, DiemTB, Email)
WHERE NOT EXISTS (SELECT 1 FROM dbo.SinhVien AS sv WHERE sv.MaSV = v.MaSV);
GO

INSERT INTO dbo.HocPhan(MaHP, TenHP, SoTinChi)
SELECT v.MaHP, v.TenHP, v.SoTinChi
FROM (VALUES
    ('HP001', N'Lập trình .NET', 3),
    ('HP002', N'Cơ sở dữ liệu', 3),
    ('HP003', N'Cấu trúc dữ liệu và giải thuật', 4),
    ('HP004', N'Phân tích thiết kế hệ thống', 3),
    ('HP005', N'Tiếng Anh chuyên ngành', 2)
) AS v(MaHP, TenHP, SoTinChi)
WHERE NOT EXISTS (SELECT 1 FROM dbo.HocPhan AS hp WHERE hp.MaHP = v.MaHP);
GO

/* Không truyền NgayDangKy để kiểm tra DEFAULT tự điền ngày hiện tại. */
INSERT INTO dbo.DangKy(MaSV, MaHP, HocKy, Diem)
SELECT v.MaSV, v.MaHP, v.HocKy, v.Diem
FROM (VALUES
    ('SV001','HP001',1,CONVERT(DECIMAL(4,2),8.50)), ('SV001','HP002',1,CONVERT(DECIMAL(4,2),7.90)),
    ('SV002','HP001',1,CONVERT(DECIMAL(4,2),7.00)), ('SV002','HP003',2,CONVERT(DECIMAL(4,2),8.10)),
    ('SV003','HP002',1,CONVERT(DECIMAL(4,2),6.75)), ('SV003','HP004',2,CONVERT(DECIMAL(4,2),7.20)),
    ('SV004','HP005',1,CONVERT(DECIMAL(4,2),9.00)), ('SV004','HP002',2,CONVERT(DECIMAL(4,2),8.40)),
    ('SV005','HP001',1,CONVERT(DECIMAL(4,2),7.80)), ('SV006','HP003',2,CONVERT(DECIMAL(4,2),9.20)),
    ('SV007','HP004',1,CONVERT(DECIMAL(4,2),6.80)), ('SV008','HP005',2,CONVERT(DECIMAL(4,2),8.25)),
    ('SV009','HP002',1,CONVERT(DECIMAL(4,2),7.15)), ('SV010','HP004',2,CONVERT(DECIMAL(4,2),8.70)),
    ('SV010','HP005',2,CONVERT(DECIMAL(4,2),8.10))
) AS v(MaSV, MaHP, HocKy, Diem)
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.DangKy AS dk
    WHERE dk.MaSV = v.MaSV AND dk.MaHP = v.MaHP
);
GO

PRINT N'Đã tạo/kiểm tra QLSinhVien và nạp tối thiểu 3 khoa, 10 sinh viên, 5 học phần cùng dữ liệu đăng ký.';
SELECT N'Khoa' AS Bang, COUNT(*) AS SoLuong FROM dbo.Khoa
UNION ALL SELECT N'SinhVien', COUNT(*) FROM dbo.SinhVien
UNION ALL SELECT N'HocPhan', COUNT(*) FROM dbo.HocPhan
UNION ALL SELECT N'DangKy', COUNT(*) FROM dbo.DangKy;
GO

SELECT sv.MaSV, sv.HoTen, sv.NgaySinh, sv.GioiTinh, k.TenKhoa, sv.DiemTB, sv.Email
FROM dbo.SinhVien AS sv
INNER JOIN dbo.Khoa AS k ON k.MaKhoa = sv.MaKhoa
ORDER BY sv.MaSV;
GO


