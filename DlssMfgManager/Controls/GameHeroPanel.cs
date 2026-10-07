using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using DlssMfgManager.Services;

namespace DlssMfgManager.Controls;

public sealed class GameHeroPanel : Panel
{
    private Image? _artwork;

    public GameHeroPanel()
    {
        DoubleBuffered = true;
        BackColor = AppTheme.Surface;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
    }

    public void SetArtwork(string? path)
    {
        var next = LoadUnlocked(path);
        var previous = _artwork;
        _artwork = next;
        previous?.Dispose();
        Invalidate(true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _artwork?.Dispose();
            _artwork = null;
        }
        base.Dispose(disposing);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        var bounds = ClientRectangle;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        using (var baseGradient = new LinearGradientBrush(bounds, AppTheme.SurfaceAlt, AppTheme.Surface,
                   LinearGradientMode.ForwardDiagonal))
            e.Graphics.FillRectangle(baseGradient, bounds);

        if (_artwork is not null)
        {
            var target = CalculateCoverRectangle(_artwork.Size, bounds);
            using var attributes = new ImageAttributes();
            attributes.SetColorMatrix(new ColorMatrix { Matrix33 = 0.52F });
            e.Graphics.DrawImage(_artwork, target, 0, 0, _artwork.Width, _artwork.Height,
                GraphicsUnit.Pixel, attributes);
        }

        using (var shade = new LinearGradientBrush(bounds,
                   Color.FromArgb(118, AppTheme.Background),
                   Color.FromArgb(238, AppTheme.Background),
                   LinearGradientMode.Vertical))
            e.Graphics.FillRectangle(shade, bounds);

        using var sideShade = new LinearGradientBrush(bounds,
            Color.FromArgb(35, AppTheme.Background), Color.FromArgb(185, AppTheme.Background),
            LinearGradientMode.Horizontal);
        e.Graphics.FillRectangle(sideShade, bounds);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var border = new Pen(Color.FromArgb(150, AppTheme.Border));
        e.Graphics.DrawRectangle(border, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
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

    private static Rectangle CalculateCoverRectangle(Size image, Rectangle bounds)
    {
        var scale = Math.Max((float)bounds.Width / image.Width, (float)bounds.Height / image.Height);
        var width = (int)Math.Ceiling(image.Width * scale);
        var height = (int)Math.Ceiling(image.Height * scale);
        return new Rectangle(bounds.X + (bounds.Width - width) / 2,
            bounds.Y + (bounds.Height - height) / 2, width, height);
    }
}
