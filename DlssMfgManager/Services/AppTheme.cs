using System.Runtime.InteropServices;

namespace DlssMfgManager.Services;

public static class AppTheme
{
    public static Color Background { get; } = Color.FromArgb(243, 246, 251);
    public static Color Sidebar { get; } = Color.FromArgb(255, 255, 255);
    public static Color Surface { get; } = Color.FromArgb(255, 255, 255);
    public static Color SurfaceAlt { get; } = Color.FromArgb(239, 242, 248);
    public static Color SurfaceRaised { get; } = Color.FromArgb(226, 231, 241);
    public static Color Input { get; } = Color.FromArgb(248, 250, 253);
    public static Color TextPrimary { get; } = Color.FromArgb(27, 34, 52);
    public static Color TextSecondary { get; } = Color.FromArgb(70, 82, 105);
    public static Color TextMuted { get; } = Color.FromArgb(117, 130, 153);
    public static Color Border { get; } = Color.FromArgb(211, 218, 231);
    public static Color Primary { get; } = Color.FromArgb(92, 99, 230);
    public static Color PrimaryHover { get; } = Color.FromArgb(112, 119, 248);
    public static Color Accent { get; } = Color.FromArgb(67, 213, 196);
    public static Color AccentBlue { get; } = Color.FromArgb(92, 188, 255);
    public static Color Success { get; } = Color.FromArgb(16, 185, 129);
    public static Color Danger { get; } = Color.FromArgb(239, 68, 68);
    public static Color WarningBackground { get; } = Color.FromArgb(255, 246, 229);
    public static Color WarningText { get; } = Color.FromArgb(139, 84, 16);
    public static Color StatusReady { get; } = Color.FromArgb(17, 132, 91);
    public static Color StatusWarning { get; } = Color.FromArgb(164, 104, 6);
    public static Color StatusRepair { get; } = Color.FromArgb(190, 87, 22);
    public static Color StatusError { get; } = Color.FromArgb(194, 55, 55);

    public static void ApplyDarkTitleBar(Form form)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            return;

        var enabled = 0;
        var attribute = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18985) ? 20 : 19;
        _ = DwmSetWindowAttribute(form.Handle, attribute, ref enabled, sizeof(int));

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            var borderColor = ToColorRef(Border);
            var captionColor = ToColorRef(Sidebar);
            var textColor = ToColorRef(TextPrimary);
            _ = DwmSetWindowAttribute(form.Handle, 34, ref borderColor, sizeof(int));
            _ = DwmSetWindowAttribute(form.Handle, 35, ref captionColor, sizeof(int));
            _ = DwmSetWindowAttribute(form.Handle, 36, ref textColor, sizeof(int));
        }
    }

    public static void StyleDataGridView(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.ScrollBars = ScrollBars.Vertical;
        grid.BackgroundColor = Input;
        grid.GridColor = Border;
        grid.DefaultCellStyle.BackColor = Input;
        grid.DefaultCellStyle.ForeColor = TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = Primary;
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
        grid.MouseWheel += (_, e) => ScrollGrid(grid, e.Delta);
    }

    private static void ScrollGrid(DataGridView grid, int delta)
    {
        if (grid.RowCount == 0)
            return;

        try
        {
            var first = Math.Max(0, grid.FirstDisplayedScrollingRowIndex);
            var direction = delta > 0 ? -3 : 3;
            grid.FirstDisplayedScrollingRowIndex = Math.Clamp(first + direction, 0, grid.RowCount - 1);
        }
        catch
        {
            // The grid can reject scrolling while rows are being refreshed.
        }
    }

    public static void StyleInput(Control control)
    {
        control.BackColor = Input;
        control.ForeColor = TextPrimary;
        if (control is ComboBox comboBox)
        {
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.DrawMode = DrawMode.OwnerDrawFixed;
            comboBox.ItemHeight = 25;
            comboBox.DrawItem += DrawComboItem;
        }
    }

    private static void DrawComboItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox comboBox)
            return;

        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        using var background = new SolidBrush(selected ? Primary : Input);
        e.Graphics.FillRectangle(background, e.Bounds);
        var text = e.Index >= 0 && e.Index < comboBox.Items.Count
            ? comboBox.GetItemText(comboBox.Items[e.Index])
            : comboBox.Text;
        TextRenderer.DrawText(e.Graphics, text, comboBox.Font,
            new Rectangle(e.Bounds.X + 8, e.Bounds.Y, Math.Max(1, e.Bounds.Width - 12), e.Bounds.Height),
            selected ? Color.White : TextPrimary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        e.DrawFocusRectangle();
    }

    public static void StyleListView(ListView list)
    {
        StyleInput(list);
        list.OwnerDraw = true;
        list.Font = new Font("Segoe UI", 9.25F);
        list.DrawColumnHeader += (_, e) =>
        {
            using var background = new SolidBrush(Surface);
            e.Graphics.FillRectangle(background, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? string.Empty, list.Font,
                new Rectangle(e.Bounds.X + 8, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 10), e.Bounds.Height),
                TextMuted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
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
            using var background = new SolidBrush(selected ? Primary :
                (e.Item.Index % 2 == 0 ? Input : SurfaceAlt));
            e.Graphics.FillRectangle(background, e.Bounds);
            var foreground = selected ? Color.White :
                (e.SubItem.ForeColor.IsEmpty ? e.Item.ForeColor : e.SubItem.ForeColor);
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, list.Font,
                new Rectangle(e.Bounds.X + 8, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 10), e.Bounds.Height),
                foreground, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr windowHandle, int attribute,
        ref int attributeValue, int attributeSize);

    private static int ToColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);
}
