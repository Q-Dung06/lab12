# Lab12: Master-Detail, SqlTransaction và Hoàn thiện Mô hình 3 Lớp

## Thông tin sinh viên
- **Họ và tên**: Nguyễn Quang Dũng
- **Mã sinh viên**: 24003351
- **Học phần**: Lập trình .NET (IT3212)
- **Cơ sở dữ liệu**: `QLSinhVien` (SQL Server)

## Yêu cầu môi trường
- Windows 10/11
- Visual Studio 2022 hoặc .NET 10 SDK
- SQL Server Express với instance mặc định `.\SQLEXPRESS` (hoặc cấu hình chuỗi kết nối trong `Lab12_StoredProcedureCRUD\App.config`)

## Hướng dẫn chạy thử nghiệm
1. Mở `Lab12_MasterDetail_Transaction.sln` (hoặc `.slnx`) trong Visual Studio.
2. Đặt `Lab12_StoredProcedureCRUD` làm Startup Project.
3. Nhấn **F5** để khởi chạy ứng dụng.
4. Lần đầu chạy, nhấn nút **"Chuẩn bị CSDL"** ở góc trên bên phải để tạo/cập nhật cơ sở dữ liệu `QLSinhVien`, thêm cột `RowVersion`, bảng `LichSuChuyenKhoa`, học phần và các Stored Procedure cần thiết.
5. Trải nghiệm 6 tab tính năng:
   - **Tab Bài 1**: Master-Detail Khoa – Sinh viên (chọn khoa ở lưới trên, xem danh sách sinh viên tương ứng ở lưới dưới).
   - **Tab Bài 2, 5**: Master-Detail Sinh viên – Đăng ký học phần (xem học phần của sinh viên, thêm/hủy đăng ký học phần).
   - **Tab Bài 3, 4**: Thêm sinh viên cùng các học phần đăng ký ban đầu trong 1 SqlTransaction (Commit) và kích hoạt Demo Rollback (gây lỗi khóa ngoại và hoàn tác an toàn).
   - **Tab Bài 6–9**: Chuyển khoa sinh viên có ghi lịch sử audit, Xóa khoa an toàn (chuyển toàn bộ SV trước khi xóa khoa), Cập nhật điểm hàng loạt Atomic và Kiểm soát xung đột phiên bản `RowVersion`.
   - **Tab Bài 10 (Sinh viên & Khoa)**: Quản lý CRUD hoàn chỉnh theo mô hình 3 lớp.

## Cấu trúc kiến trúc 3 lớp
- `Lab12.DTO`: Các Data Transfer Object (`StudentDto`, `FacultyDto`, `CourseDto`, `EnrollmentDto`, `GradeChange`, `RollbackDemoResult`).
- `Lab12.DAL`: Tầng dữ liệu kết nối SQL Server, thực thi Stored Procedure có tham số, quản lý `SqlTransaction` và `SqlErrorTranslator`.
- `Lab12.BUS`: Tầng nghiệp vụ kiểm tra logic, ràng buộc toàn vẹn và điều phối giao dịch (`AcademicService`, `StudentService`, `FacultyService`, `DatabaseSetupService`).
- `Lab12_StoredProcedureCRUD`: Giao diện người dùng WinForms phân chia theo các nghiệp vụ trong đề bài.
- `Database`: 4 kịch bản SQL an toàn và hỗ trợ chạy lặp lại (`01_QLSinhVien.sql`, `02_SeedForPaging.sql`, `03_Lab12_StoredProcedures.sql`, `04_Lab12_MasterDetailTransactions.sql`).

## Sản phẩm đính kèm
- File báo cáo đầy đủ: `24003351_NguyenQuangDung_Lab12.docx`
- File mã nguồn đóng gói: `24003351_NguyenQuangDung_SourceCode_Lab12.zip`
