using System.Diagnostics;
using DlssMfgManager.Controls;
using DlssMfgManager.Models;
using DlssMfgManager.Services;

namespace DlssMfgManager;

public sealed class FrameGenerationGamesForm : Form
{
    private readonly IReadOnlyList<GameEntry> _installedGames;
    private readonly TextBox _searchText = new();
    private readonly CheckBox _installedOnly = new();
    private readonly DataGridView _grid = new();
    private readonly Label _countLabel = new();
    private readonly ModernButton _webCheckButton = new();

    public FrameGenerationGamesForm(IReadOnlyList<GameEntry> installedGames)
    {
        _installedGames = installedGames;
        Text = L("Frame Generation destekli oyunlar", "Frame Generation supported games");
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 560);
        Size = new Size(880, 680);
        Font = new Font("Segoe UI", 9F);
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.TextPrimary;
        AppTheme.ApplyDarkTitleBar(this);
        ShowInTaskbar = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            RowCount = 6,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            AutoSize = true,
            Text = L("DLSS Frame Generation oyun kataloğu", "DLSS Frame Generation game catalogue"),
            Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(0, 0, 0, 6)
        });

        root.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(810, 0),
            Text = L(
                "ÖNEMLİ: Bu liste kesin veya eksiksiz değildir. Oyun burada görünse bile kurulumdan önce güncel DLSS Frame Generation ve dlssg_for_sm86 uyumluluğunu mutlaka internette kontrol edin. " +
                "Çevrimiçi/anti-cheat oyunlarında modu kullanmayın.",
                "IMPORTANT: This list is neither definitive nor exhaustive. Even when a game appears here, always verify its current DLSS Frame Generation and dlssg_for_sm86 compatibility online before installing. " +
                "Do not use the mod in online or anti-cheat games."),
            ForeColor = AppTheme.WarningText,
            Margin = new Padding(0, 0, 0, 12)
        });

        var filter = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2 };
        filter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _searchText.Dock = DockStyle.Fill;
        _searchText.PlaceholderText = L("Oyun ara...", "Search games...");
        _searchText.Margin = new Padding(0, 0, 12, 6);
        AppTheme.StyleInput(_searchText);
        _searchText.TextChanged += (_, _) => RefreshRows();
        filter.Controls.Add(_searchText, 0, 0);
        _installedOnly.Text = L("Yalnızca listemde olanlar", "Only games in my list");
        _installedOnly.AutoSize = true;
        _installedOnly.ForeColor = AppTheme.TextSecondary;
        _installedOnly.CheckedChanged += (_, _) => RefreshRows();
        filter.Controls.Add(_installedOnly, 1, 0);
        root.Controls.Add(filter);

        _countLabel.AutoSize = true;
        _countLabel.ForeColor = AppTheme.TextMuted;
        _countLabel.Margin = new Padding(0, 0, 0, 6);
        root.Controls.Add(_countLabel);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.BackgroundColor = AppTheme.Input;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        AppTheme.StyleDataGridView(_grid);
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = L("Oyun", "Game"),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L("Bilgisayarınız", "Your PC"), Width = 125 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L("Doğrulama", "Verification"), Width = 235 });
        _grid.SelectionChanged += (_, _) => _webCheckButton.Enabled = _grid.SelectedRows.Count == 1;
        root.Controls.Add(_grid);

        var bottom = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 4 };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _webCheckButton.Text = L("Seçili Oyunu İnternette Kontrol Et", "Check Selected Game Online");
        StyleButton(_webCheckButton, false);
        _webCheckButton.Click += (_, _) => SearchSelectedGameOnline();
        bottom.Controls.Add(_webCheckButton, 1, 0);
        var source = NewButton(L("NVIDIA DLSS Sayfasını Aç", "Open NVIDIA DLSS Page"), true);
        source.Click += (_, _) => Process.Start(new ProcessStartInfo(CompatibilityCatalog.NvidiaGamesUrl) { UseShellExecute = true });
        bottom.Controls.Add(source, 2, 0);
        var close = NewButton(L("Kapat", "Close"), false);
        close.DialogResult = DialogResult.OK;
        bottom.Controls.Add(close, 3, 0);
        root.Controls.Add(bottom);
        AcceptButton = close;
        CancelButton = close;

        RefreshRows();
    }

    private void RefreshRows()
    {
        var query = _searchText.Text.Trim();
        var localNamesMissingFromCatalogue = _installedGames
            .Select(game => game.Name)
            .Where(name => !CompatibilityCatalog.FrameGenerationGames.Any(catalogueName => NamesMatch(catalogueName, name)));
        var rows = CompatibilityCatalog.FrameGenerationGames
            .Select(name => new { Name = name, Installed = IsInstalled(name), InCatalogue = true })
            .Concat(localNamesMissingFromCatalogue.Select(name => new { Name = name, Installed = true, InCatalogue = false }))
            .GroupBy(game => game.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Where(game => string.IsNullOrWhiteSpace(query) ||
                           game.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .Where(game => !_installedOnly.Checked || game.Installed)
            .OrderBy(game => game.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        _grid.Rows.Clear();
        foreach (var game in rows)
        {
            var index = _grid.Rows.Add(game.Name,
                game.Installed ? L("Listede ✓", "In list ✓") : L("Bulunamadı", "Not found"),
                game.InCatalogue
                    ? L("Katalogda · İnternetten kontrol et", "In catalogue · Check online")
                    : L("Yerel oyun · İnternetten doğrula", "Local game · Verify online"));
            if (game.Installed)
                _grid.Rows[index].DefaultCellStyle.ForeColor = AppTheme.StatusReady;
        }
        if (_grid.Rows.Count > 0)
            _grid.Rows[0].Selected = true;
        _webCheckButton.Enabled = _grid.SelectedRows.Count == 1;
        _countLabel.Text = L(
            $"{rows.Count} oyun gösteriliyor · Çevrimdışı liste: Eylül 2026",
            $"Showing {rows.Count} games · Offline list: September 2026");
    }

    private bool IsInstalled(string catalogName)
    {
        var normalizedCatalogName = NormalizeName(catalogName);
        return _installedGames.Any(game =>
        {
            var normalizedGameName = NormalizeName(game.Name);
            var normalizedExeName = NormalizeName(Path.GetFileNameWithoutExtension(game.ExecutablePath));
            return normalizedGameName == normalizedCatalogName ||
                   (normalizedCatalogName.Length >= 5 &&
                    (normalizedGameName.Contains(normalizedCatalogName, StringComparison.OrdinalIgnoreCase) ||
                     normalizedCatalogName.Contains(normalizedGameName, StringComparison.OrdinalIgnoreCase))) ||
                   (normalizedExeName.Length >= 5 && normalizedCatalogName.Contains(normalizedExeName, StringComparison.OrdinalIgnoreCase));
        });
    }

    private static bool NamesMatch(string first, string second)
    {
        var normalizedFirst = NormalizeName(first);
        var normalizedSecond = NormalizeName(second);
        return normalizedFirst == normalizedSecond ||
               (normalizedFirst.Length >= 5 && normalizedSecond.Length >= 5 &&
                (normalizedFirst.Contains(normalizedSecond, StringComparison.OrdinalIgnoreCase) ||
                 normalizedSecond.Contains(normalizedFirst, StringComparison.OrdinalIgnoreCase)));
    }

    private void SearchSelectedGameOnline()
    {
        if (_grid.SelectedRows.Count != 1 || _grid.SelectedRows[0].Cells[0].Value is not string gameName)
            return;

        var query = Uri.EscapeDataString($"{gameName} DLSS Frame Generation dlssg_for_sm86 compatibility");
        Process.Start(new ProcessStartInfo($"https://www.google.com/search?q={query}") { UseShellExecute = true });
    }

    private static string NormalizeName(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

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
            Margin = new Padding(8, 8, 0, 0)
        };
        button.FlatAppearance.BorderSize = secondary ? 1 : 0;
        button.FlatAppearance.BorderColor = AppTheme.Border;
        button.CornerRadius = 9;
        button.BorderColor = secondary ? AppTheme.Border : Color.FromArgb(95, AppTheme.Accent);
        button.HoverBackColor = secondary ? Color.FromArgb(38, 50, 74) : AppTheme.PrimaryHover;
        return button;
    }

    private static void StyleButton(Button button, bool secondary)
    {
        button.AutoSize = true;
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = secondary ? AppTheme.SurfaceAlt : AppTheme.Primary;
        button.ForeColor = Color.White;
        button.Padding = new Padding(10, 5, 10, 5);
        button.Margin = new Padding(8, 8, 0, 0);
        button.FlatAppearance.BorderSize = secondary ? 1 : 0;
        button.FlatAppearance.BorderColor = AppTheme.Border;
        if (button is ModernButton modern)
        {
            modern.CornerRadius = 9;
            modern.BorderColor = secondary ? AppTheme.Border : Color.FromArgb(95, AppTheme.Accent);
            modern.HoverBackColor = secondary ? Color.FromArgb(38, 50, 74) : AppTheme.PrimaryHover;
        }
    }

    private static string L(string turkish, string english) => Localization.Text(turkish, english);
}
