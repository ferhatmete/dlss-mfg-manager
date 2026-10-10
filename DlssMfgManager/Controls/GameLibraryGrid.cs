using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using DlssMfgManager.Models;
using DlssMfgManager.Services;

namespace DlssMfgManager.Controls;

public sealed record GameLibraryCardData(
    GameEntry Game,
    string Status,
    InstallState State,
    string? ArtworkPath);

public sealed class GameLibraryGrid : Panel
{
    private readonly LibraryFlowPanel _flow = new();
    private readonly Dictionary<Guid, GameLibraryCard> _cards = [];
    private Guid? _selectedId;

    public event EventHandler<GameEntry>? GameSelected;

    public int ScrollOffset => Math.Max(0, -_flow.AutoScrollPosition.Y);

    public GameLibraryGrid()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Padding = Padding.Empty;

        _flow.Dock = DockStyle.Fill;
        _flow.AutoScroll = true;
        _flow.WrapContents = true;
        _flow.FlowDirection = FlowDirection.LeftToRight;
        _flow.BackColor = Color.Transparent;
        _flow.Padding = Padding.Empty;
        _flow.Margin = Padding.Empty;
        _flow.WheelScrolled += (_, delta) => ScrollByPixels(delta > 0 ? -92 : 92);
        Controls.Add(_flow);

        Resize += (_, _) => ResizeCards();
    }

    public void SetGames(IEnumerable<GameLibraryCardData> games, Guid? selectedId)
    {
        _flow.SuspendLayout();
        var previousCards = _flow.Controls.Cast<Control>().ToArray();
        _flow.Controls.Clear();
        foreach (var control in previousCards)
            control.Dispose();
        _cards.Clear();

        foreach (var data in games)
        {
            var card = new GameLibraryCard(data);
            card.Click += (_, _) => SelectGame(data.Game.Id, notify: true);
            card.MouseWheel += (_, e) => ScrollByPixels(e.Delta > 0 ? -92 : 92);
            foreach (Control child in card.Controls)
                child.Click += (_, _) => SelectGame(data.Game.Id, notify: true);
            _cards[data.Game.Id] = card;
            _flow.Controls.Add(card);
        }

        _selectedId = selectedId is not null && _cards.ContainsKey(selectedId.Value)
            ? selectedId
            : _cards.Keys.Cast<Guid?>().FirstOrDefault();
        ApplySelection();
        ResizeCards();
        _flow.ResumeLayout(true);
        ScheduleHideScrollbars();
    }

    public void SetArtwork(Guid gameId, string? path)
    {
        if (_cards.TryGetValue(gameId, out var card))
            card.SetArtwork(path);
    }

    public void ScrollByPixels(int pixels)
    {
        if (!_flow.IsHandleCreated)
            return;

        var current = Math.Max(0, -_flow.AutoScrollPosition.Y);
        var maximum = Math.Max(0, _flow.VerticalScroll.Maximum - _flow.ClientSize.Height);
        _flow.AutoScrollPosition = new Point(0, Math.Clamp(current + pixels, 0, maximum));
        ScheduleHideScrollbars();
    }

    private void SelectGame(Guid id, bool notify)
    {
        if (!_cards.TryGetValue(id, out var selected))
            return;

        _selectedId = id;
        ApplySelection();
        if (notify)
            GameSelected?.Invoke(this, selected.Game);
    }

    private void ApplySelection()
    {
        foreach (var (id, card) in _cards)
            card.Selected = id == _selectedId;
    }

    private void ResizeCards()
    {
        if (_cards.Count == 0 || ClientSize.Width <= 0)
            return;

        var available = Math.Max(150, _flow.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 14);
        var columns = Math.Clamp(available / 174, 1, 5);
        var width = Math.Max(138, (available - columns * 10) / columns);
        var preferredHeight = Math.Clamp((int)(width * 0.82F), 112, 176);
        var height = _flow.ClientSize.Height < 170
            ? Math.Max(118, _flow.ClientSize.Height - 4)
            : preferredHeight;
        foreach (var card in _cards.Values)
        {
            card.Width = width;
            card.Height = height;
            card.Margin = new Padding(0, 0, 10, 10);
        }

        ScheduleHideScrollbars();
    }

    private void ScheduleHideScrollbars()
    {
        if (!_flow.IsHandleCreated)
            return;
        BeginInvoke(() =>
        {
            if (!_flow.IsDisposed && _flow.IsHandleCreated)
            {
                _ = ShowScrollBar(_flow.Handle, 0, false);
                _ = ShowScrollBar(_flow.Handle, 1, true);
            }
        });
    }

    [DllImport("user32.dll")]
    private static extern bool ShowScrollBar(IntPtr windowHandle, int scrollBar, bool show);

    private sealed class LibraryFlowPanel : FlowLayoutPanel
    {
        public event EventHandler<int>? WheelScrolled;

        public LibraryFlowPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        protected override void OnMouseWheel(MouseEventArgs e) => WheelScrolled?.Invoke(this, e.Delta);
    }
}

