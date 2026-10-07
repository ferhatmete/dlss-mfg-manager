using System.Runtime.InteropServices;

namespace DlssMfgManager.Services;

public static class AppTheme
{
    public static Color Background { get; } = Color.FromArgb(6, 10, 20);
    public static Color Surface { get; } = Color.FromArgb(13, 20, 35);
    public static Color SurfaceAlt { get; } = Color.FromArgb(24, 34, 54);
    public static Color Input { get; } = Color.FromArgb(10, 17, 31);
    public static Color TextPrimary { get; } = Color.FromArgb(248, 250, 252);
    public static Color TextSecondary { get; } = Color.FromArgb(203, 213, 225);
    public static Color TextMuted { get; } = Color.FromArgb(148, 163, 184);
    public static Color Border { get; } = Color.FromArgb(55, 70, 96);
    public static Color Primary { get; } = Color.FromArgb(79, 70, 229);
    public static Color PrimaryHover { get; } = Color.FromArgb(99, 102, 241);
    public static Color Accent { get; } = Color.FromArgb(34, 211, 238);
    public static Color Success { get; } = Color.FromArgb(16, 185, 129);
    public static Color Danger { get; } = Color.FromArgb(239, 68, 68);
    public static Color WarningBackground { get; } = Color.FromArgb(63, 25, 31);
    public static Color WarningText { get; } = Color.FromArgb(252, 165, 165);
    public static Color StatusReady { get; } = Color.FromArgb(74, 222, 128);
    public static Color StatusWarning { get; } = Color.FromArgb(251, 191, 36);
    public static Color StatusRepair { get; } = Color.FromArgb(251, 146, 60);
    public static Color StatusError { get; } = Color.FromArgb(248, 113, 113);

    public static void ApplyDarkTitleBar(Form form)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            return;

        var enabled = 1;
        var attribute = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18985) ? 20 : 19;
        _ = DwmSetWindowAttribute(form.Handle, attribute, ref enabled, sizeof(int));
    }

    public static void StyleDataGridView(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.BackgroundColor = Input;
        grid.GridColor = Border;
        grid.DefaultCellStyle.BackColor = Input;
        grid.DefaultCellStyle.ForeColor = TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 64, 175);
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Surface;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = TextPrimary;
        grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceAlt;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = SurfaceAlt;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextPrimary;
        grid.ColumnHeadersHeight = 34;
        grid.RowTemplate.Height = 32;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
    }

    public static void StyleInput(Control control)
    {
        control.BackColor = Input;
        control.ForeColor = TextPrimary;
        if (control is ComboBox comboBox)
            comboBox.FlatStyle = FlatStyle.Flat;
    }

    public static void StyleListView(ListView list)
    {
        StyleInput(list);
        list.OwnerDraw = true;
        list.Font = new Font("Segoe UI", 9.25F);
        list.DrawColumnHeader += (_, e) =>
        {
            using var background = new SolidBrush(SurfaceAlt);
            e.Graphics.FillRectangle(background, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? string.Empty, list.Font,
                new Rectangle(e.Bounds.X + 6, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 8), e.Bounds.Height),
                TextPrimary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
        list.DrawItem += (_, e) =>
        {
            if (list.View != View.Details)
                e.DrawDefault = true;
        };
        list.DrawSubItem += (_, e) =>
        {
            if (e.Item is null || e.SubItem is null)
                return;

            var selected = e.Item.Selected;
            using var background = new SolidBrush(selected ? Color.FromArgb(30, 64, 175) : Input);
            e.Graphics.FillRectangle(background, e.Bounds);
            var foreground = selected ? Color.White :
                (e.SubItem.ForeColor.IsEmpty ? e.Item.ForeColor : e.SubItem.ForeColor);
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, list.Font,
                new Rectangle(e.Bounds.X + 4, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 6), e.Bounds.Height),
                foreground, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr windowHandle, int attribute,
        ref int attributeValue, int attributeSize);
}
