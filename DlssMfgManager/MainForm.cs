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
    private readonly ModernTextBox _gameSearchText = new();
    private readonly Label _gameCountLabel = new();
    private readonly Label _sourceStatusLabel = new();
    private readonly DarkListView _gamesList = new();
    private readonly GameLibraryGrid _libraryGrid = new();
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
    private readonly ImageList _gameRowHeight = new() { ImageSize = new Size(1, 42), ColorDepth = ColorDepth.Depth32Bit };
    private readonly Font _narrowActionFont = new("Segoe UI Semibold", 8.25F, FontStyle.Bold);
    private readonly Font _regularActionFont = new("Segoe UI Semibold", 9F, FontStyle.Bold);
    private readonly Font _heroTitleLargeFont = new("Segoe UI Semibold", 23F, FontStyle.Bold);
    private readonly Font _heroTitleMediumFont = new("Segoe UI Semibold", 18F, FontStyle.Bold);
    private readonly Font _heroTitleSmallFont = new("Segoe UI Semibold", 15F, FontStyle.Bold);
    private TableLayoutPanel? _actionButtonsLayout;
    private CancellationTokenSource? _artworkCancellation;
    private CancellationTokenSource? _libraryArtworkCancellation;
    private bool _updatingActionLayout;
    private bool _updatingControls;
    private Guid? _selectedGameId;

    public MainForm()
    {
        _artworkService = new GameArtworkService(_stateStore.DataDirectory);
        _state = _stateStore.Load();
        Localization.Use(_state.Language);
        Text = "DLSS MFG Manager v1.9.2";
        MinimumSize = new Size(980, 680);
        Size = new Size(1360, 820);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.TextPrimary;
        AppTheme.ApplyDarkTitleBar(this);

        BuildInterface();
        _sourceFolderText.Text = _state.PackageSourceFolder;
        UpdateSourceStatus();
        RefreshGameList();
        UpdateSelectionPanel();
        Shown += (_, _) => QueueLibraryArtworkPrefetch();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _artworkCancellation?.Cancel();
        _artworkCancellation?.Dispose();
        _libraryArtworkCancellation?.Cancel();
        _libraryArtworkCancellation?.Dispose();
        _artworkService.Dispose();
        _gameRowHeight.Dispose();
        _narrowActionFont.Dispose();
        _regularActionFont.Dispose();
        _heroTitleLargeFont.Dispose();
        _heroTitleMediumFont.Dispose();
        _heroTitleSmallFont.Dispose();
        SaveState();
        base.OnFormClosing(e);
    }

    private void BuildInterface()
    {
        var backdrop = new LauncherBackdropPanel();
        Controls.Add(backdrop);

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 204));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        backdrop.Controls.Add(shell);
        shell.Controls.Add(BuildSidebarPanel(), 0, 0);
        shell.Controls.Add(BuildWorkspacePanel(), 1, 0);
    }

    private Control BuildSidebarPanel()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Sidebar,
            Padding = new Padding(14, 18, 14, 16),
            Margin = Padding.Empty
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sidebar.Controls.Add(layout);

        var brand = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent };
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var logo = new Label
        {
            Dock = DockStyle.Fill,
            Text = "FG",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Black", 13F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = AppTheme.Primary,
            Margin = new Padding(0, 3, 8, 11)
        };
        brand.Controls.Add(logo, 0, 0);
        var brandText = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = Color.Transparent };
        brandText.Controls.Add(new Label
        {
            Dock = DockStyle.Bottom,
            Text = "DLSS MFG",
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 0);
        brandText.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            Text = "MANAGER  •  v1.9.2",
            Font = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold),
            ForeColor = AppTheme.Accent,
            TextAlign = ContentAlignment.TopLeft
        }, 0, 1);
        brand.Controls.Add(brandText, 1, 0);
        layout.Controls.Add(brand, 0, 0);

        layout.Controls.Add(NewSidebarCaption(L("KÜTÜPHANE", "LIBRARY")), 0, 1);
        var libraryButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 0)
        };
        var allGamesButton = NewSidebarButton(L("▦   Oyunlarım", "▦   My Games"), active: true);
        var supportedGamesButton = NewSidebarButton(L("◈   Frame Gen Oyunları", "◈   Frame Gen Games"));
        supportedGamesButton.Click += (_, _) => { using var dialog = new FrameGenerationGamesForm(_state.Games); dialog.ShowDialog(this); };
        var supportedCardsButton = NewSidebarButton(L("◆   Desteklenen Kartlar", "◆   Supported GPUs"));
        supportedCardsButton.Click += (_, _) => { using var dialog = new SupportedCardsForm(); dialog.ShowDialog(this); };
        libraryButtons.Controls.AddRange([allGamesButton, supportedGamesButton, supportedCardsButton]);
        layout.Controls.Add(libraryButtons, 0, 2);

        layout.Controls.Add(NewSidebarCaption(L("FRAME GEN PAKETİ", "FRAME GEN PACKAGE")), 0, 4);
        var package = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, RowCount = 5, BackColor = Color.Transparent };
        _sourceStatusLabel.AutoSize = false;
        _sourceStatusLabel.Height = 48;
        _sourceStatusLabel.Dock = DockStyle.Top;
        _sourceStatusLabel.ForeColor = AppTheme.TextMuted;
        _sourceStatusLabel.Font = new Font("Segoe UI", 8.25F);
        _sourceStatusLabel.AutoEllipsis = true;
        _sourceStatusLabel.Text = L("Dosyalar henüz seçilmedi", "Files not selected yet");
        package.Controls.Add(_sourceStatusLabel);
        var importButton = NewSidebarButton(L("＋  Dosyaları Seç", "＋  Select Files"));
        importButton.Click += (_, _) => ImportPackageFiles();
        package.Controls.Add(importButton);
        var browseButton = NewSidebarButton(L("▣  Klasörden Kullan", "▣  Use Folder"));
        browseButton.Click += (_, _) => SelectSourceFolder();
        package.Controls.Add(browseButton);
        var openButton = NewSidebarButton(L("↗  Konumu Aç", "↗  Open Location"));
        openButton.Click += (_, _) => OpenSourceFolder();
        package.Controls.Add(openButton);
        layout.Controls.Add(package, 0, 5);

        var safety = new GlassCardPanel
        {
            Height = 74,
            Dock = DockStyle.Fill,
            FillColor = AppTheme.WarningBackground,
            BorderColor = Color.Transparent,
            CornerRadius = 10,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 0)
        };
        safety.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = L("⚠  Yalnızca çevrimdışı ve tek oyunculu oyunlarda kullanın.",
                "⚠  Use only in offline and single-player games."),
            ForeColor = AppTheme.WarningText,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        });
        layout.Controls.Add(safety, 0, 6);

        var languagePicker = new TableLayoutPanel
        {
            AutoSize = false,
            Height = 54,
            MaximumSize = new Size(176, 54),
            Dock = DockStyle.Top,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 10, 0, 0)
        };
        languagePicker.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        languagePicker.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        languagePicker.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        languagePicker.Controls.Add(new Label
        {
            AutoSize = true,
            Text = L("DİL SEÇENEĞİ", "LANGUAGE"),
            ForeColor = AppTheme.TextMuted,
            Font = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold),
            Margin = new Padding(8, 0, 0, 4)
        }, 0, 0);

        var selector = new GlassCardPanel
        {
            Dock = DockStyle.Fill,
            FillColor = AppTheme.Input,
            BorderColor = AppTheme.Border,
            CornerRadius = 8,
            Padding = new Padding(1),
            Margin = Padding.Empty
        };
        _languageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageCombo.Items.AddRange(["Türkçe", "English"]);
        _languageCombo.SelectedIndex = string.Equals(_state.Language, "en", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        _languageCombo.SelectedIndexChanged += LanguageChanged;

        var selectedLanguage = new Label
        {
            Dock = DockStyle.Fill,
            Text = _languageCombo.SelectedItem?.ToString() ?? "Türkçe",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = AppTheme.TextPrimary,
            BackColor = AppTheme.Input,
            Cursor = Cursors.Hand,
            Padding = new Padding(12, 0, 0, 0)
        };
        var languageArrow = new Panel
        {
            Name = "LanguageDropdownArrow",
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Input,
            Cursor = Cursors.Hand,
            Margin = Padding.Empty
        };
        languageArrow.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var centerX = languageArrow.ClientSize.Width / 2F;
            var centerY = languageArrow.ClientSize.Height / 2F + 1F;
            using var brush = new SolidBrush(AppTheme.TextSecondary);
            e.Graphics.FillPolygon(brush,
            [
                new PointF(centerX - 4.5F, centerY - 2.5F),
                new PointF(centerX + 4.5F, centerY - 2.5F),
                new PointF(centerX, centerY + 3F)
            ]);
        };
        var languageMenu = new ContextMenuStrip
        {
            Font = new Font("Segoe UI", 9F),
            ShowImageMargin = false
        };
        languageMenu.Items.Add("Türkçe", null, (_, _) => _languageCombo.SelectedIndex = 0);
        languageMenu.Items.Add("English", null, (_, _) => _languageCombo.SelectedIndex = 1);
        void OpenLanguageMenu() => languageMenu.Show(selector, new Point(0, selector.Height));
        selectedLanguage.Click += (_, _) => OpenLanguageMenu();
        languageArrow.Click += (_, _) => OpenLanguageMenu();
        selector.Disposed += (_, _) => languageMenu.Dispose();
        var selectorLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AppTheme.Input,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        selectorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        selectorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30));
        selectorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        selectorLayout.Controls.Add(selectedLanguage, 0, 0);
        selectorLayout.Controls.Add(languageArrow, 1, 0);
        selector.Controls.Add(selectorLayout);
        languagePicker.Controls.Add(selector, 0, 1);
        layout.Controls.Add(languagePicker, 0, 7);
        return sidebar;
    }

    private Control BuildWorkspacePanel()
    {
        var workspace = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(20, 15, 20, 18),
            BackColor = Color.Transparent
        };
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = Color.Transparent };
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        heading.Controls.Add(new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            Text = L("OYUN KÜTÜPHANEM", "MY GAME LIBRARY"),
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary
        });
        heading.Controls.Add(new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            Text = L("Frame Generation kurulumlarını tek merkezden yönet", "Manage Frame Generation installs from one place"),
            ForeColor = AppTheme.TextMuted
        });
        header.Controls.Add(heading, 0, 0);
        var headerActions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = Color.Transparent };
        var scanButton = NewButton(L("⟳  PC'yi Tara", "⟳  Scan PC"));
        scanButton.AutoSize = false;
        scanButton.Width = 122;
        scanButton.Height = 40;
        scanButton.Click += async (_, _) => await ScanForGamesAsync();
        var scanFolderButton = NewButton(L("⌕  Klasör Tara", "⌕  Scan Folder"), secondary: true);
        scanFolderButton.AutoSize = false;
        scanFolderButton.Width = 122;
        scanFolderButton.Height = 40;
        scanFolderButton.Click += async (_, _) => await ScanFolderForGamesAsync();
        var addButton = NewButton(L("＋  Oyun Ekle", "＋  Add Game"), secondary: true);
        addButton.AutoSize = false;
        addButton.Width = 108;
        addButton.Height = 40;
        addButton.Click += (_, _) => AddGame();
        headerActions.Controls.AddRange([scanButton, scanFolderButton, addButton]);
        header.Controls.Add(headerActions, 1, 0);
        workspace.Controls.Add(header, 0, 0);

        var warning = new GlassCardPanel
        {
            Dock = DockStyle.Fill,
            FillColor = AppTheme.WarningBackground,
            BorderColor = Color.Transparent,
            Margin = new Padding(0, 2, 0, 10),
            Padding = new Padding(14, 4, 14, 4)
        };
        warning.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = L("⚠  Anti-cheat veya çevrimiçi oyunlarda kullanmayın. Her oyunun güncel uyumluluğunu internetten kontrol edin.",
                "⚠  Do not use in anti-cheat or online games. Verify current compatibility for every game online."),
            ForeColor = AppTheme.WarningText,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
        });
        workspace.Controls.Add(warning, 0, 1);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        content.Controls.Add(BuildDetailsPanel(), 0, 0);
        content.Controls.Add(BuildGamesPanel(), 0, 1);
        workspace.Controls.Add(content, 0, 2);
        return workspace;
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
        var card = new GlassCardPanel
        {
            Dock = DockStyle.Fill,
            FillColor = AppTheme.Surface,
            BorderColor = Color.Transparent,
            CornerRadius = 16,
            Margin = new Padding(0, 10, 0, 0),
            Padding = new Padding(14)
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Color.Transparent };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        card.Controls.Add(layout);

        var libraryHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        libraryHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        libraryHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        libraryHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        libraryHeader.Controls.Add(new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Text = L("OYUNLARIN", "YOUR GAMES"),
            ForeColor = AppTheme.TextPrimary,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
            Margin = new Padding(0, 0, 14, 0)
        }, 0, 0);
        _gameCountLabel.AutoSize = false;
        _gameCountLabel.Dock = DockStyle.Fill;
        _gameCountLabel.TextAlign = ContentAlignment.MiddleLeft;
        _gameCountLabel.ForeColor = AppTheme.TextMuted;
        _gameCountLabel.Margin = Padding.Empty;
        libraryHeader.Controls.Add(_gameCountLabel, 1, 0);

        _gameSearchText.Dock = DockStyle.None;
        _gameSearchText.Anchor = AnchorStyles.Right;
        _gameSearchText.Size = new Size(320, 38);
        _gameSearchText.PlaceholderText = L("Oyun ara...", "Search games...");
        _gameSearchText.Margin = Padding.Empty;
        _gameSearchText.TextChanged += (_, _) => RefreshGameList(SelectedGame?.Id);
        libraryHeader.Controls.Add(_gameSearchText, 2, 0);
        layout.Controls.Add(libraryHeader, 0, 0);

        _libraryGrid.Dock = DockStyle.Fill;
        _libraryGrid.Margin = Padding.Empty;
        _libraryGrid.GameSelected += (_, game) =>
        {
            _selectedGameId = game.Id;
            UpdateSelectionPanel();
        };
        layout.Controls.Add(_libraryGrid, 0, 1);

        var buttons = new TableLayoutPanel
        {
            AutoSize = false,
            Height = 48,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(0, 6, 0, 0)
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var removeButton = NewButton(L("−  Listeden Kaldır", "−  Remove"), secondary: true);
        removeButton.Click += (_, _) => RemoveSelectedGame();
        removeButton.AutoSize = false;
        removeButton.Width = 145;
        removeButton.Height = 38;
        var refreshButton = NewButton(L("⟳  Durumu Yenile", "⟳  Refresh Status"), secondary: true);
        refreshButton.Click += (_, _) => RefreshGameList(SelectedGame?.Id);
        refreshButton.AutoSize = false;
        refreshButton.Width = 118;
        refreshButton.Height = 38;
        buttons.Controls.Add(removeButton, 0, 0);
        buttons.Controls.Add(refreshButton, 1, 0);
        layout.Controls.Add(buttons, 0, 2);
        return card;
    }

    private void ResizeGameColumns()
    {
        if (_gamesList.Columns.Count != 3 || _gamesList.ClientSize.Width < 120)
            return;

        var usable = Math.Max(120, _gamesList.ClientSize.Width - 18);
        _gamesList.Columns[0].Width = (int)(usable * 0.58);
        _gamesList.Columns[1].Width = (int)(usable * 0.15);
        _gamesList.Columns[2].Width = usable - _gamesList.Columns[0].Width - _gamesList.Columns[1].Width;
        _gamesList.ScheduleHideNativeScrollbars();
    }

    private Control BuildDetailsPanel()
    {
        var card = new GlassCardPanel
        {
            Dock = DockStyle.Fill,
            FillColor = AppTheme.Surface,
            BorderColor = Color.Transparent,
            CornerRadius = 16,
            Margin = Padding.Empty,
            Padding = new Padding(1)
        };

        _heroPanel.Dock = DockStyle.Fill;
        _heroPanel.Margin = Padding.Empty;
        card.Controls.Add(_heroPanel);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 3,
            Padding = new Padding(24, 14, 24, 12),
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _heroPanel.Controls.Add(layout);

        var identity = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 7,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 18, 0)
        };
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        identity.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        identity.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        identity.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        _selectedGameLabel.AutoSize = false;
        _selectedGameLabel.Dock = DockStyle.Fill;
        _selectedGameLabel.AutoEllipsis = true;
        _selectedGameLabel.TextAlign = ContentAlignment.BottomLeft;
        _selectedGameLabel.Font = _heroTitleLargeFont;
        _selectedGameLabel.ForeColor = AppTheme.TextPrimary;
        _selectedGameLabel.BackColor = Color.Transparent;
        _selectedGameLabel.Margin = Padding.Empty;
        identity.Controls.Add(_selectedGameLabel, 0, 0);

        _artworkStatusLabel.AutoSize = false;
        _artworkStatusLabel.Dock = DockStyle.Fill;
        _artworkStatusLabel.AutoEllipsis = true;
        _artworkStatusLabel.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
        _artworkStatusLabel.ForeColor = AppTheme.AccentBlue;
        _artworkStatusLabel.BackColor = Color.Transparent;
        _artworkStatusLabel.Margin = new Padding(1, 0, 0, 0);
        identity.Controls.Add(_artworkStatusLabel, 0, 1);

        identity.Controls.Add(NewFieldLabel(L("KURULUM DURUMU", "INSTALLATION STATUS")), 0, 3);
        _statusLabel.AutoSize = true;
        _statusLabel.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
        _statusLabel.Margin = new Padding(0, 4, 0, 2);
        _statusLabel.BackColor = Color.Transparent;
        identity.Controls.Add(_statusLabel, 0, 4);

        _detailsLabel.AutoSize = false;
        _detailsLabel.Dock = DockStyle.Fill;
        _detailsLabel.AutoEllipsis = true;
        _detailsLabel.ForeColor = AppTheme.TextSecondary;
        _detailsLabel.Margin = Padding.Empty;
        _detailsLabel.BackColor = Color.Transparent;
        identity.Controls.Add(_detailsLabel, 0, 5);
        layout.Controls.Add(identity, 0, 0);

        var settingsCard = new GlassCardPanel
        {
            Dock = DockStyle.Fill,
            FillColor = Color.FromArgb(250, 251, 254),
            BorderColor = Color.Transparent,
            CornerRadius = 13,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 7, 14, 7)
        };
        var settings = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 6,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        settings.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settings.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settings.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
        settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        settings.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        settings.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "FRAME GEN",
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 4)
        }, 0, 0);
        settings.Controls.Add(NewFieldLabel(L("MFG üst sınırı", "MFG maximum")), 0, 1);
        _multiplierCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _multiplierCombo.Items.AddRange(["2X", "3X", "4X"]);
        _multiplierCombo.Dock = DockStyle.Top;
        _multiplierCombo.Margin = new Padding(0, 2, 0, 0);
        AppTheme.StyleInput(_multiplierCombo);
        _multiplierCombo.SelectedIndexChanged += (_, _) => MultiplierChanged();
        settings.Controls.Add(_multiplierCombo, 0, 2);

        _autoRepairCheck.Text = L("Başlatmadan önce onar", "Repair before launch");
        _autoRepairCheck.AutoSize = false;
        _autoRepairCheck.Dock = DockStyle.Fill;
        _autoRepairCheck.Margin = Padding.Empty;
        _autoRepairCheck.CheckedChanged += (_, _) => AutoRepairChanged();
        _autoRepairCheck.BackColor = Color.Transparent;
        settings.Controls.Add(_autoRepairCheck, 0, 4);

        _onlineArtworkCheck.Text = L("Çevrimiçi görseller", "Online artwork");
        _onlineArtworkCheck.AutoSize = false;
        _onlineArtworkCheck.Dock = DockStyle.Fill;
        _onlineArtworkCheck.ForeColor = AppTheme.TextSecondary;
        _onlineArtworkCheck.BackColor = Color.Transparent;
        _onlineArtworkCheck.Margin = Padding.Empty;
        _onlineArtworkCheck.CheckedChanged += (_, _) => OnlineArtworkChanged();
        settings.Controls.Add(_onlineArtworkCheck, 0, 5);
        settingsCard.Controls.Add(settings);
        layout.Controls.Add(settingsCard, 1, 0);

        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 7, 0, 7)
        };
        _actionButtonsLayout = actions;
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        _launchButton.Text = L("▶  Oyunu Başlat", "▶  Launch Game");
        StyleButton(_launchButton, false);
        _launchButton.Dock = DockStyle.Fill;
        _launchButton.AutoSize = false;
        _launchButton.Padding = Padding.Empty;
        _launchButton.Margin = new Padding(0, 0, 0, 4);
        _launchButton.BackColor = AppTheme.Primary;
        _launchButton.HoverBackColor = AppTheme.PrimaryHover;
        _launchButton.PressedBackColor = Color.FromArgb(73, 77, 190);
        _launchButton.Click += (_, _) => LaunchSelectedGame();
        actions.Controls.Add(_launchButton, 0, 1);

        _installButton.Text = L("Kur / Güncelle", "Install / Update");
        StyleButton(_installButton, true);
        _installButton.Dock = DockStyle.Fill;
        _installButton.AutoSize = false;
        _installButton.Padding = Padding.Empty;
        _installButton.Margin = new Padding(0, 0, 0, 4);
        _installButton.Click += (_, _) => InstallSelectedGame();
        actions.Controls.Add(_installButton, 0, 2);

        _uninstallButton.Text = L("Frame Gen Dosyalarını Sil", "Remove Frame Gen Files");
        StyleButton(_uninstallButton, true);
        _uninstallButton.Dock = DockStyle.Fill;
        _uninstallButton.AutoSize = false;
        _uninstallButton.Padding = Padding.Empty;
        _uninstallButton.Margin = Padding.Empty;
        _uninstallButton.BackColor = AppTheme.SurfaceAlt;
        _uninstallButton.ForeColor = AppTheme.StatusError;
        _uninstallButton.HoverBackColor = Color.FromArgb(253, 232, 232);
        _uninstallButton.PressedBackColor = Color.FromArgb(248, 218, 218);
        _uninstallButton.BorderColor = Color.Transparent;
        _uninstallButton.Click += (_, _) => UninstallSelectedGame();
        actions.Controls.Add(_uninstallButton, 0, 3);
        layout.Controls.Add(actions, 2, 0);

        return card;
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

    private GameEntry? SelectedGame => _selectedGameId is { } selectedId
        ? _state.Games.FirstOrDefault(game => game.Id == selectedId)
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
        UpdateSourceStatus();
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
            UpdateSourceStatus();
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
        await ScanForGamesAsync(_state.AdditionalScanFolders, includeKnownPlatforms: true);
    }

    private async Task ScanFolderForGamesAsync()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = L(
                "Launcher'a bağlı olmayan veya taşınabilir oyunların bulunduğu klasörü seçin.",
                "Select a folder containing launcher-free or portable games."),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
            return;

        var selectedFolder = Path.GetFullPath(dialog.SelectedPath);
        if (!_state.AdditionalScanFolders.Contains(selectedFolder, StringComparer.OrdinalIgnoreCase))
        {
            _state.AdditionalScanFolders.Add(selectedFolder);
            SaveState();
        }

        await ScanForGamesAsync([selectedFolder], includeKnownPlatforms: false);
    }

    private async Task ScanForGamesAsync(IReadOnlyList<string> additionalFolders, bool includeKnownPlatforms)
    {
        try
        {
            Enabled = false;
            UseWaitCursor = true;
            var discovered = await Task.Run(() =>
                _gameDiscoveryService.Scan(additionalFolders, includeKnownPlatforms));
            var discoveredByPath = discovered
                .GroupBy(game => Path.GetFullPath(game.ExecutablePath), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var metadataUpdated = 0;
            foreach (var existingGame in _state.Games)
            {
                var existingPath = Path.GetFullPath(existingGame.ExecutablePath);
                if (!discoveredByPath.TryGetValue(existingPath, out var match) ||
                    existingGame.SteamAppId is not null || match.SteamAppId is null)
                    continue;
                existingGame.SteamAppId = match.SteamAppId;
                metadataUpdated++;
            }
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
                if (metadataUpdated > 0)
                {
                    SaveState();
                    RefreshGameList(SelectedGame?.Id);
                }
                ShowInfo(discovered.Count == 0
                    ? L("Taranan platformlarda veya klasörlerde uygun 64-bit oyun EXE'si bulunamadı.",
                        "No suitable 64-bit game executable was found in the scanned platforms or folders.")
                    : metadataUpdated > 0
                        ? L($"{metadataUpdated} oyunun Steam bilgisi yenilendi. Tüm bulunan oyunlar zaten listede.",
                            $"Steam information was refreshed for {metadataUpdated} games. All discovered games are already listed.")
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
            else if (metadataUpdated > 0)
            {
                SaveState();
                RefreshGameList(SelectedGame?.Id);
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
        selectedId ??= _selectedGameId;
        var query = _gameSearchText.Text.Trim();
        var visibleGames = _state.Games
            .Where(game => string.IsNullOrWhiteSpace(query) ||
                           game.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                           Path.GetFileNameWithoutExtension(game.ExecutablePath)
                               .Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(game => game.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var cards = new List<GameLibraryCardData>();
        foreach (var game in visibleGames)
        {
            var inspection = _installationService.Inspect(game, _state.PackageSourceFolder);
            cards.Add(new GameLibraryCardData(
                game,
                inspection.Summary,
                inspection.State,
                _artworkService.GetCachedArtworkPath(game)));
        }

        if (selectedId is null || visibleGames.All(game => game.Id != selectedId))
            selectedId = visibleGames.FirstOrDefault()?.Id;
        _selectedGameId = selectedId;
        _libraryGrid.SetGames(cards, selectedId);
        _gameCountLabel.Text = L($"{visibleGames.Count} oyun · {_state.Games.Count} toplam",
            $"{visibleGames.Count} games · {_state.Games.Count} total");
        UpdateSelectionPanel();
        if (Visible && IsHandleCreated)
            QueueLibraryArtworkPrefetch(visibleGames);
    }

    private void UpdateSourceStatus()
    {
        var available = InstallationService.TryValidateSource(_state.PackageSourceFolder, out _);
        var trimmedPath = _state.PackageSourceFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var folderName = Path.GetFileName(trimmedPath);
        if (string.IsNullOrWhiteSpace(folderName))
            folderName = _state.PackageSourceFolder;
        _sourceStatusLabel.Text = available
            ? L($"●  Paket hazır{Environment.NewLine}{folderName}",
                $"●  Package ready{Environment.NewLine}{folderName}")
            : L("○  Frame Gen dosyaları seçilmedi", "○  Frame Gen files not selected");
        _sourceStatusLabel.ForeColor = available ? AppTheme.StatusReady : AppTheme.TextMuted;
        _buttonToolTip.SetToolTip(_sourceStatusLabel, available ? _state.PackageSourceFolder : string.Empty);
    }

    private void UpdateSelectionPanel()
    {
        var game = SelectedGame;
        _updatingControls = true;
        try
        {
            var enabled = game is not null;
            _selectedGameLabel.Text = game?.Name ?? L("Oyun seçilmedi", "No game selected");
            _selectedGameLabel.Font = game?.Name.Length switch
            {
                > 42 => _heroTitleSmallFont,
                > 27 => _heroTitleMediumFont,
                _ => _heroTitleLargeFont
            };
            _multiplierCombo.Enabled = enabled;
            _autoRepairCheck.Enabled = enabled;
            _installButton.Enabled = enabled;
            _uninstallButton.Enabled = enabled;
            _launchButton.Enabled = enabled;
            _onlineArtworkCheck.Enabled = enabled;
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
            _detailsLabel.Text = $"{inspection.Details}{Environment.NewLine}{CompactPath(game.ExecutablePath, 76)}";
            _buttonToolTip.SetToolTip(_detailsLabel, $"{inspection.Details}{Environment.NewLine}{game.ExecutablePath}");
            _statusLabel.ForeColor = StatusColor(inspection.State);
        }
        finally
        {
            _updatingControls = false;
        }

        _ = UpdateArtworkAsync(game, forceRefresh: false);
    }

    private static string CompactPath(string path, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length <= maximumLength)
            return path;

        var fileName = Path.GetFileName(path);
        var root = Path.GetPathRoot(path) ?? string.Empty;
        var compact = $"{root}…{Path.DirectorySeparatorChar}{fileName}";
        return compact.Length <= maximumLength
            ? compact
            : $"…{Path.DirectorySeparatorChar}{fileName}";
    }

    private void OnlineArtworkChanged()
    {
        if (_updatingControls)
            return;

        _state.OnlineArtworkEnabled = _onlineArtworkCheck.Checked;
        if (!_state.OnlineArtworkEnabled)
            _libraryArtworkCancellation?.Cancel();
        SaveState();
        _ = UpdateArtworkAsync(SelectedGame, forceRefresh: false);
        if (_state.OnlineArtworkEnabled)
            QueueLibraryArtworkPrefetch();
    }

    private void QueueLibraryArtworkPrefetch(IReadOnlyList<GameEntry>? games = null)
    {
        if (!_state.OnlineArtworkEnabled || IsDisposed || Disposing)
            return;

        games ??= _state.Games
            .Where(game => string.IsNullOrWhiteSpace(_gameSearchText.Text) ||
                           game.Name.Contains(_gameSearchText.Text.Trim(), StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(game => game.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var uncachedGames = games
            .Where(game => game.Id != _selectedGameId && _artworkService.NeedsOnlineArtwork(game))
            .Take(40)
            .ToList();
        if (uncachedGames.Count == 0)
            return;

        _libraryArtworkCancellation?.Cancel();
        _libraryArtworkCancellation?.Dispose();
        _libraryArtworkCancellation = new CancellationTokenSource();
        _ = PrefetchLibraryArtworkAsync(uncachedGames, _libraryArtworkCancellation.Token);
    }

    private async Task PrefetchLibraryArtworkAsync(IReadOnlyList<GameEntry> games, CancellationToken cancellationToken)
    {
        using var gate = new SemaphoreSlim(2);
        var stateChanged = false;
        var tasks = games.Select(async game =>
        {
            await gate.WaitAsync(cancellationToken);
            GameArtworkResult? result;
            try
            {
                result = await _artworkService.GetArtworkAsync(game, forceRefresh: false, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                result = null;
            }
            finally
            {
                gate.Release();
            }

            if (result is null || cancellationToken.IsCancellationRequested || IsDisposed || Disposing)
                return;

            // Each card appears as soon as its download finishes; a slow or
            // unavailable game cannot hold back the rest of the gallery.
            _libraryGrid.SetArtwork(game.Id, result.LocalPath);
            if (result.SteamAppId is { } resultAppId && game.SteamAppId != resultAppId)
            {
                game.SteamAppId = resultAppId;
                stateChanged = true;
            }
        }).ToArray();

        try
        {
            await Task.WhenAll(tasks);
            if (cancellationToken.IsCancellationRequested || IsDisposed || Disposing)
                return;

            if (stateChanged)
                SaveState();
        }
        catch (OperationCanceledException)
        {
            // Search/filter changes replace the current background artwork pass.
        }
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

        var cachedArtwork = _artworkService.GetCachedArtworkPath(game);
        _heroPanel.SetArtwork(cachedArtwork);
        _artworkStatusLabel.Text = cachedArtwork is null
            ? L("STEAM CDN'DE GÖRSEL ARANIYOR…", "LOOKING FOR ARTWORK ON STEAM CDN…")
            : Path.GetFileName(cachedArtwork).StartsWith("local_", StringComparison.OrdinalIgnoreCase)
                ? L("YEREL KAPAK • STEAM'DE KONTROL EDİLİYOR…", "LOCAL COVER • CHECKING STEAM…")
                : L("STEAM CDN GÖRSELİ • ÖNBELLEK", "STEAM CDN ARTWORK • CACHED");
        try
        {
            var result = await _artworkService.GetArtworkAsync(game, forceRefresh, cancellationToken);
            if (cancellationToken.IsCancellationRequested || SelectedGame?.Id != game.Id)
                return;

            if (result is null)
            {
                if (cachedArtwork is null)
                    _heroPanel.SetArtwork(null);
                _artworkStatusLabel.Text = cachedArtwork is null
                    ? L("STEAM CDN GÖRSELİ BULUNAMADI", "STEAM CDN ARTWORK NOT FOUND")
                    : L("ÖNBELLEKTEKİ GÖRSEL KULLANILIYOR", "USING CACHED ARTWORK");
                return;
            }

            _heroPanel.SetArtwork(result.LocalPath);
            _libraryGrid.SetArtwork(game.Id, result.LocalPath);
            var localCover = result.SourceUrl.Equals(GameArtworkService.LocalCoverSource, StringComparison.Ordinal);
            _artworkStatusLabel.Text = localCover
                ? L("YEREL OYUN KAPAĞI • OTOMATİK", "LOCAL GAME COVER • AUTOMATIC")
                : L("STEAM CDN GÖRSELİ • ÖNBELLEĞE ALINDI", "STEAM CDN ARTWORK • CACHED");
            if (result.SteamAppId is { } resultAppId && game.SteamAppId != resultAppId)
            {
                game.SteamAppId = resultAppId;
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
                if (cachedArtwork is null)
                    _heroPanel.SetArtwork(null);
                _artworkStatusLabel.Text = L("GÖRSEL İNDİRİLEMEDİ • DEGRADE KULLANILIYOR",
                    "ARTWORK UNAVAILABLE • USING GRADIENT");
            }
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

    private static Label NewSidebarCaption(string text) => new()
    {
        AutoSize = true,
        Text = text,
        ForeColor = AppTheme.TextMuted,
        BackColor = Color.Transparent,
        Font = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold),
        Margin = new Padding(8, 0, 0, 2)
    };

    private static ModernButton NewSidebarButton(string text, bool active = false) => new()
    {
        Text = text,
        Width = 176,
        Height = 40,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(13, 0, 8, 0),
        Margin = new Padding(0, 2, 0, 2),
        BackColor = active ? AppTheme.Primary : AppTheme.SurfaceAlt,
        ForeColor = active ? Color.White : AppTheme.TextSecondary,
        BorderColor = active ? Color.Transparent : AppTheme.Border,
        HoverBackColor = active ? AppTheme.PrimaryHover : AppTheme.SurfaceAlt,
        PressedBackColor = AppTheme.SurfaceRaised,
        CornerRadius = 9,
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
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.BorderColor = AppTheme.Border;
        button.BackColor = secondary ? AppTheme.SurfaceAlt : AppTheme.Primary;
        button.ForeColor = secondary ? AppTheme.TextPrimary : Color.White;
        button.Padding = new Padding(10, 5, 10, 5);
        button.Margin = new Padding(0, 2, 8, 6);
        button.Cursor = Cursors.Hand;
        button.Height = 36;
        button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        if (button is ModernButton modern)
        {
            modern.CornerRadius = 9;
            modern.BorderColor = secondary ? AppTheme.Border : Color.Transparent;
            modern.HoverBackColor = secondary ? AppTheme.SurfaceRaised : AppTheme.PrimaryHover;
            modern.PressedBackColor = secondary ? Color.FromArgb(214, 220, 233) : Color.FromArgb(67, 56, 202);
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
