using System.Runtime.InteropServices;

namespace DlssMfgManager.Controls;

public sealed class DarkListView : ListView
{
    private readonly System.Windows.Forms.Timer _hideTimer = new() { Interval = 80 };

    public DarkListView()
    {
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            if (IsHandleCreated && !IsDisposed)
                _ = ShowScrollBar(Handle, 3, false);
        };
    }

    public void ScheduleHideNativeScrollbars()
    {
        if (!IsHandleCreated || IsDisposed)
            return;
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    public void ScrollRows(int rowCount)
    {
        if (Items.Count == 0 || !IsHandleCreated)
            return;

        var currentIndex = TopItem?.Index ?? 0;
        var nextIndex = Math.Clamp(currentIndex + rowCount, 0, Items.Count - 1);
        TopItem = Items[nextIndex];
        ScheduleHideNativeScrollbars();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (Items.Count == 0)
            return;

        var wheelNotches = Math.Max(1, Math.Abs(e.Delta) / SystemInformation.MouseWheelScrollDelta);
        var configuredLines = SystemInformation.MouseWheelScrollLines;
        var rowsPerNotch = configuredLines <= 0 || configuredLines == -1 ? 3 : configuredLines;
        var direction = e.Delta > 0 ? -1 : 1;
        ScrollRows(direction * rowsPerNotch * wheelNotches);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.PageDown or Keys.PageUp)
        {
            var visibleRows = Math.Max(1, ClientSize.Height / Math.Max(1, SmallImageList?.ImageSize.Height ?? Font.Height + 12));
            ScrollRows(e.KeyCode == Keys.PageDown ? visibleRows : -visibleRows);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
        SelectedItems.Cast<ListViewItem>().FirstOrDefault()?.EnsureVisible();
        ScheduleHideNativeScrollbars();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ScheduleHideNativeScrollbars();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ScheduleHideNativeScrollbars();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _hideTimer.Dispose();
        base.Dispose(disposing);
    }

    [DllImport("user32.dll")]
    private static extern bool ShowScrollBar(IntPtr windowHandle, int scrollBar, bool show);
}
