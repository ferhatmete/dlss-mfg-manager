using DlssMfgManager.Models;
using DlssMfgManager.Services;
using DlssMfgManager.Controls;

namespace DlssMfgManager;

public sealed class GameDiscoveryForm : Form
{
    private readonly CheckedListBox _gamesList = new();

    public IReadOnlyList<DiscoveredGame> SelectedGames => _gamesList.CheckedItems
        .Cast<DiscoveredGame>()
        .ToList();

    public GameDiscoveryForm(IReadOnlyList<DiscoveredGame> games)
    {
        Text = L("Bulunan oyunlar", "Discovered games");
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 520);
        Size = new Size(860, 590);
        Font = new Font("Segoe UI", 9F);
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.TextPrimary;
        AppTheme.ApplyDarkTitleBar(this);
        ShowInTaskbar = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            RowCount = 4,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            AutoSize = true,
            Text = L($"{games.Count} oyun bulundu", $"{games.Count} games found"),
            Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(0, 0, 0, 6)
        });

        root.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(790, 0),
            Text = L(
                "EXE seçimi otomatik tahmindir; özellikle launcher kullanan oyunlarda yolu kontrol edin. " +
                "Çevrimiçi veya anti-cheat kullanan oyunların işaretini kaldırın. Bu adım hiçbir oyun dosyasını değiştirmez.",
                "Executable selection is an automatic estimate; verify the path, especially for games that use a launcher. " +
                "Uncheck online or anti-cheat games. This step does not modify any game files."),
            ForeColor = AppTheme.WarningText,
            Margin = new Padding(0, 0, 0, 12)
        });

        _gamesList.Dock = DockStyle.Fill;
        _gamesList.CheckOnClick = true;
        _gamesList.HorizontalScrollbar = true;
        _gamesList.IntegralHeight = false;
        AppTheme.StyleInput(_gamesList);
        _gamesList.BorderStyle = BorderStyle.FixedSingle;
        foreach (var game in games)
            _gamesList.Items.Add(game, true);
        root.Controls.Add(_gamesList);

        var actionBar = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            Margin = new Padding(0, 12, 0, 0)
        };
        actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var selectAll = NewButton(L("Tümünü Seç", "Select All"), true);
        selectAll.Click += (_, _) => SetAllChecked(true);
        actionBar.Controls.Add(selectAll, 0, 0);

        var selectNone = NewButton(L("Seçimi Kaldır", "Select None"), true);
        selectNone.Click += (_, _) => SetAllChecked(false);
        actionBar.Controls.Add(selectNone, 1, 0);

        var cancel = NewButton(L("İptal", "Cancel"), true);
        cancel.DialogResult = DialogResult.Cancel;
        actionBar.Controls.Add(cancel, 3, 0);

        var add = NewButton(L("Seçilenleri Ekle", "Add Selected"), false);
        add.DialogResult = DialogResult.OK;
        actionBar.Controls.Add(add, 4, 0);
        root.Controls.Add(actionBar);

        AcceptButton = add;
        CancelButton = cancel;
    }

    private void SetAllChecked(bool value)
    {
        for (var i = 0; i < _gamesList.Items.Count; i++)
            _gamesList.SetItemChecked(i, value);
    }

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
            Margin = new Padding(0, 0, 8, 0)
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
