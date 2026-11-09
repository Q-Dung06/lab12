using System.Data.SqlClient;

namespace Lab12.DAL;

public static class SqlErrorTranslator
{
    public static DalException Translate(SqlException exception)
    {
        var message = exception.Number switch
        {
            50001 => "Mã sinh viên đã tồn tại.",
            50002 => exception.Message,
            50011 => "Mã khoa đã tồn tại.",
            50012 => "Tên khoa đã tồn tại.",
            50013 => "Không tìm thấy khoa.",
            50021 => "Không thể xóa khoa đang có sinh viên.",
            50031 => "Sinh viên đã được người khác cập nhật. Tải lại dữ liệu trước khi sửa.",
            50041 => "Mã học phần đã tồn tại.",
            50042 => "Không tìm thấy học phần.",
            50043 => "Không thể xóa học phần đang có đăng ký.",
            50051 => "Sinh viên đã đăng ký học phần này.",
            50052 => "Không tìm thấy đăng ký học phần.",
            2601 or 2627 when exception.Message.Contains("UX_SinhVien_Email", StringComparison.OrdinalIgnoreCase)
                => "Email đã được sử dụng bởi sinh viên khác.",
            2601 or 2627 when exception.Message.Contains("UQ_Khoa_TenKhoa", StringComparison.OrdinalIgnoreCase)
                => "Tên khoa đã tồn tại.",
            2601 or 2627 => "Dữ liệu bị trùng khóa hoặc giá trị duy nhất.",
            547 => "Không thể thực hiện vì dữ liệu đang được tham chiếu ở nơi khác.",
            _ => "Không thể truy cập hoặc cập nhật CSDL. Hãy kiểm tra kết nối và thử lại."
        };
        return new DalException(exception.Number, message, exception.ToString(), exception);
    }
}
