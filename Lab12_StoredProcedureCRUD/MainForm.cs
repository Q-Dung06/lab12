using System.Data;
using System.Globalization;
using Lab12.BUS;
using Lab12.DTO;

namespace Lab12_StoredProcedureCRUD;

public sealed class MainForm : Form
{
    private readonly StudentService _students = new();
    private readonly FacultyService _faculties = new();
    private readonly AcademicService _academic = new();
    private readonly DatabaseSetupService _setup = new();

    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };

    // --- Tab SinhVien controls ---
    private readonly DataGridView _studentGrid = MakeGrid();
    private readonly TextBox _searchText = new() { Width = 220, PlaceholderText = "Mã SV hoặc họ tên" };
    private readonly ComboBox _searchFaculty = new() { Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _includeInactive = new() { Text = "Hiện cả đã xóa mềm", AutoSize = true };
    private readonly TextBox _studentCode = new() { Width = 115, MaxLength = 15 };
    private readonly TextBox _studentName = new() { Width = 190, MaxLength = 100 };
    private readonly DateTimePicker _birthDate = new() { Width = 135, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
    private readonly RadioButton _male = new() { Text = "Nam", AutoSize = true, Checked = true };
    private readonly RadioButton _female = new() { Text = "Nữ", AutoSize = true };
    private readonly ComboBox _studentFaculty = new() { Width = 175, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _score = new() { Width = 90, Minimum = 0, Maximum = 10, DecimalPlaces = 2, Increment = .25M };
    private readonly TextBox _email = new() { Width = 190, MaxLength = 100 };
    private readonly Label _studentStatus = NewStatus();
    private string? _currentStudentCode;
    private bool _currentStudentActive;

    // --- Tab Khoa controls ---
    private readonly DataGridView _facultyGrid = MakeGrid();
    private readonly TextBox _facultyCode = new() { Width = 130, MaxLength = 10 };
    private readonly TextBox _facultyName = new() { Width = 260, MaxLength = 100 };
    private readonly Label _facultyStatus = NewStatus();
    private readonly Label _facultyCount = NewStatus();
    private DataTable? _facultyTable;
    private bool _currentFacultyEditing;

    // --- Tab Master-Detail Khoa - SV controls (Bài 1) ---
    private readonly DataGridView _mdFacultyGrid = MakeGrid();
    private readonly DataGridView _mdStudentGrid = MakeGrid();
    private readonly Label _mdFacultyTitle = new() { Text = "Chọn khoa để xem danh sách sinh viên:", AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
    private readonly Label _mdStudentTitle = new() { Text = "Danh sách sinh viên thuộc khoa: (Chưa chọn)", AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(25, 63, 110) };

    // --- Tab Master-Detail SV - Đăng Ký Học Phần controls (Bài 2, 5, 10) ---
    private readonly DataGridView _enrollStudentGrid = MakeGrid();
    private readonly DataGridView _enrollmentGrid = MakeGrid();
    private readonly Label _enrollStudentTitle = new() { Text = "1. Chọn sinh viên:", AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
    private readonly Label _enrollmentTitle = new() { Text = "2. Học phần đã đăng ký của sinh viên:", AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(25, 63, 110) };
    private readonly ComboBox _cboCourse = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _numSemester = new() { Width = 65, Minimum = 1, Maximum = 3, Value = 1 };
    private readonly NumericUpDown _numGrade = new() { Width = 75, Minimum = 0, Maximum = 10, DecimalPlaces = 2, Increment = 0.25M };
    private readonly CheckBox _chkHasGrade = new() { Text = "Có điểm", AutoSize = true, Checked = false };
    private readonly Label _enrollStatus = NewStatus();
    private string? _selectedEnrollStudentCode;

    // --- Tab Transaction & Rollback Demo controls (Bài 3, 4) ---
    private readonly TextBox _txStudentCode = new() { Width = 110, MaxLength = 15 };
    private readonly TextBox _txStudentName = new() { Width = 180, MaxLength = 100 };
    private readonly ComboBox _txFaculty = new() { Width = 175, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckedListBox _txCourseList = new() { Width = 360, Height = 130, CheckOnClick = true };
    private readonly TextBox _txLog = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.FromArgb(220, 220, 220), Font = new Font("Consolas", 9.5F) };

    // --- Tab Nâng cao: Chuyển khoa, Xóa an toàn, Cập nhật điểm, RowVersion (Bài 6-9) ---
    private readonly ComboBox _advStudent = new() { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _advTargetFaculty = new() { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _advTransferReason = new() { Width = 260, PlaceholderText = "Lý do chuyển khoa" };
    private readonly DataGridView _advHistoryGrid = MakeGrid();

    private readonly ComboBox _advDeleteSourceFaculty = new() { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _advDeleteTargetFaculty = new() { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };

    private readonly TextBox _advBatchGrades = new() { Width = 320, Height = 65, Multiline = true, ScrollBars = ScrollBars.Vertical, PlaceholderText = "Nhập mỗi dòng: MaSV,MaHP,Diem (VD: SV001,CS101,8.5)" };
    private readonly TextBox _advConflictStudent = new() { Width = 120, PlaceholderText = "Mã SV thử nghiệm" };

    // Common header status
    private readonly Label _connectionStatus = NewStatus();

    public MainForm()
    {
        Text = "Lab 12 - Master-Detail, SqlTransaction và Hoàn thiện Mô hình 3 Lớp";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1380, 920);
        MinimumSize = new Size(1160, 760);
        WindowState = FormWindowState.Normal;
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(245, 248, 252);
        BuildUi();
        Shown += (_, _) => RunUi(LoadInitialData, showValidationAsWarning: true);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(10), BackColor = BackColor };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        header.Controls.Add(new Label
        {
            Text = "LAB 12  |  Master-Detail, SqlTransaction và Hoàn thiện Kiến trúc 3 Lớp",
            Location = new Point(14, 7),
            AutoSize = true,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(25, 63, 110)
        });
        _connectionStatus.Location = new Point(16, 43);
        _connectionStatus.Text = "Đang nạp dữ liệu...";
        header.Controls.Add(_connectionStatus);

        var prepare = MakeButton("Chuẩn bị CSDL", Color.FromArgb(16, 129, 102), 145);
        prepare.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        prepare.Location = new Point(1190, 19);
        prepare.Click += (_, _) => RunUi(PrepareDatabase);
        header.Resize += (_, _) => prepare.Left = header.ClientSize.Width - prepare.Width - 16;
        header.Controls.Add(prepare);

        root.Controls.Add(header, 0, 0);

        _tabs.TabPages.Add(BuildMdFacultyStudentTab());       // Bài 1
        _tabs.TabPages.Add(BuildEnrollmentTab());              // Bài 2, 5
        _tabs.TabPages.Add(BuildTransactionTab());             // Bài 3, 4
        _tabs.TabPages.Add(BuildAdvancedTab());                // Bài 6, 7, 8, 9
        _tabs.TabPages.Add(BuildStudentTab());                 // Bài 10 (Quản lý Sinh viên)
        _tabs.TabPages.Add(BuildFacultyTab());                 // Bài 10 (Quản lý Khoa)

        root.Controls.Add(_tabs, 0, 1);
        Controls.Add(root);
    }

    #region Tab 1: Master-Detail Khoa - SinhVien (Bài 1)
    private TabPage BuildMdFacultyStudentTab()
    {
        var page = NewTab("Bài 1 | Master-Detail Khoa - Sinh viên");
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 240,
            BackColor = Color.FromArgb(215, 225, 238)
        };

        // Panel Master (Khoa)
        var topPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8) };
        _mdFacultyTitle.Dock = DockStyle.Top;
        topPanel.Controls.Add(_mdFacultyGrid);
        topPanel.Controls.Add(_mdFacultyTitle);
        _mdFacultyGrid.SelectionChanged += (_, _) => RunUi(OnMdFacultySelectionChanged, false);

        // Panel Detail (SinhVien)
        var bottomPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8) };
        _mdStudentTitle.Dock = DockStyle.Top;
        bottomPanel.Controls.Add(_mdStudentGrid);
        bottomPanel.Controls.Add(_mdStudentTitle);

        split.Panel1.Controls.Add(topPanel);
        split.Panel2.Controls.Add(bottomPanel);
        page.Controls.Add(split);
        return page;
    }

    private void OnMdFacultySelectionChanged()
    {
        if (_mdFacultyGrid.CurrentRow?.DataBoundItem is not DataRowView row) return;
        var maKhoa = Convert.ToString(row["MaKhoa"]);
        var tenKhoa = Convert.ToString(row["TenKhoa"]);
        if (string.IsNullOrEmpty(maKhoa)) return;

        var dt = _academic.StudentsByFaculty(maKhoa);
        _mdStudentGrid.DataSource = dt;
        _mdStudentTitle.Text = $"Danh sách sinh viên thuộc khoa: {maKhoa} - {tenKhoa} ({dt.Rows.Count} sinh viên)";
    }
    #endregion

    #region Tab 2: Master-Detail SinhVien - DangKy (Bài 2, 5)
    private TabPage BuildEnrollmentTab()
    {
        var page = NewTab("Bài 2, 5 | Sinh viên - Đăng ký học phần");
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 560,
            BackColor = Color.FromArgb(215, 225, 238)
        };

        // Panel trái: Chọn Sinh viên
        var left = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8) };
        _enrollStudentTitle.Dock = DockStyle.Top;
        left.Controls.Add(_enrollStudentGrid);
        left.Controls.Add(_enrollStudentTitle);
        _enrollStudentGrid.SelectionChanged += (_, _) => RunUi(OnEnrollStudentSelectionChanged, false);

        // Panel phải: Lưới Học phần đã đăng ký + Form thêm/xóa đăng ký
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.White, Padding = new Padding(8) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));

        right.Controls.Add(_enrollmentTitle, 0, 0);
        right.Controls.Add(_enrollmentGrid, 0, 1);

        // Form thêm đăng ký
        var editor = new GroupBox { Text = "Thêm / Xóa đăng ký học phần cho sinh viên đang chọn", Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(6) };
        var fields = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, Padding = new Padding(2) };
        AddField(fields, "Học phần", _cboCourse);
        AddField(fields, "Học kỳ", _numSemester);
        AddField(fields, "Điểm", _numGrade);
        fields.Controls.Add(_chkHasGrade);
        _chkHasGrade.CheckedChanged += (_, _) => _numGrade.Enabled = _chkHasGrade.Checked;
        _numGrade.Enabled = false;

        var btnAddEnroll = MakeButton("Thêm đăng ký", Color.FromArgb(16, 129, 102), 115);
        btnAddEnroll.Click += (_, _) => RunUi(AddEnrollment);
        fields.Controls.Add(btnAddEnroll);

        var btnDelEnroll = MakeButton("Hủy đăng ký", Color.FromArgb(166, 79, 70), 105);
        btnDelEnroll.Click += (_, _) => RunUi(DeleteEnrollment);
        fields.Controls.Add(btnDelEnroll);

        _enrollStatus.Padding = new Padding(6, 6, 0, 0);
        fields.Controls.Add(_enrollStatus);
        editor.Controls.Add(fields);
        right.Controls.Add(editor, 0, 2);

        split.Panel1.Controls.Add(left);
        split.Panel2.Controls.Add(right);
        page.Controls.Add(split);
        return page;
    }

    private void OnEnrollStudentSelectionChanged()
    {
        if (_enrollStudentGrid.CurrentRow?.DataBoundItem is not DataRowView row) return;
        _selectedEnrollStudentCode = Convert.ToString(row["MaSV"]);
        var hoTen = Convert.ToString(row["HoTen"]);
        if (string.IsNullOrEmpty(_selectedEnrollStudentCode)) return;

        var enrolls = _academic.Enrollments(_selectedEnrollStudentCode);
        _enrollmentGrid.DataSource = enrolls;
        _enrollmentTitle.Text = $"2. Học phần đã đăng ký của: {_selectedEnrollStudentCode} - {hoTen} ({enrolls.Rows.Count} học phần)";
        _enrollStatus.Text = "Sẵn sàng thêm/xóa học phần.";
    }

    private void AddEnrollment()
    {
        if (string.IsNullOrEmpty(_selectedEnrollStudentCode)) throw new BusinessRuleException("Chọn sinh viên trước khi thêm đăng ký.");
        if (_cboCourse.SelectedValue is not string maHP || string.IsNullOrEmpty(maHP)) throw new BusinessRuleException("Chọn học phần.");

        decimal? grade = _chkHasGrade.Checked ? _numGrade.Value : null;
        _academic.AddEnrollment(_selectedEnrollStudentCode, maHP, (int)_numSemester.Value, grade);
        OnEnrollStudentSelectionChanged();
        _enrollStatus.Text = $"Đã thêm học phần {maHP} thành công.";
    }

    private void DeleteEnrollment()
    {
        if (string.IsNullOrEmpty(_selectedEnrollStudentCode)) throw new BusinessRuleException("Chọn sinh viên.");
        if (_enrollmentGrid.CurrentRow?.DataBoundItem is not DataRowView row) throw new BusinessRuleException("Chọn đăng ký học phần cần xóa.");

        var maHP = Convert.ToString(row["MaHP"]);
        if (string.IsNullOrEmpty(maHP)) return;

        if (MessageBox.Show(this, $"Hủy đăng ký học phần {maHP} của sinh viên {_selectedEnrollStudentCode}?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        _academic.DeleteEnrollment(_selectedEnrollStudentCode, maHP);
        OnEnrollStudentSelectionChanged();
        _enrollStatus.Text = $"Đã hủy đăng ký học phần {maHP}.";
    }
    #endregion

    #region Tab 3: SqlTransaction & Demo Rollback (Bài 3, 4)
    private TabPage BuildTransactionTab()
    {
        var page = NewTab("Bài 3, 4 | SqlTransaction & Demo Rollback");
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 620,
            BackColor = Color.FromArgb(215, 225, 238)
        };

        // Panel trái: Thêm Sinh viên + Học phần ban đầu trong 1 Transaction
        var left = new GroupBox { Text = "Bài 3. Thêm sinh viên & đăng ký học phần ban đầu (SqlTransaction)", Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));

        var r1 = NewFieldRow();
        AddField(r1, "Mã SV", _txStudentCode);
        AddField(r1, "Họ tên", _txStudentName);
        content.Controls.Add(r1, 0, 0);

        var r2 = NewFieldRow();
        AddField(r2, "Khoa", _txFaculty);
        content.Controls.Add(r2, 0, 1);

        var r3 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        r3.Controls.Add(MakeLabel("Chọn các học phần đăng ký ban đầu (chọn ít nhất 1):"));
        r3.Controls.Add(_txCourseList);
        content.Controls.Add(r3, 0, 2);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var btnAddTx = MakeButton("3. Thực hiện Giao dịch (Commit)", Color.FromArgb(16, 129, 102), 220);
        btnAddTx.Click += (_, _) => RunUi(ExecuteAddStudentTransaction);
        actions.Controls.Add(btnAddTx);
        content.Controls.Add(actions, 0, 3);

        // Khối Demo Rollback (Bài 4)
        var rbBox = new GroupBox { Text = "Bài 4. Cố ý gây lỗi và kiểm chứng Rollback", Dock = DockStyle.Fill, BackColor = Color.FromArgb(253, 245, 245), Padding = new Padding(6) };
        var rbFlow = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var btnDemoRb = MakeButton("4. Kích hoạt Demo Rollback", Color.FromArgb(166, 79, 70), 195);
        btnDemoRb.Click += (_, _) => RunUi(ExecuteRollbackDemo);
        rbFlow.Controls.Add(btnDemoRb);
        rbFlow.Controls.Add(MakeLabel("Thêm sinh viên rồi cố ý chèn mã HP sai (FK vi phạm). Xác nhận Rollback!"));
        rbBox.Controls.Add(rbFlow);
        content.Controls.Add(rbBox, 0, 4);

        left.Controls.Add(content);

        // Panel phải: Log kết quả giao dịch
        var right = new GroupBox { Text = "Nhật ký kiểm chứng giao dịch (Transaction Logs)", Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8) };
        right.Controls.Add(_txLog);

        split.Panel1.Controls.Add(left);
        split.Panel2.Controls.Add(right);
        page.Controls.Add(split);
        return page;
    }

    private void ExecuteAddStudentTransaction()
    {
        var code = _txStudentCode.Text.Trim();
        var name = _txStudentName.Text.Trim();
        if (string.IsNullOrEmpty(code)) throw new BusinessRuleException("Nhập mã sinh viên.");
        if (string.IsNullOrEmpty(name)) throw new BusinessRuleException("Nhập họ tên sinh viên.");
        if (_txFaculty.SelectedValue is not string faculty || string.IsNullOrEmpty(faculty)) throw new BusinessRuleException("Chọn khoa.");

        var selectedCourses = new List<EnrollmentDto>();
        for (int i = 0; i < _txCourseList.Items.Count; i++)
        {
            if (_txCourseList.GetItemChecked(i) && _txCourseList.Items[i] is CourseChoice cc)
            {
                selectedCourses.Add(new EnrollmentDto(code, cc.Code, 1, null));
            }
        }
        if (selectedCourses.Count == 0) throw new BusinessRuleException("Chọn ít nhất một học phần.");

        var student = new StudentDto
        {
            MaSV = code,
            HoTen = name,
            NgaySinh = DateTime.Today.AddYears(-19),
            GioiTinh = true,
            MaKhoa = faculty,
            DiemTB = 0m,
            Email = null
        };

        LogTx($"=== BẮT ĐẦU GIAO DỊCH (BÀI 3) ===");
        LogTx($"Thêm sinh viên: {code} - {name} ({faculty})");
        LogTx($"Số học phần đăng ký ban đầu: {selectedCourses.Count}");

        int rows = _academic.AddStudentWithEnrollments(student, selectedCourses);

        LogTx($"GIAO DỊCH THÀNH CÔNG (COMMIT). Đã ghi {rows} bản ghi vào CSDL.");
        LogTx($"Kiểm tra: Sinh viên {code} và {selectedCourses.Count} môn học đã lưu an toàn.\n");

        _txStudentCode.Clear();
        _txStudentName.Clear();
        for (int i = 0; i < _txCourseList.Items.Count; i++) _txCourseList.SetItemChecked(i, false);
        LoadStudents();
        LoadFaculties();

        MessageBox.Show(this, $"Thêm thành công sinh viên {code} cùng {selectedCourses.Count} học phần trong 1 SqlTransaction!", "Giao dịch thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExecuteRollbackDemo()
    {
        if (_txFaculty.SelectedValue is not string faculty || string.IsNullOrEmpty(faculty)) throw new BusinessRuleException("Chọn khoa.");

        LogTx($"=== BẮT ĐẦU DEMO ROLLBACK (BÀI 4) ===");
        LogTx($"1. Khởi tạo SqlTransaction.");
        LogTx($"2. Chèn sinh viên thử nghiệm vào bảng SinhVien.");
        LogTx($"3. Cố ý chèn mã học phần không tồn tại 'LAB12_BAD' vào bảng DangKy để kích hoạt lỗi FK 547...");

        var result = _academic.DemonstrateRollback(faculty);

        if (result.FailedAsExpected && result.StudentWasRemoved)
        {
            LogTx($"-> Bắt lỗi vi phạm khóa ngoại SqlException 547!");
            LogTx($"-> ĐÃ THỰC HIỆN ROLLBACK TOÀN BỘ GIAO DỊCH!");
            LogTx($"-> Kiểm tra lại: Sinh viên thử nghiệm HOÀN TOÀN KHÔNG TỒN TẠI trong bảng SinhVien.");
            LogTx($"KẾT LUẬN: Rollback hoạt động hoàn hảo, CSDL không bị dữ liệu dở dang (Atomic).\n");

            MessageBox.Show(this, "Đã kích hoạt lỗi vi phạm FK thành công!\nGiao dịch đã ROLLBACK hoàn toàn. Sinh viên thử nghiệm không được lưu vào CSDL.", "Demo Rollback thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            LogTx("Rollback thất bại hoặc dữ liệu không như kỳ vọng.\n");
        }
    }

    private void LogTx(string msg)
    {
        _txLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
    }
    #endregion

    #region Tab 4: Bài tập tự thực hiện nâng cao (Bài 6, 7, 8, 9)
    private TabPage BuildAdvancedTab()
    {
        var page = NewTab("Bài 6–9 | Chuyển khoa, Xóa an toàn, Điểm, RowVersion");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.White, Padding = new Padding(8) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        // Khối 1: Bài 6 - Chuyển sinh viên sang khoa khác (Transaction + Audit)
        var box6 = new GroupBox { Text = "Bài 6. Chuyển khoa sinh viên (Ghi lịch sử trong Transaction)", Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(6) };
        var l6 = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        l6.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        l6.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        l6.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var f6 = new FlowLayoutPanel { Dock = DockStyle.Fill };
        AddField(f6, "Sinh viên", _advStudent);
        AddField(f6, "Khoa đích", _advTargetFaculty);
        AddField(f6, "Lý do", _advTransferReason);
        l6.Controls.Add(f6, 0, 0);

        var a6 = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var btnTransfer = MakeButton("6. Chuyển khoa", Color.FromArgb(42, 102, 184), 130);
        btnTransfer.Click += (_, _) => RunUi(ExecuteTransferStudent);
        a6.Controls.Add(btnTransfer);
        l6.Controls.Add(a6, 0, 1);

        l6.Controls.Add(_advHistoryGrid, 0, 2);
        box6.Controls.Add(l6);
        root.Controls.Add(box6, 0, 0);

        // Khối 2: Bài 7 - Xóa khoa an toàn
        var box7 = new GroupBox { Text = "Bài 7. Xóa khoa an toàn (Chuyển SV sang khoa khác trước khi xóa)", Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8) };
        var f7 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
        AddField(f7, "Khoa cần xóa", _advDeleteSourceFaculty);
        AddField(f7, "Khoa tiếp nhận SV", _advDeleteTargetFaculty);
        var btnDelFacultySafe = MakeButton("7. Xóa khoa an toàn", Color.FromArgb(166, 79, 70), 160);
        btnDelFacultySafe.Click += (_, _) => RunUi(ExecuteDeleteFacultySafe);
        f7.Controls.Add(btnDelFacultySafe);
        f7.Controls.Add(MakeLabel("Trong cùng 1 Transaction, toàn bộ sinh viên khoa nguồn sẽ được chuyển sang khoa đích rồi mới xóa khoa nguồn."));
        box7.Controls.Add(f7);
        root.Controls.Add(box7, 1, 0);

        // Khối 3: Bài 8 - Cập nhật điểm hàng loạt (Atomic)
        var box8 = new GroupBox { Text = "Bài 8. Cập nhật điểm hàng loạt (Rollback nếu có điểm sai ngoài [0,10])", Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8) };
        var f8 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
        f8.Controls.Add(MakeLabel("Nhập danh sách (MaSV,MaHP,Diem):"));
        f8.Controls.Add(_advBatchGrades);
        var btnBatchGrades = MakeButton("8. Cập nhật điểm", Color.FromArgb(16, 129, 102), 150);
        btnBatchGrades.Click += (_, _) => RunUi(ExecuteBatchGrades);
        f8.Controls.Add(btnBatchGrades);
        var btnSampleGrades = MakeButton("Mẫu hợp lệ", Color.FromArgb(95, 110, 128), 100);
        btnSampleGrades.Click += (_, _) => _advBatchGrades.Text = "SV001,CS101,8.50\nSV001,CS102,9.00";
        f8.Controls.Add(btnSampleGrades);
        var btnErrorGrades = MakeButton("Mẫu sai (điểm 12)", Color.FromArgb(166, 79, 70), 130);
        btnErrorGrades.Click += (_, _) => _advBatchGrades.Text = "SV001,CS101,8.50\nSV001,CS102,12.50";
        f8.Controls.Add(btnErrorGrades);
        box8.Controls.Add(f8);
        root.Controls.Add(box8, 0, 1);

        // Khối 4: Bài 9 - Kiểm soát xung đột cập nhật (RowVersion Concurrency)
        var box9 = new GroupBox { Text = "Bài 9. Kiểm soát xung đột cập nhật (RowVersion Concurrency)", Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8) };
        var f9 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
        AddField(f9, "Mã SV", _advConflictStudent);
        var btnDemoConflict = MakeButton("9. Kiểm thử xung đột", Color.FromArgb(42, 102, 184), 160);
        btnDemoConflict.Click += (_, _) => RunUi(ExecuteConflictDemo);
        f9.Controls.Add(btnDemoConflict);
        f9.Controls.Add(MakeLabel("Giả lập Người dùng A đã sửa và cập nhật RowVersion mới, Người dùng B gửi bản ghi với RowVersion cũ -> Bắt lỗi 50031."));
        box9.Controls.Add(f9);
        root.Controls.Add(box9, 1, 1);

        page.Controls.Add(root);
        return page;
    }

    private void ExecuteTransferStudent()
    {
        if (_advStudent.SelectedValue is not string maSV || string.IsNullOrEmpty(maSV)) throw new BusinessRuleException("Chọn sinh viên.");
        if (_advTargetFaculty.SelectedValue is not string target || string.IsNullOrEmpty(target)) throw new BusinessRuleException("Chọn khoa đích.");

        var reason = _advTransferReason.Text;
        _academic.TransferStudent(maSV, target, null, reason);
        _advHistoryGrid.DataSource = _academic.TransferHistory();
        LoadStudents();
        LoadFaculties();
        MessageBox.Show(this, $"Đã chuyển sinh viên {maSV} sang khoa {target} và ghi nhật ký thành công!", "Chuyển khoa", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExecuteDeleteFacultySafe()
    {
        if (_advDeleteSourceFaculty.SelectedValue is not string src || string.IsNullOrEmpty(src)) throw new BusinessRuleException("Chọn khoa cần xóa.");
        if (_advDeleteTargetFaculty.SelectedValue is not string target || string.IsNullOrEmpty(target)) throw new BusinessRuleException("Chọn khoa tiếp nhận.");

        if (MessageBox.Show(this, $"Xóa an toàn khoa {src} và chuyển toàn bộ sinh viên sang khoa {target}?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        int transferred = _academic.DeleteFacultySafely(src, target);
        LoadFaculties();
        LoadStudents();
        _advHistoryGrid.DataSource = _academic.TransferHistory();
        MessageBox.Show(this, $"Đã xóa khoa {src} thành công! Đã tự động chuyển {transferred} sinh viên sang khoa {target}.", "Xóa an toàn", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExecuteBatchGrades()
    {
        var lines = _advBatchGrades.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var list = new List<GradeChange>();
        foreach (var l in lines)
        {
            var parts = l.Split(',');
            if (parts.Length != 3) throw new BusinessRuleException($"Dòng không đúng định dạng: {l}");
            if (!decimal.TryParse(parts[2].Trim(), CultureInfo.InvariantCulture, out var g)) throw new BusinessRuleException($"Điểm không hợp lệ: {parts[2]}");
            list.Add(new GradeChange(parts[0].Trim(), parts[1].Trim(), g));
        }

        int count = _academic.UpdateGradesAtomically(list);
        MessageBox.Show(this, $"Đã cập nhật điểm thành công cho {count} học phần trong 1 SqlTransaction!", "Cập nhật điểm", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExecuteConflictDemo()
    {
        var code = _advConflictStudent.Text.Trim();
        if (string.IsNullOrEmpty(code)) throw new BusinessRuleException("Nhập mã sinh viên đang hoạt động.");

        bool conflictDetected = _academic.DemonstrateRowVersionConflict(code);
        if (conflictDetected)
        {
            MessageBox.Show(this, $"Kiểm tra thành công! Hệ thống đã phát hiện xung đột RowVersion (Mã lỗi 50031: Sinh viên đã được người khác cập nhật trước đó).", "Phát hiện xung đột", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show(this, "Không có xung đột.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
    #endregion

    #region Tab 5 & 6: CRUD Sinh viên & Khoa (Bài 10 Hoàn thiện CSDL)
    private TabPage BuildStudentTab()
    {
        var page = NewTab("Bài 10 | Quản lý Sinh viên");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8), ColumnCount = 1, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 218));

        var filters = new FlowLayoutPanel { BackColor = Color.White, Padding = new Padding(6), WrapContents = false, Dock = DockStyle.Fill };
        filters.Controls.Add(MakeLabel("Tìm kiếm"));
        filters.Controls.Add(_searchText);
        filters.Controls.Add(MakeLabel("Khoa"));
        filters.Controls.Add(_searchFaculty);
        filters.Controls.Add(_includeInactive);

        var search = MakeButton("Tìm kiếm", Color.FromArgb(42, 102, 184), 92);
        search.Click += (_, _) => RunUi(LoadStudents);
        filters.Controls.Add(search);

        var refresh = MakeButton("Làm mới", Color.FromArgb(95, 110, 128), 105);
        refresh.Click += (_, _) => { _searchText.Clear(); _searchFaculty.SelectedIndex = 0; _includeInactive.Checked = false; RunUi(LoadStudents); };
        filters.Controls.Add(refresh);

        var gridBox = new GroupBox { Text = "Danh sách sinh viên", Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(6) };
        gridBox.Controls.Add(_studentGrid);
        _studentGrid.SelectionChanged += (_, _) => ShowSelectedStudent();
        _studentGrid.DataBindingComplete += (_, _) => FormatStudentGrid();

        var editor = BuildStudentEditor();
        layout.Controls.Add(filters, 0, 0);
        layout.Controls.Add(gridBox, 0, 1);
        layout.Controls.Add(editor, 0, 2);
        page.Controls.Add(layout);
        return page;
    }

    private Control BuildStudentEditor()
    {
        var group = new GroupBox { Text = "Thêm, sửa, xóa mềm hoặc khôi phục sinh viên", Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(7) };
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(3) };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        var row1 = NewFieldRow();
        AddField(row1, "Mã SV", _studentCode);
        AddField(row1, "Họ tên", _studentName);
        AddField(row1, "Ngày sinh", _birthDate);
        content.Controls.Add(row1, 0, 0);

        var row2 = NewFieldRow();
        var gender = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(4, 2, 8, 2) };
        gender.Controls.Add(MakeLabel("Giới tính"));
        gender.Controls.Add(_male);
        gender.Controls.Add(_female);
        row2.Controls.Add(gender);
        AddField(row2, "Khoa", _studentFaculty);
        AddField(row2, "Điểm TB", _score);
        AddField(row2, "Email", _email);
        content.Controls.Add(row2, 0, 1);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(1) };
        var add = MakeButton("Thêm SV", Color.FromArgb(16, 129, 102), 105);
        add.Click += (_, _) => RunUi(AddStudent);
        actions.Controls.Add(add);

        var update = MakeButton("Sửa", Color.FromArgb(42, 102, 184), 85);
        update.Click += (_, _) => RunUi(UpdateStudent);
        actions.Controls.Add(update);

        var delete = MakeButton("Xóa mềm", Color.FromArgb(166, 79, 70), 105);
        delete.Click += (_, _) => RunUi(SoftDeleteStudent);
        actions.Controls.Add(delete);

        var restore = MakeButton("Khôi phục", Color.FromArgb(16, 129, 102), 115);
        restore.Click += (_, _) => RunUi(RestoreStudent);
        actions.Controls.Add(restore);

        var clear = MakeButton("Nhập mới", Color.FromArgb(95, 110, 128), 100);
        clear.Click += (_, _) => ClearStudentForm();
        actions.Controls.Add(clear);

        _studentStatus.Padding = new Padding(10, 8, 0, 0);
        actions.Controls.Add(_studentStatus);
        content.Controls.Add(actions, 0, 2);

        var note = MakeLabel("BUS kiểm tra mã, họ tên, tuổi, điểm và email; DAL chỉ gọi Stored Procedure có tham số.");
        note.ForeColor = Color.FromArgb(80, 92, 105);
        content.Controls.Add(note, 0, 3);
        group.Controls.Add(content);
        return group;
    }

    private TabPage BuildFacultyTab()
    {
        var page = NewTab("Bài 10 | Quản lý Khoa");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8), ColumnCount = 1, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));

        var top = new FlowLayoutPanel { Height = 42, Padding = new Padding(3), Dock = DockStyle.Fill };
        var refresh = MakeButton("Tải danh sách khoa", Color.FromArgb(42, 102, 184), 165);
        refresh.Click += (_, _) => RunUi(LoadFaculties);
        top.Controls.Add(refresh);
        _facultyCount.Padding = new Padding(8, 8, 0, 0);
        top.Controls.Add(_facultyCount);

        var gridBox = new GroupBox { Text = "Danh sách Khoa", Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(6) };
        gridBox.Controls.Add(_facultyGrid);
        _facultyGrid.SelectionChanged += (_, _) => ShowSelectedFaculty();

        var editor = new GroupBox { Text = "Thêm, sửa hoặc xóa khoa", Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(6) };
        var fields = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, Padding = new Padding(4) };
        AddField(fields, "Mã khoa", _facultyCode);
        AddField(fields, "Tên khoa", _facultyName);

        var add = MakeButton("Thêm khoa", Color.FromArgb(16, 129, 102), 115);
        add.Click += (_, _) => RunUi(AddFaculty);
        fields.Controls.Add(add);

        var update = MakeButton("Sửa tên", Color.FromArgb(42, 102, 184), 100);
        update.Click += (_, _) => RunUi(UpdateFaculty);
        fields.Controls.Add(update);

        var delete = MakeButton("Xóa khoa", Color.FromArgb(166, 79, 70), 105);
        delete.Click += (_, _) => RunUi(DeleteFaculty);
        fields.Controls.Add(delete);

        var clear = MakeButton("Nhập mới", Color.FromArgb(95, 110, 128), 100);
        clear.Click += (_, _) => ClearFacultyForm();
        fields.Controls.Add(clear);

        _facultyStatus.Padding = new Padding(8, 8, 0, 0);
        fields.Controls.Add(_facultyStatus);
        editor.Controls.Add(fields);

        layout.Controls.Add(top, 0, 0);
        layout.Controls.Add(gridBox, 0, 1);
        layout.Controls.Add(editor, 0, 2);
        page.Controls.Add(layout);
        return page;
    }
    #endregion

    private void LoadInitialData()
    {
        LoadFaculties();
        LoadStudents();
        LoadCourses();
        _advHistoryGrid.DataSource = _academic.TransferHistory();

        _connectionStatus.Text = "Kết nối thành công | QLSinhVien | Master-Detail & SqlTransaction sẵn sàng";
        _connectionStatus.ForeColor = Color.FromArgb(16, 129, 102);
    }

    private void PrepareDatabase()
    {
        var batches = _setup.Prepare();
        LoadInitialData();
        MessageBox.Show(this, $"Đã chạy {batches} batch SQL. Database và Stored Procedure đã sẵn sàng.", "CSDL Lab12", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LoadCourses()
    {
        var courses = _academic.Courses();
        _cboCourse.DataSource = courses.Copy();
        _cboCourse.DisplayMember = "TenHP";
        _cboCourse.ValueMember = "MaHP";

        _txCourseList.Items.Clear();
        foreach (DataRow r in courses.Rows)
        {
            _txCourseList.Items.Add(new CourseChoice(Convert.ToString(r["MaHP"])!, Convert.ToString(r["TenHP"])!, Convert.ToInt32(r["SoTinChi"])));
        }
    }

    private void LoadFaculties()
    {
        _facultyTable = _faculties.GetAll();
        _facultyGrid.DataSource = _facultyTable;
        _mdFacultyGrid.DataSource = _facultyTable.Copy();

        _searchFaculty.Items.Clear();
        _searchFaculty.Items.Add(new FacultyChoice("", "Tất cả khoa"));
        _advTargetFaculty.Items.Clear();
        _advDeleteSourceFaculty.Items.Clear();
        _advDeleteTargetFaculty.Items.Clear();

        foreach (DataRow row in _facultyTable.Rows)
        {
            var code = Convert.ToString(row["MaKhoa"])!;
            var name = Convert.ToString(row["TenKhoa"])!;
            _searchFaculty.Items.Add(new FacultyChoice(code, name));
            _advTargetFaculty.Items.Add(new FacultyChoice(code, name));
            _advDeleteSourceFaculty.Items.Add(new FacultyChoice(code, name));
            _advDeleteTargetFaculty.Items.Add(new FacultyChoice(code, name));
        }

        _searchFaculty.DisplayMember = nameof(FacultyChoice.Display);
        _searchFaculty.SelectedIndex = 0;

        _advTargetFaculty.DisplayMember = nameof(FacultyChoice.Display);
        if (_advTargetFaculty.Items.Count > 0) _advTargetFaculty.SelectedIndex = 0;

        _advDeleteSourceFaculty.DisplayMember = nameof(FacultyChoice.Display);
        if (_advDeleteSourceFaculty.Items.Count > 0) _advDeleteSourceFaculty.SelectedIndex = 0;

        _advDeleteTargetFaculty.DisplayMember = nameof(FacultyChoice.Display);
        if (_advDeleteTargetFaculty.Items.Count > 1) _advDeleteTargetFaculty.SelectedIndex = 1;

        _studentFaculty.DataSource = _facultyTable.Copy();
        _studentFaculty.DisplayMember = "TenKhoa";
        _studentFaculty.ValueMember = "MaKhoa";

        _txFaculty.DataSource = _facultyTable.Copy();
        _txFaculty.DisplayMember = "TenKhoa";
        _txFaculty.ValueMember = "MaKhoa";

        _facultyCount.Text = $"{_facultyTable.Rows.Count} khoa";
    }

    private void LoadStudents()
    {
        var facultyCode = _searchFaculty.SelectedItem is FacultyChoice { Code.Length: > 0 } faculty ? faculty.Code : null;
        var dt = _students.Search(_searchText.Text, facultyCode, _includeInactive.Checked);
        _studentGrid.DataSource = dt;
        _enrollStudentGrid.DataSource = dt.Copy();

        _advStudent.Items.Clear();
        foreach (DataRow r in dt.Rows)
        {
            if (Convert.ToBoolean(r["IsActive"]))
            {
                var code = Convert.ToString(r["MaSV"])!;
                var name = Convert.ToString(r["HoTen"])!;
                _advStudent.Items.Add(new StudentChoice(code, name));
            }
        }
        _advStudent.DisplayMember = nameof(StudentChoice.Display);
        if (_advStudent.Items.Count > 0) _advStudent.SelectedIndex = 0;

        _studentStatus.Text = $"Đang hiện {_studentGrid.Rows.Count} dòng.";
    }

    private void AddStudent()
    {
        var created = _students.Add(ReadStudentDto());
        LoadStudents();
        ClearStudentForm();
        _studentStatus.Text = $"Đã thêm {created} sinh viên.";
    }

    private void UpdateStudent()
    {
        if (string.IsNullOrWhiteSpace(_currentStudentCode)) throw new BusinessRuleException("Chọn sinh viên đang hoạt động cần sửa.");
        if (!_currentStudentActive) throw new BusinessRuleException("Sinh viên đã xóa mềm; hãy khôi phục trước khi sửa.");
        var dto = ReadStudentDto();
        dto.MaSV = _currentStudentCode;
        var changed = _students.Update(dto);
        LoadStudents();
        _studentStatus.Text = $"Đã cập nhật {changed} sinh viên.";
    }

    private void SoftDeleteStudent()
    {
        if (string.IsNullOrWhiteSpace(_currentStudentCode)) throw new BusinessRuleException("Chọn sinh viên cần xóa mềm.");
        if (!_currentStudentActive) throw new BusinessRuleException("Sinh viên này đã được xóa mềm.");
        if (MessageBox.Show(this, $"Đánh dấu sinh viên {_currentStudentCode} là đã xóa?", "Xóa mềm sinh viên", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var affected = _students.SoftDelete(_currentStudentCode);
        LoadStudents();
        ClearStudentForm();
        _studentStatus.Text = $"Đã xóa mềm {affected} sinh viên; dữ liệu vẫn được giữ trong CSDL.";
    }

    private void RestoreStudent()
    {
        if (string.IsNullOrWhiteSpace(_currentStudentCode)) throw new BusinessRuleException("Chọn sinh viên đã xóa mềm cần khôi phục.");
        if (_currentStudentActive) throw new BusinessRuleException("Sinh viên này đang hoạt động.");
        var changed = _students.Restore(_currentStudentCode);
        _includeInactive.Checked = false;
        LoadStudents();
        ClearStudentForm();
        _studentStatus.Text = $"Đã khôi phục {changed} sinh viên.";
    }

    private StudentDto ReadStudentDto() => new()
    {
        MaSV = _studentCode.Text,
        HoTen = _studentName.Text,
        NgaySinh = _birthDate.Value.Date,
        GioiTinh = _male.Checked,
        MaKhoa = Convert.ToString(_studentFaculty.SelectedValue, CultureInfo.InvariantCulture) ?? "",
        DiemTB = _score.Value,
        Email = _email.Text
    };

    private void ShowSelectedStudent()
    {
        if (_studentGrid.CurrentRow?.DataBoundItem is not DataRowView row) return;
        _currentStudentCode = Convert.ToString(row["MaSV"]);
        _currentStudentActive = Convert.ToBoolean(row["IsActive"]);
        _studentCode.Text = _currentStudentCode;
        _studentCode.ReadOnly = true;
        _studentName.Text = Convert.ToString(row["HoTen"]);
        if (DateTime.TryParse(Convert.ToString(row["NgaySinh"]), out var date)) _birthDate.Value = date;
        _male.Checked = Convert.ToBoolean(row["GioiTinh"]);
        _female.Checked = !_male.Checked;
        _studentFaculty.SelectedValue = Convert.ToString(row["MaKhoa"]) ?? "";
        _score.Value = Math.Clamp(Convert.ToDecimal(row["DiemTB"]), _score.Minimum, _score.Maximum);
        _email.Text = row["Email"] is DBNull ? "" : Convert.ToString(row["Email"]);
        _studentStatus.Text = _currentStudentActive ? "Đang chọn sinh viên hoạt động." : "Đang chọn bản ghi đã xóa mềm.";
    }

    private void ClearStudentForm()
    {
        _studentGrid.ClearSelection();
        _studentGrid.CurrentCell = null;
        _currentStudentCode = null;
        _currentStudentActive = false;
        _studentCode.ReadOnly = false;
        _studentCode.Clear();
        _studentName.Clear();
        _birthDate.Value = DateTime.Today.AddYears(-18);
        _male.Checked = true;
        _score.Value = 0;
        _email.Clear();
        if (_studentFaculty.Items.Count > 0) _studentFaculty.SelectedIndex = 0;
        _studentStatus.Text = "Nhập thông tin sinh viên.";
    }

    private void AddFaculty()
    {
        var count = _faculties.Add(_facultyCode.Text, _facultyName.Text);
        LoadFaculties();
        LoadStudents();
        ClearFacultyForm();
        _facultyStatus.Text = $"Đã thêm {count} khoa.";
    }

    private void UpdateFaculty()
    {
        if (!_currentFacultyEditing) throw new BusinessRuleException("Chọn khoa cần sửa.");
        var count = _faculties.Update(_facultyCode.Text, _facultyName.Text);
        LoadFaculties();
        LoadStudents();
        _facultyStatus.Text = $"Đã cập nhật {count} khoa.";
    }

    private void DeleteFaculty()
    {
        if (!_currentFacultyEditing) throw new BusinessRuleException("Chọn khoa cần xóa.");
        if (MessageBox.Show(this, $"Xóa khoa {_facultyCode.Text}?", "Xóa khoa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var count = _faculties.Delete(_facultyCode.Text);
        LoadFaculties();
        LoadStudents();
        ClearFacultyForm();
        _facultyStatus.Text = $"Đã xóa {count} khoa.";
    }

    private void ShowSelectedFaculty()
    {
        if (_facultyGrid.CurrentRow?.DataBoundItem is not DataRowView row) return;
        _currentFacultyEditing = true;
        _facultyCode.Text = Convert.ToString(row["MaKhoa"]);
        _facultyCode.ReadOnly = true;
        _facultyName.Text = Convert.ToString(row["TenKhoa"]);
        _facultyStatus.Text = $"Sinh viên tham chiếu: {row["SoSinhVien"]}";
    }

    private void ClearFacultyForm()
    {
        _facultyGrid.ClearSelection();
        _facultyGrid.CurrentCell = null;
        _currentFacultyEditing = false;
        _facultyCode.ReadOnly = false;
        _facultyCode.Clear();
        _facultyName.Clear();
        _facultyStatus.Text = "Nhập thông tin khoa.";
    }

    private void FormatStudentGrid()
    {
        Set("MaSV", "Mã SV", 75);
        Set("HoTen", "Họ tên", 180);
        Set("NgaySinh", "Ngày sinh", 105);
        Set("GioiTinh", "Nam", 64);
        Set("MaKhoa", "Mã khoa", 65);
        Set("TenKhoa", "Khoa", 150);
        Set("DiemTB", "Điểm", 60);
        Set("Email", "Email", 125);
        Set("IsActive", "Hoạt động", 60);
        if (_studentGrid.Columns.Contains("NgaySinh")) _studentGrid.Columns["NgaySinh"]!.DefaultCellStyle.Format = "dd/MM/yyyy";
        if (_studentGrid.Columns.Contains("DiemTB")) _studentGrid.Columns["DiemTB"]!.DefaultCellStyle.Format = "N2";
        if (_studentGrid.Columns.Contains("Email")) _studentGrid.Columns["Email"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        void Set(string name, string title, int width)
        {
            if (!_studentGrid.Columns.Contains(name)) return;
            var col = _studentGrid.Columns[name]!;
            col.HeaderText = title;
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            col.MinimumWidth = 42;
            col.Width = width;
        }
    }

    private void RunUi(Action action, bool showValidationAsWarning = true)
    {
        try { action(); }
        catch (BusinessRuleException ex)
        {
            SetStatus(ex.Message, Color.FromArgb(166, 98, 0));
            if (showValidationAsWarning)
                MessageBox.Show(this, ex.Message, "Kiểm tra nghiệp vụ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (ApplicationDataException ex)
        {
            LogTechnicalError(ex.TechnicalDetails);
            SetStatus(ex.Message, Color.Firebrick);
            MessageBox.Show(this, ex.Message, "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            LogTechnicalError(ex.ToString());
            const string safeMessage = "Đã xảy ra lỗi. Hãy kiểm tra cấu hình và nhật ký Lab12 để biết chi tiết.";
            SetStatus(safeMessage, Color.Firebrick);
            MessageBox.Show(this, safeMessage, "Lỗi ứng dụng", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetStatus(string message, Color color)
    {
        _connectionStatus.Text = message;
        _connectionStatus.ForeColor = color;
    }

    private static void LogTechnicalError(string details)
    {
        try
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "Logs");
            Directory.CreateDirectory(directory);
            var file = Path.Combine(directory, $"Lab12-{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(file, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {details}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { }
    }

    private static TabPage NewTab(string title) => new(title) { BackColor = Color.FromArgb(245, 248, 252), Padding = new Padding(4) };

    private static DataGridView MakeGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        RowHeadersVisible = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        EnableHeadersVisualStyles = false,
        ColumnHeadersDefaultCellStyle = { BackColor = Color.FromArgb(25, 65, 110), ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold) },
        AlternatingRowsDefaultCellStyle = { BackColor = Color.FromArgb(241, 246, 251) }
    };

    private static Label NewStatus() => new() { AutoSize = true, ForeColor = Color.DimGray };
    private static Label MakeLabel(string text) => new() { Text = text, AutoSize = true, Padding = new Padding(4, 7, 2, 0) };
    private static FlowLayoutPanel NewFieldRow() => new() { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(1) };
    private static Button MakeButton(string text, Color color, int width) => new()
    {
        Text = text,
        Width = width,
        Height = 31,
        BackColor = color,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
        Margin = new Padding(3)
    };

    private static void AddField(FlowLayoutPanel row, string label, Control input)
    {
        var field = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(4, 2, 7, 2) };
        field.Controls.Add(MakeLabel(label));
        field.Controls.Add(input);
        row.Controls.Add(field);
    }

    private sealed record FacultyChoice(string Code, string Name)
    {
        public string Display => string.IsNullOrEmpty(Code) ? Name : $"{Code} - {Name}";
        public override string ToString() => Display;
    }

    private sealed record StudentChoice(string Code, string Name)
    {
        public string Display => $"{Code} - {Name}";
        public override string ToString() => Display;
    }

    private sealed record CourseChoice(string Code, string Name, int Credits)
    {
        public override string ToString() => $"{Code} - {Name} ({Credits} tín chỉ)";
    }
}
