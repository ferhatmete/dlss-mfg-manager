using System.Diagnostics;
using DlssMfgManager.Controls;
using DlssMfgManager.Models;
using DlssMfgManager.Services;

namespace DlssMfgManager;

public sealed class MainForm : Form
{
    private readonly AppStateStore _stateStore = new();
    private readonly InstallationService _installationService = new();
    private readonly GameDiscoveryService _gameDiscoveryService = new();
    private readonly GameArtworkService _artworkService;
    private readonly AppState _state;

    private readonly TextBox _sourceFolderText = new();
    private readonly ListView _gamesList = new();
    private readonly ComboBox _multiplierCombo = new();
    private readonly CheckBox _autoRepairCheck = new();
    private readonly Label _selectedGameLabel = new();
    private readonly Label _statusLabel = new();
    private readonly Label _detailsLabel = new();
    private readonly ModernButton _installButton = new();
    private readonly ModernButton _uninstallButton = new();
    private readonly ModernButton _launchButton = new();
    private readonly ComboBox _languageCombo = new();
    private readonly ToolTip _buttonToolTip = new();
    private readonly GameHeroPanel _heroPanel = new();
    private readonly CheckBox _onlineArtworkCheck = new();
    private readonly Label _artworkStatusLabel = new();
    private readonly ModernButton _refreshArtworkButton = new();
    private readonly ImageList _gameRowHeight = new() { ImageSize = new Size(1, 32), ColorDepth = ColorDepth.Depth32Bit };
    private readonly Font _narrowActionFont = new("Segoe UI Semibold", 8.25F, FontStyle.Bold);
    private readonly Font _regularActionFont = new("Segoe UI Semibold", 9F, FontStyle.Bold);
    private TableLayoutPanel? _actionButtonsLayout;
    private CancellationTokenSource? _artworkCancellation;
    private bool _updatingActionLayout;
    private bool _updatingControls;

    public MainForm()
    {
        _artworkService = new GameArtworkService(_stateStore.DataDirectory);
        _state = _stateStore.Load();
        Localization.Use(_state.Language);
        Text = "DLSS MFG Manager v1.3";
        MinimumSize = new Size(940, 700);
        Size = new Size(1040, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.TextPrimary;
        AppTheme.ApplyDarkTitleBar(this);

        BuildInterface();
        _sourceFolderText.Text = _state.PackageSourceFolder;
        RefreshGameList();
        UpdateSelectionPanel();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _artworkCancellation?.Cancel();
        _artworkCancellation?.Dispose();
        _artworkService.Dispose();
        _gameRowHeight.Dispose();
        _narrowActionFont.Dispose();
        _regularActionFont.Dispose();
        SaveState();
        base.OnFormClosing(e);
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 5,
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var title = new Label
        {
            AutoSize = true,
            Text = "DLSS MFG Manager v1.3",
            Font = new Font("Segoe UI Semibold", 21F, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(0, 0, 0, 4)
        };
        header.Controls.Add(title, 0, 0);

        root.Controls.Add(header, 0, 0);

        var subtitle = new Label
        {
            AutoSize = true,
            Text = L("Desteklenen RTX 20 ve RTX 30 serisi kartlarda Frame Generation kurulumlarını oyun bazında yönetin.",
                "Manage Frame Generation installations per game on supported RTX 20 and RTX 30 series GPUs."),
            ForeColor = AppTheme.TextMuted,
            Margin = new Padding(1, 0, 0, 14)
        };
        root.Controls.Add(subtitle, 0, 1);

        root.Controls.Add(BuildNavigationPanel(), 0, 2);

        var warningPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = AppTheme.WarningBackground,
            Margin = new Padding(0, 0, 0, 14),
            Padding = new Padding(14, 9, 14, 8)
        };
        warningPanel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = L(
                "⚠  Yalnızca çevrimdışı / tek oyunculu oyunlarda kullanın. Anti-cheat kullanan veya çevrimiçi oyunlarda proxy DLL kullanımı ban ya da hesap yaptırımı riski oluşturabilir.",
                "⚠  Use only in offline / single-player games. Using a proxy DLL in anti-cheat or online games may result in a ban or other account penalties."),
            ForeColor = AppTheme.WarningText,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        });
        root.Controls.Add(warningPanel, 0, 3);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(content, 0, 4);

