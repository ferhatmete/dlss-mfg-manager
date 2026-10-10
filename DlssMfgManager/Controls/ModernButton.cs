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
        // A UserPaint Button otherwise leaves the pixels outside the rounded
        // path uninitialised on the real Win32 surface (they show up as black
        // hooks/lines, especially at 125% DPI). Paint the actual parent first.
        if (Parent is not null)
            ButtonRenderer.DrawParentBackground(e.Graphics, ClientRectangle, this);
        else
            e.Graphics.Clear(SystemColors.Control);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        var radius = Math.Min(CornerRadius, Math.Min(bounds.Width, bounds.Height) / 2);
        using var path = RoundedRectangle(bounds, radius);

        var baseColor = Enabled ? BackColor : Blend(BackColor, Color.White, 0.44f);
        if (Enabled && _pressed)
            baseColor = PressedBackColor.IsEmpty ? Blend(BackColor, Color.Black, 0.22f) : PressedBackColor;
        else if (Enabled && _hovered)
            baseColor = HoverBackColor.IsEmpty ? Blend(BackColor, Color.White, 0.12f) : HoverBackColor;

        using (var fill = new SolidBrush(baseColor))
            e.Graphics.FillPath(fill, path);

        if (BorderColor.A > 0)
        {
            using var border = new Pen(BorderColor, 1F);
            e.Graphics.DrawPath(border, path);
        }

        var textColor = Enabled ? ForeColor : Color.FromArgb(125, ForeColor);
        var textBounds = new Rectangle(bounds.X + Padding.Left, bounds.Y,
            Math.Max(1, bounds.Width - Padding.Horizontal), bounds.Height);
        var alignment = TextAlign switch
        {
            ContentAlignment.MiddleLeft or ContentAlignment.TopLeft or ContentAlignment.BottomLeft => TextFormatFlags.Left,
            ContentAlignment.MiddleRight or ContentAlignment.TopRight or ContentAlignment.BottomRight => TextFormatFlags.Right,
            _ => TextFormatFlags.HorizontalCenter
        };
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, textColor,
            alignment | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        if (Focused && ShowFocusCues)
        {
            var focus = Rectangle.Inflate(bounds, -3, -3);
            using var focusPath = RoundedRectangle(focus, Math.Max(3, radius - 3));
            using var focusPen = new Pen(Color.FromArgb(255, 150, 156, 247), 1.5F);
            e.Graphics.DrawPath(focusPen, focusPath);
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
