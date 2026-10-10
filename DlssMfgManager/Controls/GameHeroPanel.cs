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
        BackColor = Color.Transparent;
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
        base.OnPaintBackground(e);
        e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var bounds = ClientRectangle;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var roundedBounds = new Rectangle(0, 0, Math.Max(1, bounds.Width - 1), Math.Max(1, bounds.Height - 1));
        using var roundedPath = RoundedRectangle(roundedBounds, 16);
        var roundedState = e.Graphics.Save();
        e.Graphics.SetClip(roundedPath);

        using (var baseGradient = new LinearGradientBrush(bounds, AppTheme.SurfaceAlt, AppTheme.Surface,
                   LinearGradientMode.ForwardDiagonal))
            e.Graphics.FillRectangle(baseGradient, bounds);

        if (_artwork is not null)
        {
            // Steam artwork is normally wide. Keeping it in a dedicated hero band
            // avoids the severe zoom/crop caused by filling this tall details card.
            var heroHeight = Math.Min(bounds.Height, Math.Max(210, (int)Math.Round(bounds.Width * 0.58F)));
            var artworkArea = new Rectangle(0, 0, bounds.Width, heroHeight);
            var target = CalculateCoverRectangle(_artwork.Size, artworkArea);
            using var attributes = new ImageAttributes();
            attributes.SetColorMatrix(new ColorMatrix { Matrix33 = 0.84F });
            var state = e.Graphics.Save();
            e.Graphics.SetClip(artworkArea, CombineMode.Intersect);
            e.Graphics.DrawImage(_artwork, target, 0, 0, _artwork.Width, _artwork.Height,
                GraphicsUnit.Pixel, attributes);
            e.Graphics.Restore(state);
        }

        using (var shade = new LinearGradientBrush(bounds,
                   Color.FromArgb(24, AppTheme.Background),
                   Color.FromArgb(154, AppTheme.Background),
                   LinearGradientMode.Vertical))
            e.Graphics.FillRectangle(shade, bounds);

        using var sideShade = new LinearGradientBrush(bounds,
            Color.FromArgb(192, AppTheme.Background), Color.FromArgb(22, AppTheme.Background),
            LinearGradientMode.Horizontal);
        e.Graphics.FillRectangle(sideShade, bounds);
        e.Graphics.Restore(roundedState);
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
