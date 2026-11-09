using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Lab12.BUS;

namespace Lab12_StoredProcedureCRUD;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--test")
        {
            RunTests();
            return;
        }

        if (args.Length > 0 && args[0] == "--capture")
        {
            RunCapture(args.Length > 1 ? args[1] : "Screenshots");
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }

    private static void RunTests()
    {
        try
        {
            var academic = new AcademicService();
            Console.WriteLine("Running tests...");
            var courses = academic.Courses();
            Console.WriteLine($"Courses: {courses.Rows.Count}");
            var faculties = new FacultyService().GetAll();
            Console.WriteLine($"Faculties: {faculties.Rows.Count}");
            var rb = academic.DemonstrateRollback("CNTT");
            Console.WriteLine($"Rollback demo: failed={rb.FailedAsExpected}, removed={rb.StudentWasRemoved}");
            Console.WriteLine("TESTS OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TEST ERROR: {ex.Message}");
        }
    }

    private static void RunCapture(string folder)
    {
        ApplicationConfiguration.Initialize();
        var dir = Path.IsPathRooted(folder) ? folder : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", folder);
        Directory.CreateDirectory(dir);

        using var form = new MainForm();
        form.Show();
        Application.DoEvents();
        Thread.Sleep(1000);

        // Find TabControl
        TabControl? tabControl = null;
        void SearchTabs(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is TabControl tc) { tabControl = tc; return; }
                if (c.HasChildren) SearchTabs(c);
            }
        }
        SearchTabs(form);

        if (tabControl != null)
        {
            for (int i = 0; i < tabControl.TabCount; i++)
            {
                tabControl.SelectedIndex = i;
                Application.DoEvents();
                Thread.Sleep(500);

                using var bmp = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                var cleanName = $"Tab_{i + 1}_{tabControl.TabPages[i].Text.Replace(" ", "_").Replace("|", "_").Replace("–", "-")}.png";
                foreach (var invalid in Path.GetInvalidFileNameChars()) cleanName = cleanName.Replace(invalid, '_');
                var path = Path.Combine(dir, cleanName);
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine($"Captured: {cleanName}");
            }
        }
        form.Close();
    }
}