        content.Controls.Add(BuildPackagePanel(), 0, 0);
        content.SetColumnSpan(content.GetControlFromPosition(0, 0)!, 2);
        content.Controls.Add(BuildGamesPanel(), 0, 1);
        content.Controls.Add(BuildDetailsPanel(), 1, 1);
    }

    private Control BuildNavigationPanel()
    {
        var navigation = new TableLayoutPanel
        {
            AutoSize = false,
            Height = 38,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 10)
        };
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0)
        };
        var supportedCardsButton = NewButton(L("Desteklenen Kartlar", "Supported GPUs"), secondary: true);
        supportedCardsButton.Click += (_, _) =>
        {
            using var dialog = new SupportedCardsForm();
            dialog.ShowDialog(this);
        };
        var supportedGamesButton = NewButton(L("Frame Gen Oyun Listesi", "Frame Gen Game List"));
        supportedGamesButton.Click += (_, _) =>
        {
            using var dialog = new FrameGenerationGamesForm(_state.Games);
            dialog.ShowDialog(this);
        };
        buttons.Controls.AddRange([supportedCardsButton, supportedGamesButton]);
        navigation.Controls.Add(buttons, 0, 0);

        _languageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageCombo.Items.Clear();
        _languageCombo.Items.AddRange(["Türkçe", "English"]);
        _languageCombo.SelectedIndex = string.Equals(_state.Language, "en", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        _languageCombo.Width = 105;
        _languageCombo.Margin = new Padding(8, 4, 0, 0);
        AppTheme.StyleInput(_languageCombo);
        _languageCombo.SelectedIndexChanged += LanguageChanged;
        navigation.Controls.Add(_languageCombo, 1, 0);
        return navigation;
    }

    private Control BuildPackagePanel()
    {
        var group = NewGroupBox(L("Frame Gen dosyaları", "Frame Gen files"), new Padding(12));
        group.Dock = DockStyle.Top;
        group.Height = 82;
        group.Margin = new Padding(0, 0, 0, 12);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        group.Controls.Add(layout);

        _sourceFolderText.Dock = DockStyle.Fill;
        _sourceFolderText.ReadOnly = true;
        _sourceFolderText.PlaceholderText = L(
            "Frame Gen dosyaları henüz seçilmedi (version.dll + dlssg_sm86.ini)",
            "Frame Gen files have not been selected yet (version.dll + dlssg_sm86.ini)");
        AppTheme.StyleInput(_sourceFolderText);
        _sourceFolderText.Margin = new Padding(0, 5, 8, 5);
        layout.Controls.Add(_sourceFolderText, 0, 0);

        var importButton = NewButton(L("Frame Gen Dosyalarını Seç", "Select Frame Gen Files"));
        importButton.Click += (_, _) => ImportPackageFiles();
        layout.Controls.Add(importButton, 1, 0);

        var browseButton = NewButton(L("Klasörden Kullan", "Use Folder"), secondary: true);
        browseButton.Click += (_, _) => SelectSourceFolder();
        layout.Controls.Add(browseButton, 2, 0);

        var openButton = NewButton(L("Konumu Aç", "Open Location"), secondary: true);
        openButton.Click += (_, _) => OpenSourceFolder();
        layout.Controls.Add(openButton, 3, 0);
        return group;
    }

    private Control BuildGamesPanel()
    {
        var group = NewGroupBox(L("Oyunlar", "Games"), new Padding(10));
        group.Dock = DockStyle.Fill;
        group.Margin = new Padding(0, 0, 10, 0);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        group.Controls.Add(layout);

        _gamesList.Dock = DockStyle.Fill;
        _gamesList.View = View.Details;
        _gamesList.FullRowSelect = true;
        _gamesList.HideSelection = false;
        _gamesList.MultiSelect = false;
        _gamesList.BorderStyle = BorderStyle.FixedSingle;
        AppTheme.StyleListView(_gamesList);
        _gamesList.SmallImageList = _gameRowHeight;
        _gamesList.Columns.Add(L("Oyun", "Game"), 210);
        _gamesList.Columns.Add("MFG", 65);
        _gamesList.Columns.Add(L("Durum", "Status"), 200);
        _gamesList.SelectedIndexChanged += (_, _) => UpdateSelectionPanel();
        _gamesList.DoubleClick += (_, _) => LaunchSelectedGame();
        layout.Controls.Add(_gamesList, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 8, 0, 0)
        };
        var addButton = NewButton(L("+ Oyun Ekle", "+ Add Game"), secondary: true);
        addButton.Click += (_, _) => AddGame();
        var scanButton = NewButton(L("Oyunları Otomatik Tara", "Auto-Scan Games"));
        scanButton.Click += async (_, _) => await ScanForGamesAsync();
        var removeButton = NewButton(L("Listeden Kaldır", "Remove from List"), secondary: true);
        removeButton.Click += (_, _) => RemoveSelectedGame();
        var refreshButton = NewButton(L("Yenile", "Refresh"), secondary: true);
        refreshButton.Click += (_, _) => RefreshGameList(SelectedGame?.Id);
        buttons.Controls.AddRange([addButton, scanButton, removeButton, refreshButton]);
        layout.Controls.Add(buttons, 0, 1);
        return group;
    }

    private Control BuildDetailsPanel()
    {
        var group = NewGroupBox(L("Seçili oyun", "Selected game"), new Padding(7, 8, 7, 7));
        group.Dock = DockStyle.Fill;
        group.Margin = new Padding(10, 0, 0, 0);

        _heroPanel.Dock = DockStyle.Fill;
        _heroPanel.Margin = Padding.Empty;
        group.Controls.Add(_heroPanel);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 11,
            ColumnCount = 1,
            Padding = new Padding(14, 10, 14, 10),
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _heroPanel.Controls.Add(layout);

        _selectedGameLabel.AutoSize = true;
        _selectedGameLabel.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
        _selectedGameLabel.ForeColor = AppTheme.TextPrimary;
        _selectedGameLabel.BackColor = Color.Transparent;
        _selectedGameLabel.Margin = new Padding(0, 0, 0, 2);
        layout.Controls.Add(_selectedGameLabel, 0, 0);

        _artworkStatusLabel.AutoSize = true;
        _artworkStatusLabel.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
        _artworkStatusLabel.ForeColor = AppTheme.Accent;
        _artworkStatusLabel.BackColor = Color.Transparent;
        _artworkStatusLabel.Margin = new Padding(0, 0, 0, 10);
        layout.Controls.Add(_artworkStatusLabel, 0, 1);

        layout.Controls.Add(NewFieldLabel(L("MFG üst sınırı", "MFG maximum")), 0, 2);
        _multiplierCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _multiplierCombo.Items.AddRange(["2X", "3X", "4X"]);
        _multiplierCombo.Width = 120;
        _multiplierCombo.Margin = new Padding(0, 2, 0, 6);
        AppTheme.StyleInput(_multiplierCombo);
        _multiplierCombo.SelectedIndexChanged += (_, _) => MultiplierChanged();
        layout.Controls.Add(_multiplierCombo, 0, 3);

        _autoRepairCheck.Text = L("Başlatmadan önce otomatik kontrol et ve onar",
            "Automatically check and repair before launch");
        _autoRepairCheck.AutoSize = true;
        _autoRepairCheck.Margin = new Padding(0, 0, 0, 8);
        _autoRepairCheck.CheckedChanged += (_, _) => AutoRepairChanged();
        _autoRepairCheck.BackColor = Color.Transparent;
        layout.Controls.Add(_autoRepairCheck, 0, 4);

        _onlineArtworkCheck.Text = L("İnternetten oyun görseli kullan", "Use online game artwork");
        _onlineArtworkCheck.AutoSize = true;
        _onlineArtworkCheck.ForeColor = AppTheme.TextSecondary;
        _onlineArtworkCheck.BackColor = Color.Transparent;
        _onlineArtworkCheck.Margin = new Padding(0, 0, 0, 8);
        _onlineArtworkCheck.CheckedChanged += (_, _) => OnlineArtworkChanged();
        layout.Controls.Add(_onlineArtworkCheck, 0, 5);

        layout.Controls.Add(NewFieldLabel(L("Kurulum durumu", "Installation status")), 0, 6);
        _statusLabel.AutoSize = true;
        _statusLabel.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
        _statusLabel.Margin = new Padding(0, 2, 0, 2);
        _statusLabel.BackColor = Color.Transparent;
        layout.Controls.Add(_statusLabel, 0, 7);

        _detailsLabel.AutoSize = false;
        _detailsLabel.Dock = DockStyle.Fill;
        _detailsLabel.AutoEllipsis = true;
        _detailsLabel.ForeColor = AppTheme.TextMuted;
        _detailsLabel.Margin = new Padding(0, 0, 0, 6);
        _detailsLabel.BackColor = Color.Transparent;
        layout.Controls.Add(_detailsLabel, 0, 8);

        var artworkBar = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 5)
        };
        _refreshArtworkButton.Text = L("Görseli Yenile", "Refresh Artwork");
        StyleButton(_refreshArtworkButton, secondary: true);
        _refreshArtworkButton.AutoSize = true;
        _refreshArtworkButton.Height = 32;
        _refreshArtworkButton.Padding = new Padding(10, 3, 10, 3);
        _refreshArtworkButton.Margin = Padding.Empty;
        _refreshArtworkButton.Click += async (_, _) => await UpdateArtworkAsync(SelectedGame, forceRefresh: true);
        artworkBar.Controls.Add(_refreshArtworkButton);
        layout.Controls.Add(artworkBar, 0, 9);

        var actions = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0)
        };
        actions.MinimumSize = new Size(300, 32);
        _actionButtonsLayout = actions;
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        actions.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _installButton.Text = L("Kur / Güncelle", "Install / Update");
        StyleButton(_installButton, false);
        _installButton.Dock = DockStyle.Top;
        _installButton.AutoSize = false;
        _installButton.Height = 40;
        _installButton.Padding = Padding.Empty;
        _installButton.Margin = new Padding(0, 1, 4, 1);
        _installButton.Click += (_, _) => InstallSelectedGame();
        actions.Controls.Add(_installButton, 0, 0);

        _uninstallButton.Text = L("Frame Gen Dosyalarını Sil", "Remove Frame Gen Files");
        StyleButton(_uninstallButton, false);
        _uninstallButton.Dock = DockStyle.Top;
        _uninstallButton.AutoSize = false;
        _uninstallButton.Height = 40;
        _uninstallButton.Padding = Padding.Empty;
        _uninstallButton.Margin = new Padding(2, 1, 2, 1);
        _uninstallButton.BackColor = AppTheme.Danger;
        _uninstallButton.HoverBackColor = Color.FromArgb(248, 78, 78);
        _uninstallButton.PressedBackColor = Color.FromArgb(190, 35, 35);
        _uninstallButton.Click += (_, _) => UninstallSelectedGame();
        actions.Controls.Add(_uninstallButton, 1, 0);

        _launchButton.Text = L("Oyunu Başlat", "Launch Game");
        StyleButton(_launchButton, false);
        _launchButton.Dock = DockStyle.Top;
        _launchButton.AutoSize = false;
        _launchButton.Height = 40;
        _launchButton.Padding = Padding.Empty;
        _launchButton.Margin = new Padding(4, 1, 0, 1);
        _launchButton.BackColor = AppTheme.Success;
        _launchButton.HoverBackColor = Color.FromArgb(31, 204, 148);
        _launchButton.PressedBackColor = Color.FromArgb(5, 135, 95);
        _launchButton.Click += (_, _) => LaunchSelectedGame();
        actions.Controls.Add(_launchButton, 2, 0);
        layout.Controls.Add(actions, 0, 10);
        actions.SizeChanged += (_, _) => UpdateActionButtonLayout();
        UpdateActionButtonLayout();

        return group;
    }

    private void UpdateActionButtonLayout()
    {
        if (_actionButtonsLayout is null || _updatingActionLayout)
            return;

        _updatingActionLayout = true;
        try
        {
            var width = _actionButtonsLayout.ClientSize.Width;
            var stacked = width < 285;
            var narrow = width < 360;
            _actionButtonsLayout.SuspendLayout();
            _actionButtonsLayout.RowStyles.Clear();
            _actionButtonsLayout.ColumnStyles.Clear();

            if (stacked)
            {
                _actionButtonsLayout.RowCount = 3;
                _actionButtonsLayout.ColumnCount = 3;
                _actionButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                _actionButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
                _actionButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
                _actionButtonsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                _actionButtonsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                _actionButtonsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                PlaceActionButton(_installButton, 0, 0, 3, new Padding(0, 1, 0, 2));
                PlaceActionButton(_uninstallButton, 0, 1, 3, new Padding(0, 1, 0, 2));
                PlaceActionButton(_launchButton, 0, 2, 3, new Padding(0, 1, 0, 1));
            }
            else
            {
                _actionButtonsLayout.RowCount = 1;
                _actionButtonsLayout.ColumnCount = 3;
                _actionButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, narrow ? 28 : 29));
                _actionButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, narrow ? 47 : 46));
                _actionButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
                _actionButtonsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                PlaceActionButton(_installButton, 0, 0, 1, new Padding(0, 1, narrow ? 2 : 4, 1));
                PlaceActionButton(_uninstallButton, 1, 0, 1, new Padding(narrow ? 1 : 2, 1, narrow ? 1 : 2, 1));
                PlaceActionButton(_launchButton, 2, 0, 1, new Padding(narrow ? 2 : 4, 1, 0, 1));
            }

            var actionFont = narrow ? _narrowActionFont : _regularActionFont;
            _installButton.Font = actionFont;
            _uninstallButton.Font = actionFont;
            _launchButton.Font = actionFont;
            _installButton.Text = L("Kur / Güncelle", "Install / Update");
            _uninstallButton.Text = L("Frame Gen Dosyalarını Sil", "Remove Frame Gen Files");
            _launchButton.Text = L("Oyunu Başlat", "Launch Game");
            _actionButtonsLayout.ResumeLayout(true);
        }
        finally
        {
            _updatingActionLayout = false;
        }

        _buttonToolTip.SetToolTip(_installButton, L("Frame Gen kur veya güncelle", "Install or update Frame Gen"));
        _buttonToolTip.SetToolTip(_uninstallButton, L("Frame Gen dosyalarını sil", "Remove Frame Gen files"));
        _buttonToolTip.SetToolTip(_launchButton, L("Oyunu başlat", "Launch the game"));
    }

    private void PlaceActionButton(Button button, int column, int row, int columnSpan, Padding margin)
    {
        if (_actionButtonsLayout is null)
            return;

        _actionButtonsLayout.SetColumnSpan(button, 1);
        _actionButtonsLayout.SetCellPosition(button, new TableLayoutPanelCellPosition(column, row));
        _actionButtonsLayout.SetColumnSpan(button, columnSpan);
        button.Margin = margin;
    }

    private GameEntry? SelectedGame => _gamesList.SelectedItems.Count == 1
        ? _gamesList.SelectedItems[0].Tag as GameEntry
        : null;

    private void LanguageChanged(object? sender, EventArgs e)
    {
        var languageCode = _languageCombo.SelectedIndex == 1 ? "en" : "tr";
        if (string.Equals(_state.Language, languageCode, StringComparison.OrdinalIgnoreCase))
            return;

        _state.Language = languageCode;
        Localization.Use(languageCode);
        SaveState();
        MessageBox.Show(this,
            L("Dil değişikliği uygulanıyor. Uygulama yeniden başlatılacak.",
                "Applying the language change. The application will restart."),
            "DLSS MFG Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Application.Restart();
        Close();
    }

    private void SelectSourceFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = L(
                "Frame Gen dosyalarını içeren klasörü seçin (version.dll + dlssg_sm86.ini)",
                "Select the folder containing the Frame Gen files (version.dll + dlssg_sm86.ini)"),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        if (!InstallationService.TryValidateSource(dialog.SelectedPath, out var error))
        {
            ShowError(error);
            return;
        }

        _state.PackageSourceFolder = dialog.SelectedPath;
        _sourceFolderText.Text = dialog.SelectedPath;
        SaveState();
        RefreshGameList(SelectedGame?.Id);
    }

    private void ImportPackageFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Title = L("Frame Gen dosyalarını seçin: version.dll ve dlssg_sm86.ini",
                "Select the Frame Gen files: version.dll and dlssg_sm86.ini"),
            Filter = $"{L("Gerekli dosyalar", "Required files")}|version.dll;dlssg_sm86.ini|{L("Tüm dosyalar", "All files")}|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var selectedByName = dialog.FileNames.ToDictionary(
            path => Path.GetFileName(path)!,
            StringComparer.OrdinalIgnoreCase);
        if (!selectedByName.ContainsKey(InstallationService.ProxyFileName) ||
            !selectedByName.ContainsKey(InstallationService.IniFileName))
        {
            ShowError(L(
                "Frame Gen için version.dll ve dlssg_sm86.ini dosyalarını aynı anda seçin.",
                "Select version.dll and dlssg_sm86.ini together for Frame Gen."));
            return;
        }

        try
        {
            Directory.CreateDirectory(_stateStore.ImportedPackageDirectory);
            foreach (var fileName in new[] { InstallationService.ProxyFileName, InstallationService.IniFileName })
                File.Copy(selectedByName[fileName], Path.Combine(_stateStore.ImportedPackageDirectory, fileName), true);

            _state.PackageSourceFolder = _stateStore.ImportedPackageDirectory;
            _sourceFolderText.Text = _state.PackageSourceFolder;
            SaveState();
            RefreshGameList(SelectedGame?.Id);
            ShowInfo(L(
                "Frame Gen dosyaları uygulamanın yerel veri klasörüne aktarıldı.",
                "The Frame Gen files were imported into the application's local data folder."));
        }
        catch (Exception ex)
        {
            ShowError($"{L("Dosyalar içe aktarılamadı.", "The files could not be imported.")}{Environment.NewLine}{ex.Message}");
        }
    }

    private void OpenSourceFolder()
    {
        if (!Directory.Exists(_state.PackageSourceFolder))
        {
            ShowError(L("Önce Frame Gen dosyalarını seçin.", "Select the Frame Gen files first."));
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = _state.PackageSourceFolder,
            UseShellExecute = true
        });
    }

    private void AddGame()
    {
        using var dialog = new OpenFileDialog
        {
            Title = L("Oyunun gerçek render .exe dosyasını seçin", "Select the game's actual rendering .exe"),
            Filter = $"{L("Windows uygulaması", "Windows application")} (*.exe)|*.exe",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var fullPath = Path.GetFullPath(dialog.FileName);
        var existing = _state.Games.FirstOrDefault(g =>
            Path.GetFullPath(g.ExecutablePath).Equals(fullPath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            RefreshGameList(existing.Id);
            return;
        }

        var game = new GameEntry
        {
            Name = FileVersionInfo.GetVersionInfo(fullPath).FileDescription is { Length: > 0 } description
                ? description
                : Path.GetFileNameWithoutExtension(fullPath),
            ExecutablePath = fullPath
        };
        _state.Games.Add(game);
        SaveState();
        RefreshGameList(game.Id);
    }

    private async Task ScanForGamesAsync()
    {
        try
        {
            Enabled = false;
            UseWaitCursor = true;
            var discovered = await Task.Run(_gameDiscoveryService.Scan);
            var existingPaths = _state.Games
                .Select(game => Path.GetFullPath(game.ExecutablePath))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var newGames = discovered
                .Where(game => !existingPaths.Contains(Path.GetFullPath(game.ExecutablePath)))
                .ToList();

            Enabled = true;
            UseWaitCursor = false;

            if (newGames.Count == 0)
            {
                ShowInfo(discovered.Count == 0
                    ? L("Steam veya Epic Games kütüphanelerinde uygun 64-bit oyun EXE'si bulunamadı.",
                        "No suitable 64-bit game executable was found in the Steam or Epic Games libraries.")
                    : L("Bulunan oyunların tamamı zaten listede.", "All discovered games are already in the list."));
                return;
            }

            using var dialog = new GameDiscoveryForm(newGames);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var selected = dialog.SelectedGames;
            foreach (var discoveredGame in selected)
            {
                _state.Games.Add(new GameEntry
                {
                    Name = discoveredGame.Name,
                    ExecutablePath = discoveredGame.ExecutablePath,
                    Multiplier = 4,
                    AutoRepairBeforeLaunch = true,
                    SteamAppId = discoveredGame.SteamAppId
                });
            }

            if (selected.Count > 0)
            {
                SaveState();
                RefreshGameList(_state.Games[^1].Id);
                ShowInfo(L(
                    $"{selected.Count} oyun listeye eklendi. Kurulum yapılmadan önce EXE yolunu ve anti-cheat durumunu kontrol edin.",
                    $"{selected.Count} games were added. Check the executable path and anti-cheat status before installing."));
            }
        }
        catch (Exception ex)
        {
            ShowError($"{L("Oyun taraması tamamlanamadı.", "The game scan could not be completed.")}{Environment.NewLine}{ex.Message}");
        }
        finally
        {
            Enabled = true;
            UseWaitCursor = false;
        }
    }

    private void RemoveSelectedGame()
    {
        var game = SelectedGame;
        if (game is null)
            return;

        var answer = MessageBox.Show(this,
            L($"{game.Name} yalnızca uygulama listesinden kaldırılsın mı?\n\nOyun klasöründeki kurulum dosyalarına dokunulmayacak.",
                $"Remove {game.Name} only from the application list?\n\nFiles in the game folder will not be changed."),
            L("Listeden kaldır", "Remove from list"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes)
            return;

        _state.Games.Remove(game);
        SaveState();
        RefreshGameList();
    }

    private void MultiplierChanged()
    {
        if (_updatingControls || SelectedGame is not { } game || _multiplierCombo.SelectedIndex < 0)
            return;

        game.Multiplier = _multiplierCombo.SelectedIndex + 2;
        SaveState();
        RefreshGameList(game.Id);
    }

    private void AutoRepairChanged()
    {
        if (_updatingControls || SelectedGame is not { } game)
            return;

        game.AutoRepairBeforeLaunch = _autoRepairCheck.Checked;
        SaveState();
    }

    private void InstallSelectedGame()
    {
        if (SelectedGame is not { } game)
            return;

        var answer = MessageBox.Show(this,
            L(
                $"{game.Name} için güncel Frame Generation ve dlssg_for_sm86 uyumluluğunu internette kontrol ettiniz mi?\n\nÇevrimdışı katalog kesin veya eksiksiz değildir. Anti-cheat ya da çevrimiçi oyunlarda kurulum yapmayın.",
                $"Have you checked the current Frame Generation and dlssg_for_sm86 compatibility for {game.Name} online?\n\nThe offline catalogue is neither definitive nor exhaustive. Do not install in anti-cheat or online games."),
            L("İnternet doğrulaması gerekli", "Online verification required"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (answer != DialogResult.Yes)
            return;

        RunAction(() => _installationService.Install(game, _state.PackageSourceFolder), game.Id);
    }

    private void UninstallSelectedGame()
    {
        if (SelectedGame is not { } game)
            return;

        var answer = MessageBox.Show(this,
            L(
                "Frame Gen kurulumu kaldırılacak. Uygulamanın eklediği dosyalar silinecek; önceden var olan dosyalar varsa yedekten geri yüklenecek. Devam edilsin mi?",
                "The Frame Gen installation will be removed. Files added by the application will be deleted, and any pre-existing files will be restored from backup. Continue?"),
            L("Frame Gen Dosyalarını Sil", "Remove Frame Gen Files"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (answer != DialogResult.Yes)
            return;

        RunAction(() => _installationService.RestoreOrRemove(game), game.Id);
    }

    private void LaunchSelectedGame()
    {
        if (SelectedGame is not { } game)
            return;

        try
        {
            if (game.AutoRepairBeforeLaunch)
            {
                var inspection = _installationService.Inspect(game, _state.PackageSourceFolder);
                if (inspection.State is InstallState.NotInstalled or InstallState.NeedsRepair or InstallState.Error)
                    _installationService.Repair(game, _state.PackageSourceFolder);
            }

            var finalInspection = _installationService.Inspect(game, _state.PackageSourceFolder);
            if (finalInspection.State is InstallState.NotInstalled or InstallState.NeedsRepair or InstallState.Error)
                throw new InvalidOperationException(
                    $"{L("Oyun başlatılmadı", "The game was not launched")}: {finalInspection.Summary}. {finalInspection.Details}");

            _installationService.Launch(game);
            RefreshGameList(game.Id);
        }
        catch (Exception ex)
        {
            ShowError($"{L("Oyun başlatılamadı.", "The game could not be launched.")}{Environment.NewLine}{ex.Message}");
        }
    }

    private void RunAction(Func<string> action, Guid selectedId)
    {
        try
        {
            UseWaitCursor = true;
            var message = action();
            RefreshGameList(selectedId);
            ShowInfo(message);
        }
        catch (UnauthorizedAccessException ex)
        {
            ShowError($"{L("Oyun klasörüne yazma izni yok. Uygulamayı yönetici olarak çalıştırmayı deneyin.",
                "The game folder is not writable. Try running the application as administrator.")}{Environment.NewLine}{ex.Message}");
        }
        catch (IOException ex)
        {
            ShowError($"{L("Dosya işlemi tamamlanamadı. Oyun açıksa tamamen kapatın.",
                "The file operation could not be completed. Fully close the game if it is running.")}{Environment.NewLine}{ex.Message}");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void RefreshGameList(Guid? selectedId = null)
    {
        selectedId ??= SelectedGame?.Id;
        _gamesList.BeginUpdate();
        _gamesList.Items.Clear();
        foreach (var game in _state.Games.OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            var inspection = _installationService.Inspect(game, _state.PackageSourceFolder);
            var item = new ListViewItem(game.Name) { Tag = game, ForeColor = AppTheme.TextPrimary, UseItemStyleForSubItems = false };
            item.SubItems.Add($"{game.Multiplier}X");
            item.SubItems.Add(inspection.Summary);
            item.SubItems[1].ForeColor = AppTheme.Accent;
            item.SubItems[2].ForeColor = StatusColor(inspection.State);
            _gamesList.Items.Add(item);
            if (game.Id == selectedId)
                item.Selected = true;
        }
        _gamesList.EndUpdate();

        if (_gamesList.SelectedItems.Count == 0 && _gamesList.Items.Count > 0)
            _gamesList.Items[0].Selected = true;
        UpdateSelectionPanel();
    }

    private void UpdateSelectionPanel()
    {
        var game = SelectedGame;
        _updatingControls = true;
        try
        {
            var enabled = game is not null;
            _selectedGameLabel.Text = game?.Name ?? L("Oyun seçilmedi", "No game selected");
            _multiplierCombo.Enabled = enabled;
            _autoRepairCheck.Enabled = enabled;
            _installButton.Enabled = enabled;
            _uninstallButton.Enabled = enabled;
            _launchButton.Enabled = enabled;
            _onlineArtworkCheck.Enabled = enabled;
            _refreshArtworkButton.Enabled = enabled && _state.OnlineArtworkEnabled;
            _onlineArtworkCheck.Checked = _state.OnlineArtworkEnabled;

            if (game is null)
            {
                _artworkCancellation?.Cancel();
                _heroPanel.SetArtwork(null);
                _multiplierCombo.SelectedIndex = -1;
                _autoRepairCheck.Checked = false;
                _statusLabel.Text = "—";
                _detailsLabel.Text = L(
                    "Başlamak için listeden bir oyun seçin veya yeni oyun ekleyin.",
                    "Select a game from the list or add a new game to begin.");
                _artworkStatusLabel.Text = L("OYUN GÖRSELİ YOK", "NO GAME ARTWORK");
                _statusLabel.ForeColor = AppTheme.TextMuted;
                return;
            }

            _multiplierCombo.SelectedIndex = Math.Clamp(game.Multiplier - 2, 0, 2);
            _autoRepairCheck.Checked = game.AutoRepairBeforeLaunch;
            var inspection = _installationService.Inspect(game, _state.PackageSourceFolder);
            _statusLabel.Text = inspection.Summary;
            _detailsLabel.Text = $"{inspection.Details}{Environment.NewLine}{game.ExecutablePath}";
            _statusLabel.ForeColor = StatusColor(inspection.State);
        }
        finally
        {
            _updatingControls = false;
        }

        _ = UpdateArtworkAsync(game, forceRefresh: false);
    }

    private void OnlineArtworkChanged()
    {
        if (_updatingControls)
            return;

        _state.OnlineArtworkEnabled = _onlineArtworkCheck.Checked;
        _refreshArtworkButton.Enabled = _state.OnlineArtworkEnabled && SelectedGame is not null;
        SaveState();
        _ = UpdateArtworkAsync(SelectedGame, forceRefresh: false);
    }

    private async Task UpdateArtworkAsync(GameEntry? game, bool forceRefresh)
    {
        _artworkCancellation?.Cancel();
        _artworkCancellation?.Dispose();
        _artworkCancellation = new CancellationTokenSource();
        var cancellationToken = _artworkCancellation.Token;

        if (game is null || !_state.OnlineArtworkEnabled)
        {
            _heroPanel.SetArtwork(null);
            _artworkStatusLabel.Text = game is null
                ? L("OYUN GÖRSELİ YOK", "NO GAME ARTWORK")
                : L("ÇEVRİMİÇİ GÖRSELLER KAPALI", "ONLINE ARTWORK OFF");
            return;
        }

        _artworkStatusLabel.Text = L("STEAM'DE GÖRSEL ARANIYOR…", "LOOKING FOR ARTWORK ON STEAM…");
        _refreshArtworkButton.Enabled = false;
        try
        {
            var result = await _artworkService.GetArtworkAsync(game, forceRefresh, cancellationToken);
            if (cancellationToken.IsCancellationRequested || SelectedGame?.Id != game.Id)
                return;

            if (result is null)
            {
                _heroPanel.SetArtwork(null);
                _artworkStatusLabel.Text = L("STEAM GÖRSELİ BULUNAMADI", "STEAM ARTWORK NOT FOUND");
                return;
            }

            _heroPanel.SetArtwork(result.LocalPath);
            _artworkStatusLabel.Text = L("STEAM GÖRSELİ • ÖNBELLEĞE ALINDI", "STEAM ARTWORK • CACHED");
            if (game.SteamAppId != result.SteamAppId)
            {
                game.SteamAppId = result.SteamAppId;
                SaveState();
            }
        }
        catch (OperationCanceledException)
        {
            // A newly selected game superseded this request.
        }
        catch
        {
            if (!cancellationToken.IsCancellationRequested && SelectedGame?.Id == game.Id)
            {
                _heroPanel.SetArtwork(null);
                _artworkStatusLabel.Text = L("GÖRSEL İNDİRİLEMEDİ • DEGRADE KULLANILIYOR",
                    "ARTWORK UNAVAILABLE • USING GRADIENT");
            }
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested && SelectedGame?.Id == game.Id)
                _refreshArtworkButton.Enabled = _state.OnlineArtworkEnabled;
        }
    }

    private void SaveState()
    {
        try
        {
            _stateStore.Save(_state);
        }
        catch (Exception ex)
        {
            ShowError($"{L("Ayarlar kaydedilemedi.", "The settings could not be saved.")}{Environment.NewLine}{ex.Message}");
        }
    }

    private static GroupBox NewGroupBox(string text, Padding padding) => new()
    {
        Text = text,
        Padding = padding,
        BackColor = AppTheme.Surface,
        ForeColor = AppTheme.TextSecondary,
        Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
    };

    private static Label NewFieldLabel(string text) => new()
    {
        AutoSize = true,
        Text = text,
        ForeColor = AppTheme.TextSecondary,
        BackColor = Color.Transparent,
        Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
    };

    private static Button NewButton(string text, bool secondary = false)
    {
        var button = new ModernButton { Text = text, AutoSize = true };
        StyleButton(button, secondary);
        return button;
    }

    private static void StyleButton(Button button, bool secondary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = secondary ? 1 : 0;
        button.FlatAppearance.BorderColor = AppTheme.Border;
        button.BackColor = secondary ? AppTheme.SurfaceAlt : AppTheme.Primary;
        button.ForeColor = Color.White;
        button.Padding = new Padding(10, 5, 10, 5);
        button.Margin = new Padding(0, 2, 8, 6);
        button.Cursor = Cursors.Hand;
        button.Height = 36;
        if (button is ModernButton modern)
        {
            modern.CornerRadius = 9;
            modern.BorderColor = secondary ? AppTheme.Border : Color.FromArgb(95, AppTheme.Accent);
            modern.HoverBackColor = secondary ? Color.FromArgb(38, 50, 74) : AppTheme.PrimaryHover;
            modern.PressedBackColor = secondary ? Color.FromArgb(18, 28, 46) : Color.FromArgb(67, 56, 202);
        }
    }

    private static Color StatusColor(InstallState state) => state switch
    {
        InstallState.Installed => AppTheme.StatusReady,
        InstallState.SourceUnavailable => AppTheme.StatusWarning,
        InstallState.NeedsRepair => AppTheme.StatusRepair,
        InstallState.Error => AppTheme.StatusError,
        _ => AppTheme.TextMuted
    };

    private static string L(string turkish, string english) => Localization.Text(turkish, english);

    private void ShowInfo(string message) =>
        MessageBox.Show(this, message, "DLSS MFG Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void ShowError(string message) =>
        MessageBox.Show(this, message, "DLSS MFG Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
