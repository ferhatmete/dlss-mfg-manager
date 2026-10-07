using System.Diagnostics;
using DlssMfgManager.Controls;
using DlssMfgManager.Services;

namespace DlssMfgManager;

public sealed class SupportedCardsForm : Form
{
    public SupportedCardsForm()
    {
        Text = L("Desteklenen ekran kartları", "Supported GPUs");
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(740, 520);
        Size = new Size(820, 620);
        Font = new Font("Segoe UI", 9F);
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.TextPrimary;
        AppTheme.ApplyDarkTitleBar(this);
        ShowInTaskbar = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            RowCount = 5,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            AutoSize = true,
            Text = L("Desteklenen RTX 20 ve RTX 30 kartları", "Supported RTX 20 and RTX 30 GPUs"),
            Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(0, 0, 0, 6)
        });

        root.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(750, 0),
            Text = L(
                "dlssg_for_sm86, RTX 20 serisinin SM75 ve RTX 30 serisinin SM86 mimarilerini hedefler. " +
                "Kartın listede olması her oyunla kesin uyumluluk garantisi vermez; oyun tarafında DLSS Frame Generation entegrasyonu gerekir.",
                "dlssg_for_sm86 targets the SM75 architecture of the RTX 20 series and the SM86 architecture of the RTX 30 series. " +
                "A listed GPU does not guarantee compatibility with every game; the game must include a DLSS Frame Generation integration."),
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(0, 0, 0, 12)
        });

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoGenerateColumns = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
            BackgroundColor = AppTheme.Input,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        AppTheme.StyleDataGridView(grid);
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L("Mimari", "Architecture"), Width = 125 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L("Ekran kartı", "GPU"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L("Tür", "Type"), Width = 125 });
        foreach (var gpu in CompatibilityCatalog.SupportedGpus)
            grid.Rows.Add(gpu.Series, gpu.Model, gpu.FormFactor == "Desktop" ? L("Masaüstü", "Desktop") : gpu.FormFactor);
        root.Controls.Add(grid);

        root.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(750, 0),
            Text = L(
                "Not: Upstream proje SM75 / RTX 20 yolunu deneysel olarak tanımlar. Laptop üreticisinin güç/BIOS yapılandırması, NVIDIA sürücüsü ve oyunun Streamline/DLSSG sürümü sonucu etkileyebilir. " +
                "RTX 2050 Laptop gibi farklı adlandırılan SM86 modelleri mimari olarak yakın olsa da bu listede doğrulanmış ailelere dahil edilmemiştir.",
                "Note: The upstream project describes the SM75 / RTX 20 path as experimental. Laptop power/BIOS configuration, the NVIDIA driver, and the game's Streamline/DLSSG version can affect the result. " +
                "Differently named SM86 products such as the RTX 2050 Laptop are architecturally related but are not included among the verified families in this list."),
            ForeColor = AppTheme.WarningText,
            Margin = new Padding(0, 12, 0, 8)
        });

        var bottom = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        var close = NewButton(L("Kapat", "Close"), false);
        close.DialogResult = DialogResult.OK;
        bottom.Controls.Add(close);
        var source = NewButton(L("Proje Kaynağını Aç", "Open Project Source"), true);
        source.Click += (_, _) => OpenUrl(CompatibilityCatalog.UpstreamProjectUrl);
        bottom.Controls.Add(source);
        root.Controls.Add(bottom);
        AcceptButton = close;
        CancelButton = close;
    }

    private static void OpenUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    private static Button NewButton(string text, bool secondary)
    {
        var button = new ModernButton
        {
            Text = text,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = secondary ? AppTheme.SurfaceAlt : AppTheme.Primary,
            ForeColor = Color.White,
            Padding = new Padding(10, 5, 10, 5),
            Margin = new Padding(8, 0, 0, 0)
        };
        button.FlatAppearance.BorderSize = secondary ? 1 : 0;
        button.FlatAppearance.BorderColor = AppTheme.Border;
        button.CornerRadius = 9;
        button.BorderColor = secondary ? AppTheme.Border : Color.FromArgb(95, AppTheme.Accent);
        button.HoverBackColor = secondary ? Color.FromArgb(38, 50, 74) : AppTheme.PrimaryHover;
        return button;
    }

    private static string L(string turkish, string english) => Localization.Text(turkish, english);
}