internal sealed class GameLibraryCard : Control
{
    private readonly GameLibraryCardData _data;
    private Image? _artwork;
    private bool _hovered;
    private bool _selected;

    public GameEntry Game => _data.Game;

    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value)
                return;
            _selected = value;
            Invalidate();
        }
    }

    public GameLibraryCard(GameLibraryCardData data)
    {
        _data = data;
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        SetArtwork(data.ArtworkPath);
    }

    public void SetArtwork(string? path)
    {
        var next = LoadUnlocked(path);
        var old = _artwork;
        _artwork = next;
        old?.Dispose();
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _artwork?.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        var bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var cardPath = RoundedRectangle(bounds, 12);
        using var cardFill = new SolidBrush(_hovered ? AppTheme.SurfaceRaised : AppTheme.SurfaceAlt);
        e.Graphics.FillPath(cardFill, cardPath);

        var imageHeight = Math.Max(54, Height - 58);
        var imageArea = new Rectangle(0, 0, Width - 1, imageHeight);
        var clipState = e.Graphics.Save();
        e.Graphics.SetClip(cardPath);
        e.Graphics.IntersectClip(imageArea);
        if (_artwork is not null)
        {
            var target = Cover(_artwork.Size, imageArea);
            e.Graphics.DrawImage(_artwork, target);
        }
        else
        {
            using var fallback = new LinearGradientBrush(imageArea,
                Color.FromArgb(224, 229, 244), Color.FromArgb(243, 246, 252), LinearGradientMode.ForwardDiagonal);
            e.Graphics.FillRectangle(fallback, imageArea);
            using var glow = new SolidBrush(Color.FromArgb(38, AppTheme.Primary));
            e.Graphics.FillEllipse(glow, Width - 82, -22, 108, 108);
            var initials = Initials(_data.Game.Name);
            using var initialsFont = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            TextRenderer.DrawText(e.Graphics, initials, initialsFont, imageArea,
                Color.FromArgb(190, AppTheme.TextSecondary),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
        using (var shade = new LinearGradientBrush(imageArea, Color.Transparent,
                   Color.FromArgb(232, AppTheme.SurfaceAlt), LinearGradientMode.Vertical))
            e.Graphics.FillRectangle(shade, imageArea);
        e.Graphics.Restore(clipState);

        var titleArea = new Rectangle(11, imageHeight, Math.Max(1, Width - 22), 26);
        using var titleFont = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, _data.Game.Name, titleFont, titleArea, AppTheme.TextPrimary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        var statusColor = StatusColor(_data.State);
        using var dot = new SolidBrush(statusColor);
        e.Graphics.FillEllipse(dot, 11, Height - 20, 7, 7);
        var statusArea = new Rectangle(24, Height - 26, Math.Max(1, Width - 70), 20);
        TextRenderer.DrawText(e.Graphics, _data.Status, Font, statusArea, AppTheme.TextMuted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        var badgeArea = new Rectangle(Width - 45, Height - 27, 34, 20);
        using var badgeFont = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, $"{_data.Game.Multiplier}X", badgeFont,
            badgeArea, AppTheme.AccentBlue,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        if (_selected)
        {
            using var border = new Pen(AppTheme.Primary, 2F);
            e.Graphics.DrawPath(border, cardPath);
            using var accent = new SolidBrush(AppTheme.Primary);
            e.Graphics.FillRectangle(accent, 12, 0, Math.Max(1, Width - 24), 3);
        }
    }

    private static Image? LoadUnlocked(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var source = Image.FromStream(stream);
            return new Bitmap(source);
        }
        catch
        {
            return null;
        }
    }

    private static Rectangle Cover(Size image, Rectangle bounds)
    {
        var scale = Math.Max((float)bounds.Width / image.Width, (float)bounds.Height / image.Height);
        var width = (int)Math.Ceiling(image.Width * scale);
        var height = (int)Math.Ceiling(image.Height * scale);
        return new Rectangle(bounds.X + (bounds.Width - width) / 2,
            bounds.Y + (bounds.Height - height) / 2, width, height);
    }

    private static string Initials(string value)
    {
        var parts = value.Split([' ', '-', ':'], StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
    }

    private static Color StatusColor(InstallState state) => state switch
    {
        InstallState.Installed => AppTheme.StatusReady,
        InstallState.SourceUnavailable => AppTheme.StatusWarning,
        InstallState.NeedsRepair => AppTheme.StatusRepair,
        InstallState.Error => AppTheme.StatusError,
        _ => AppTheme.TextMuted
    };

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Max(2, radius * 2);
        var arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
