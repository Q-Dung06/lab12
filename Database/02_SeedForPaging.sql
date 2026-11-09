/* Lab12: nâng dữ liệu sinh viên lên tối thiểu 30 bản ghi để demo đủ 3 trang. */
USE QLSinhVien;
GO

INSERT INTO dbo.SinhVien(MaSV, HoTen, NgaySinh, GioiTinh, MaKhoa, DiemTB, Email)
SELECT v.MaSV, v.HoTen, v.NgaySinh, v.GioiTinh, v.MaKhoa, v.DiemTB, v.Email
FROM (VALUES
    ('SV011', N'Nguyễn Hải Nam',       CONVERT(DATE,'2004-03-12'), CONVERT(BIT,1), 'CNTT', CONVERT(DECIMAL(4,2),4.25), 'sv011@eaut.edu.vn'),
    ('SV012', N'Trần Ngọc Mai',        CONVERT(DATE,'2005-07-09'), CONVERT(BIT,0), 'CNTT', CONVERT(DECIMAL(4,2),8.75), 'sv012@eaut.edu.vn'),
    ('SV013', N'Phạm Quốc Bảo',        CONVERT(DATE,'2003-11-25'), CONVERT(BIT,1), 'OTO',  CONVERT(DECIMAL(4,2),6.10), 'sv013@eaut.edu.vn'),
    ('SV014', N'Lê Thu Trang',         CONVERT(DATE,'2006-01-18'), CONVERT(BIT,0), 'KT',   CONVERT(DECIMAL(4,2),9.20), 'sv014@eaut.edu.vn'),
    ('SV015', N'Đỗ Minh Quân',         CONVERT(DATE,'2004-08-02'), CONVERT(BIT,1), 'CNTT', CONVERT(DECIMAL(4,2),3.80), 'sv015@eaut.edu.vn'),
    ('SV016', N'Võ Khánh Linh',        CONVERT(DATE,'2005-12-20'), CONVERT(BIT,0), 'OTO',  CONVERT(DECIMAL(4,2),8.05), 'sv016@eaut.edu.vn'),
    ('SV017', N'Bùi Đức Thịnh',        CONVERT(DATE,'2003-05-14'), CONVERT(BIT,1), 'KT',   CONVERT(DECIMAL(4,2),5.50), 'sv017@eaut.edu.vn'),
    ('SV018', N'Ngô Phương Anh',       CONVERT(DATE,'2006-09-30'), CONVERT(BIT,0), 'CNTT', CONVERT(DECIMAL(4,2),7.30), 'sv018@eaut.edu.vn'),
    ('SV019', N'Đặng Gia Huy',         CONVERT(DATE,'2004-02-07'), CONVERT(BIT,1), 'OTO',  CONVERT(DECIMAL(4,2),2.90), 'sv019@eaut.edu.vn'),
    ('SV020', N'Hoàng Bảo Ngọc',       CONVERT(DATE,'2005-10-11'), CONVERT(BIT,0), 'KT',   CONVERT(DECIMAL(4,2),8.60), 'sv020@eaut.edu.vn'),
    ('SV021', N'Phan Tuấn Kiệt',       CONVERT(DATE,'2003-06-22'), CONVERT(BIT,1), 'CNTT', CONVERT(DECIMAL(4,2),6.75), 'sv021@eaut.edu.vn'),
    ('SV022', N'Vũ Mỹ Duyên',          CONVERT(DATE,'2006-04-04'), CONVERT(BIT,0), 'OTO',  CONVERT(DECIMAL(4,2),9.50), 'sv022@eaut.edu.vn'),
    ('SV023', N'Đinh Thành Công',      CONVERT(DATE,'2004-12-16'), CONVERT(BIT,1), 'KT',   CONVERT(DECIMAL(4,2),4.90), 'sv023@eaut.edu.vn'),
    ('SV024', N'Nguyễn Hoài Thương',   CONVERT(DATE,'2005-02-28'), CONVERT(BIT,0), 'CNTT', CONVERT(DECIMAL(4,2),7.95), 'sv024@eaut.edu.vn'),
    ('SV025', N'Trương Anh Tuấn',      CONVERT(DATE,'2003-09-06'), CONVERT(BIT,1), 'OTO',  CONVERT(DECIMAL(4,2),5.25), 'sv025@eaut.edu.vn'),
    ('SV026', N'Phạm Ngọc Hà',         CONVERT(DATE,'2006-07-13'), CONVERT(BIT,0), 'KT',   CONVERT(DECIMAL(4,2),8.30), 'sv026@eaut.edu.vn'),
    ('SV027', N'Lý Minh Nhật',         CONVERT(DATE,'2004-04-19'), CONVERT(BIT,1), 'CNTT', CONVERT(DECIMAL(4,2),1.75), 'sv027@eaut.edu.vn'),
    ('SV028', N'Đào Thanh Tâm',        CONVERT(DATE,'2005-11-02'), CONVERT(BIT,0), 'OTO',  CONVERT(DECIMAL(4,2),6.40), 'sv028@eaut.edu.vn'),
    ('SV029', N'Tạ Khả Vy',            CONVERT(DATE,'2003-01-27'), CONVERT(BIT,0), 'KT',   CONVERT(DECIMAL(4,2),9.00), 'sv029@eaut.edu.vn'),
    ('SV030', N'Mai Nhật Long',        CONVERT(DATE,'2006-10-08'), CONVERT(BIT,1), 'CNTT', CONVERT(DECIMAL(4,2),4.60), 'sv030@eaut.edu.vn')
) AS v(MaSV, HoTen, NgaySinh, GioiTinh, MaKhoa, DiemTB, Email)
WHERE NOT EXISTS (SELECT 1 FROM dbo.SinhVien sv WHERE sv.MaSV = v.MaSV);
GO

SELECT COUNT(*) AS TongSinhVien FROM dbo.SinhVien;
GO


