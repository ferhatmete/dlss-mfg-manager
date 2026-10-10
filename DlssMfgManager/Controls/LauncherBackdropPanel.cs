using System.Drawing.Drawing2D;
using DlssMfgManager.Services;

namespace DlssMfgManager.Controls;

public sealed class LauncherBackdropPanel : Panel
{
    public LauncherBackdropPanel()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var bounds = ClientRectangle;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var background = new LinearGradientBrush(bounds,
                   Color.FromArgb(247, 249, 253), Color.FromArgb(235, 239, 248),
                   LinearGradientMode.ForwardDiagonal))
            e.Graphics.FillRectangle(background, bounds);

        DrawGlow(e.Graphics, new Rectangle(bounds.Width - 520, -260, 760, 620),
            Color.FromArgb(36, 127, 136, 246));
        DrawGlow(e.Graphics, new Rectangle(-330, bounds.Height - 410, 680, 600),
            Color.FromArgb(28, 95, 218, 201));
    }

    private static void DrawGlow(Graphics graphics, Rectangle bounds, Color color)
    {
        using var path = new GraphicsPath();
        path.AddEllipse(bounds);
        using var brush = new PathGradientBrush(path)
        {
            CenterColor = color,
            SurroundColors = [Color.Transparent]
        };
        graphics.FillPath(brush, path);
    }
}
