using System.Drawing.Drawing2D;
using DlssMfgManager.Services;

namespace DlssMfgManager.Controls;

public sealed class GlassCardPanel : Panel
{
    public Color FillColor { get; set; } = AppTheme.Surface;
    public Color BorderColor { get; set; } = Color.Transparent;
    public int CornerRadius { get; set; } = 14;

    public GlassCardPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Padding = new Padding(1);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // First let WinForms paint the parent behind the transparent corners. A
        // binary window Region leaves aliased/black pixels at non-integer DPI
        // scales, which is why the old cards looked fine in DrawToBitmap but
        // gained broken dark edges in the real window.
        base.OnPaintBackground(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var inset = BorderColor.A == 255 ? 1 : 0;
        var bounds = new Rectangle(inset, inset,
            Math.Max(1, Width - 1 - inset * 2), Math.Max(1, Height - 1 - inset * 2));
        using var path = RoundedRectangle(bounds, Math.Min(CornerRadius, Math.Min(Width, Height) / 2));
        using var fill = new SolidBrush(Color.FromArgb(255, FillColor.R, FillColor.G, FillColor.B));
        e.Graphics.FillPath(fill, path);

        if (BorderColor.A == 255)
        {
            var opaqueBorder = Color.FromArgb(255, BorderColor.R, BorderColor.G, BorderColor.B);
            using var border = new Pen(opaqueBorder, 1F);
            e.Graphics.DrawPath(border, path);
        }
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var diameter = radius * 2;
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
