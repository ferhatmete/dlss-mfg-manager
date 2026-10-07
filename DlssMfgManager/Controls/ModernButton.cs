using System.Drawing.Drawing2D;

namespace DlssMfgManager.Controls;

public sealed class ModernButton : Button
{
    private bool _hovered;
    private bool _pressed;

    public Color HoverBackColor { get; set; } = Color.Empty;
    public Color PressedBackColor { get; set; } = Color.Empty;
    public Color BorderColor { get; set; } = Color.Transparent;
    public int CornerRadius { get; set; } = 10;

    public ModernButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
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
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        var radius = Math.Min(CornerRadius, Math.Min(bounds.Width, bounds.Height) / 2);
        using var path = RoundedRectangle(bounds, radius);

        var baseColor = Enabled ? BackColor : Blend(BackColor, Color.Black, 0.48f);
        if (Enabled && _pressed)
            baseColor = PressedBackColor.IsEmpty ? Blend(BackColor, Color.Black, 0.22f) : PressedBackColor;
        else if (Enabled && _hovered)
            baseColor = HoverBackColor.IsEmpty ? Blend(BackColor, Color.White, 0.12f) : HoverBackColor;

        var lowerColor = Blend(baseColor, Color.Black, 0.10f);
        using (var fill = new LinearGradientBrush(bounds, baseColor, lowerColor, LinearGradientMode.Vertical))
            e.Graphics.FillPath(fill, path);

        if (BorderColor.A > 0)
        {
            using var border = new Pen(BorderColor, 1F);
            e.Graphics.DrawPath(border, path);
        }

        var textColor = Enabled ? ForeColor : Color.FromArgb(125, ForeColor);
        TextRenderer.DrawText(e.Graphics, Text, Font, bounds, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        if (Focused && ShowFocusCues)
        {
            var focus = Rectangle.Inflate(bounds, -4, -4);
            ControlPaint.DrawFocusRectangle(e.Graphics, focus, textColor, Color.Transparent);
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
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
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

    private static Color Blend(Color first, Color second, float amount)
    {
        amount = Math.Clamp(amount, 0F, 1F);
        return Color.FromArgb(
            (int)(first.A + (second.A - first.A) * amount),
            (int)(first.R + (second.R - first.R) * amount),
            (int)(first.G + (second.G - first.G) * amount),
            (int)(first.B + (second.B - first.B) * amount));
    }
}
