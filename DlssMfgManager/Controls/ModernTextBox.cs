using System.Drawing.Drawing2D;
using DlssMfgManager.Services;

namespace DlssMfgManager.Controls;

public sealed class ModernTextBox : UserControl
{
    private readonly TextBox _textBox = new();
    private readonly Label _placeholderLabel = new();

    public string PlaceholderText
    {
        get => _placeholderLabel.Text;
        set
        {
            _textBox.PlaceholderText = value;
            _placeholderLabel.Text = value;
            UpdatePlaceholder();
        }
    }

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value ?? string.Empty;
    }

    public ModernTextBox()
    {
        DoubleBuffered = true;
        Height = 38;
        MinimumSize = new Size(80, 38);
        BackColor = AppTheme.Input;
        ForeColor = AppTheme.TextPrimary;
        Padding = new Padding(12, 9, 12, 7);
        Margin = Padding.Empty;
        Cursor = Cursors.IBeam;
        TabStop = false;

        _textBox.BorderStyle = BorderStyle.None;
        _textBox.Dock = DockStyle.Fill;
        _textBox.BackColor = AppTheme.Input;
        _textBox.ForeColor = AppTheme.TextPrimary;
        _textBox.Font = new Font("Segoe UI", 9.25F);
        _textBox.TabStop = false;
        _textBox.TextChanged += (_, _) =>
        {
            UpdatePlaceholder();
            OnTextChanged(EventArgs.Empty);
        };
        _textBox.GotFocus += (_, _) => { UpdatePlaceholder(); Invalidate(); };
        _textBox.LostFocus += (_, _) => { UpdatePlaceholder(); Invalidate(); };
        Controls.Add(_textBox);

        _placeholderLabel.Dock = DockStyle.Fill;
        _placeholderLabel.BackColor = AppTheme.Input;
        _placeholderLabel.ForeColor = AppTheme.TextMuted;
        _placeholderLabel.Font = new Font("Segoe UI", 9.25F);
        _placeholderLabel.TextAlign = ContentAlignment.MiddleLeft;
        _placeholderLabel.Cursor = Cursors.IBeam;
        _placeholderLabel.Click += (_, _) => BeginEdit();
        Controls.Add(_placeholderLabel);
        _placeholderLabel.BringToFront();

        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnClick(EventArgs e)
    {
        BeginEdit();
        base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using var path = RoundedRectangle(bounds, 9);
        using var border = new Pen(_textBox.Focused ? AppTheme.PrimaryHover : AppTheme.Border);
        e.Graphics.DrawPath(border, path);
        base.OnPaint(e);
    }

    protected override void OnEnter(EventArgs e)
    {
        _textBox.Focus();
        Invalidate();
        base.OnEnter(e);
    }

    protected override void OnLeave(EventArgs e)
    {
        Invalidate();
        base.OnLeave(e);
    }

    private void BeginEdit()
    {
        _placeholderLabel.Visible = false;
        _textBox.Visible = true;
        _textBox.Focus();
    }

    private void UpdatePlaceholder()
    {
        var showPlaceholder = string.IsNullOrEmpty(_textBox.Text) && !_textBox.Focused;
        _placeholderLabel.Visible = showPlaceholder;
        _textBox.Visible = !showPlaceholder;
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
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
