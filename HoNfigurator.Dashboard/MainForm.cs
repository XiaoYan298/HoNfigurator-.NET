using System;
using System.Drawing;
using System.Windows.Forms;
using HoNfigurator.Dashboard.Services;

namespace HoNfigurator.Dashboard;

public partial class MainForm : Form
{
    private TabControl tabControl = null!;
    private DataGridView serverGrid = null!;
    private RichTextBox logTextBox = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;
    private ToolStripStatusLabel connectionLabel = null!;
    private System.Windows.Forms.Timer refreshTimer = null!;
    
    // API Service
    private readonly ApiService _apiService;
    private bool _isConnected = false;
    private string _apiUrl = "http://localhost:8080";
    
    // Config text boxes for saving
    private readonly Dictionary<string, TextBox> _configFields = new();
    
    // Stats labels for updating
    private Label? _serversOnlineValue;
    private Label? _totalPlayersValue;
    private Label? _gamesTodayValue;
    private Label? _uptimeValue;

    public MainForm()
    {
        _apiService = new ApiService(_apiUrl);
        _apiService.OnLog += (s, msg) => AppendLog("[API]", msg, Color.Cyan);
        _apiService.OnError += (s, msg) => AppendLog("[ERROR]", msg, Color.Red);
        
        InitializeComponent();
        SetupUI();
        SetupTimer();
        
        // Initial connection check
        _ = CheckConnectionAsync();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        
        // Form settings
        this.Text = "HoNfigurator .NET Dashboard";
        this.Size = new Size(1200, 800);
        this.MinimumSize = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.White;
        this.Font = new Font("Segoe UI", 9F);
        
        this.ResumeLayout(false);
    }

    private void SetupUI()
    {
        // Main layout panel
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = Color.FromArgb(30, 30, 30),
            Padding = new Padding(10)
        };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // Header
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Content
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); // Status

        // Header Panel
        var headerPanel = CreateHeaderPanel();
        mainPanel.Controls.Add(headerPanel, 0, 0);

        // Tab Control
        tabControl = CreateTabControl();
        mainPanel.Controls.Add(tabControl, 0, 1);

        // Status Strip
        statusStrip = CreateStatusStrip();
        mainPanel.Controls.Add(statusStrip, 0, 2);

        this.Controls.Add(mainPanel);
    }

    private Panel CreateHeaderPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(45, 45, 48),
            Padding = new Padding(10)
        };

        // Title
        var titleLabel = new Label
        {
            Text = "🎮 HoNfigurator .NET",
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 150, 255),
            AutoSize = true,
            Location = new Point(10, 10)
        };

        // Subtitle
        var subtitleLabel = new Label
        {
            Text = "Heroes of Newerth Server Manager",
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(12, 50)
        };

        // Control buttons panel
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 15, 10, 0)
        };

        var startAllBtn = CreateStyledButton("▶ Start All", Color.FromArgb(0, 120, 0));
        startAllBtn.Click += (s, e) => StartAllServers();
        
        var stopAllBtn = CreateStyledButton("⬛ Stop All", Color.FromArgb(180, 0, 0));
        stopAllBtn.Click += (s, e) => StopAllServers();
        
        var refreshBtn = CreateStyledButton("🔄 Refresh", Color.FromArgb(0, 100, 180));
        refreshBtn.Click += (s, e) => RefreshServerList();

        var settingsBtn = CreateStyledButton("⚙ Settings", Color.FromArgb(80, 80, 80));
        settingsBtn.Click += (s, e) => OpenSettings();

        buttonPanel.Controls.AddRange(new Control[] { startAllBtn, stopAllBtn, refreshBtn, settingsBtn });

        panel.Controls.Add(titleLabel);
        panel.Controls.Add(subtitleLabel);
        panel.Controls.Add(buttonPanel);

        return panel;
    }

    private Button CreateStyledButton(string text, Color backColor)
    {
        return new Button
        {
            Text = text,
            Size = new Size(100, 35),
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Margin = new Padding(5),
            Cursor = Cursors.Hand
        };
    }

    private TabControl CreateTabControl()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F),
            Padding = new Point(15, 5)
        };

        // Servers Tab
        var serversTab = new TabPage("🖥 Servers")
        {
            BackColor = Color.FromArgb(37, 37, 38)
        };
        serversTab.Controls.Add(CreateServersPanel());
        tabs.TabPages.Add(serversTab);

        // Logs Tab
        var logsTab = new TabPage("📋 Logs")
        {
            BackColor = Color.FromArgb(37, 37, 38)
        };
        logsTab.Controls.Add(CreateLogsPanel());
        tabs.TabPages.Add(logsTab);

        // Configuration Tab
        var configTab = new TabPage("⚙ Configuration")
        {
            BackColor = Color.FromArgb(37, 37, 38)
        };
        configTab.Controls.Add(CreateConfigPanel());
        tabs.TabPages.Add(configTab);

        // Statistics Tab
        var statsTab = new TabPage("📊 Statistics")
        {
            BackColor = Color.FromArgb(37, 37, 38)
        };
        statsTab.Controls.Add(CreateStatsPanel());
        tabs.TabPages.Add(statsTab);

        return tabs;
    }

    private Panel CreateServersPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        serverGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            GridColor = Color.FromArgb(60, 60, 60),
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(37, 37, 38),
                ForeColor = Color.White,
                SelectionBackColor = Color.FromArgb(0, 100, 180),
                SelectionForeColor = Color.White,
                Padding = new Padding(5)
            },
            EnableHeadersVisualStyles = false
        };

        // Add columns
        serverGrid.Columns.Add("Id", "ID");
        serverGrid.Columns.Add("Name", "Server Name");
        serverGrid.Columns.Add("Status", "Status");
        serverGrid.Columns.Add("GamePort", "Game Port");
        serverGrid.Columns.Add("VoicePort", "Voice Port");
        serverGrid.Columns.Add("Players", "Players");
        serverGrid.Columns.Add("Map", "Map");
        serverGrid.Columns.Add("Uptime", "Uptime");

        // Add context menu
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Start Server", null, async (s, e) => await StartSelectedServerAsync());
        contextMenu.Items.Add("Stop Server", null, async (s, e) => await StopSelectedServerAsync());
        contextMenu.Items.Add("Restart Server", null, async (s, e) => await RestartSelectedServerAsync());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("View Logs", null, (s, e) => ViewServerLogs());
        serverGrid.ContextMenuStrip = contextMenu;

        panel.Controls.Add(serverGrid);
        return panel;
    }

    private void AddSampleServerData()
    {
        // Will be populated from API
    }

    private Panel CreateLogsPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        logTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(20, 20, 20),
            ForeColor = Color.LightGreen,
            Font = new Font("Consolas", 10F),
            BorderStyle = BorderStyle.None,
            ReadOnly = true
        };

        // Initial log
        AppendLog("[INFO]", "Dashboard started - connecting to API...", Color.Cyan);

        panel.Controls.Add(logTextBox);
        return panel;
    }

    private void AppendLog(string level, string message, Color color)
    {
        if (logTextBox.InvokeRequired)
        {
            logTextBox.Invoke(() => AppendLog(level, message, color));
            return;
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        logTextBox.SelectionStart = logTextBox.TextLength;
        logTextBox.SelectionColor = Color.Gray;
        logTextBox.AppendText($"[{timestamp}] ");
        logTextBox.SelectionColor = color;
        logTextBox.AppendText($"{level}: ");
        logTextBox.SelectionColor = Color.White;
        logTextBox.AppendText($"{message}\n");
        logTextBox.ScrollToCaret();
    }

    private Panel CreateConfigPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 15,
            Padding = new Padding(10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;

        // Section: API Connection
        AddSectionHeader(layout, "API Connection", row++);
        AddConfigField(layout, "API URL:", _apiUrl, row++, "apiUrl");
        
        // Section: Installation
        AddSectionHeader(layout, "Installation Paths", row++);
        AddConfigField(layout, "HoN Install Directory:", "", row++, "honInstallDirectory");
        AddConfigField(layout, "HoN Home Directory:", "", row++, "honHomeDirectory");
        
        // Section: Server
        AddSectionHeader(layout, "Server Settings", row++);
        AddConfigField(layout, "Server Name:", "", row++, "serverName");
        AddConfigField(layout, "Server Login:", "", row++, "login");
        AddConfigField(layout, "Total Servers:", "", row++, "totalServers");
        AddConfigField(layout, "Starting Game Port:", "", row++, "startingGamePort");
        
        // Section: Connection
        AddSectionHeader(layout, "Connection", row++);
        AddConfigField(layout, "Master Server:", "", row++, "masterServer");

        // Buttons panel
        var buttonsPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0)
        };
        
        var loadBtn = CreateStyledButton("📥 Load from API", Color.FromArgb(0, 100, 180));
        loadBtn.Size = new Size(150, 40);
        loadBtn.Click += async (s, e) => await LoadConfigurationAsync();
        
        var saveBtn = CreateStyledButton("💾 Save", Color.FromArgb(0, 120, 80));
        saveBtn.Size = new Size(150, 40);
        saveBtn.Click += async (s, e) => await SaveConfigurationAsync();
        
        var connectBtn = CreateStyledButton("🔌 Connect", Color.FromArgb(100, 50, 150));
        connectBtn.Size = new Size(150, 40);
        connectBtn.Click += async (s, e) => await ReconnectApiAsync();
        
        buttonsPanel.Controls.AddRange(new Control[] { loadBtn, saveBtn, connectBtn });
        layout.Controls.Add(buttonsPanel, 1, row);

        panel.Controls.Add(layout);
        return panel;
    }

    private void AddSectionHeader(TableLayoutPanel layout, string text, int row)
    {
        var label = new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 150, 255),
            AutoSize = true,
            Padding = new Padding(0, 15, 0, 5)
        };
        layout.Controls.Add(label, 0, row);
        layout.SetColumnSpan(label, 2);
    }

    private void AddConfigField(TableLayoutPanel layout, string labelText, string defaultValue, int row, string fieldName = "")
    {
        var label = new Label
        {
            Text = labelText,
            ForeColor = Color.White,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        };
        
        var textBox = new TextBox
        {
            Text = defaultValue,
            Width = 400,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Name = fieldName
        };
        
        if (!string.IsNullOrEmpty(fieldName))
        {
            _configFields[fieldName] = textBox;
        }

        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(textBox, 1, row);
    }

    private Panel CreateStatsPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var statsLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        // Stats cards with stored references
        var serversCard = CreateStatCard("🖥 Servers Online", "- / -", Color.FromArgb(0, 150, 0), out _serversOnlineValue);
        var playersCard = CreateStatCard("👥 Total Players", "-", Color.FromArgb(0, 100, 200), out _totalPlayersValue);
        var gamesCard = CreateStatCard("🎮 Games Today", "-", Color.FromArgb(150, 100, 0), out _gamesTodayValue);
        var uptimeCard = CreateStatCard("⏱ Uptime", "-", Color.FromArgb(100, 0, 150), out _uptimeValue);
        
        statsLayout.Controls.AddRange(new Control[] { serversCard, playersCard, gamesCard, uptimeCard });

        panel.Controls.Add(statsLayout);
        return panel;
    }

    private Panel CreateStatCard(string title, string value, Color accentColor, out Label valueLabel)
    {
        var card = new Panel
        {
            Size = new Size(200, 100),
            BackColor = Color.FromArgb(45, 45, 48),
            Margin = new Padding(10),
            Padding = new Padding(15)
        };

        var titleLabel = new Label
        {
            Text = title,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 10F),
            AutoSize = true,
            Location = new Point(15, 15)
        };

        valueLabel = new Label
        {
            Text = value,
            ForeColor = accentColor,
            Font = new Font("Segoe UI", 24F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(15, 45)
        };

        card.Controls.Add(titleLabel);
        card.Controls.Add(valueLabel);
        return card;
    }

    private StatusStrip CreateStatusStrip()
    {
        var strip = new StatusStrip
        {
            BackColor = Color.FromArgb(0, 122, 204),
            SizingGrip = false
        };

        statusLabel = new ToolStripStatusLabel
        {
            Text = "Ready",
            ForeColor = Color.White
        };

        connectionLabel = new ToolStripStatusLabel
        {
            Text = "🔴 Disconnected from Chat Server",
            ForeColor = Color.White,
            Alignment = ToolStripItemAlignment.Right
        };

        strip.Items.Add(statusLabel);
        strip.Items.Add(new ToolStripStatusLabel { Spring = true });
        strip.Items.Add(connectionLabel);

        return strip;
    }

    private void SetupTimer()
    {
        refreshTimer = new System.Windows.Forms.Timer
        {
            Interval = 5000 // 5 seconds
        };
        refreshTimer.Tick += async (s, e) => await RefreshStatusAsync();
        refreshTimer.Start();
    }

    #region API Connection Methods

    private async Task CheckConnectionAsync()
    {
        _isConnected = await _apiService.CheckConnectionAsync();
        UpdateConnectionStatus();
        
        if (_isConnected)
        {
            AppendLog("[INFO]", $"Connected to API at {_apiUrl}", Color.LightGreen);
            await RefreshAllDataAsync();
        }
        else
        {
            AppendLog("[WARN]", $"Cannot connect to API at {_apiUrl}", Color.Yellow);
        }
    }

    private async Task ReconnectApiAsync()
    {
        if (_configFields.TryGetValue("apiUrl", out var urlField))
        {
            _apiUrl = urlField.Text;
            _apiService.SetBaseUrl(_apiUrl);
        }
        
        statusLabel.Text = "Connecting...";
        await CheckConnectionAsync();
    }

    private void UpdateConnectionStatus()
    {
        if (InvokeRequired)
        {
            Invoke(UpdateConnectionStatus);
            return;
        }
        
        if (_isConnected)
        {
            connectionLabel.Text = "🟢 Connected to API";
            connectionLabel.ForeColor = Color.LightGreen;
        }
        else
        {
            connectionLabel.Text = "🔴 Disconnected from API";
            connectionLabel.ForeColor = Color.White;
        }
    }

    #endregion

    #region Data Refresh Methods

    private async Task RefreshAllDataAsync()
    {
        await RefreshServersAsync();
        await RefreshMetricsAsync();
        await LoadConfigurationAsync();
    }

    private async Task RefreshServersAsync()
    {
        if (!_isConnected) return;
        
        var servers = await _apiService.GetServersAsync();
        if (servers == null) return;
        
        if (InvokeRequired)
        {
            Invoke(() => UpdateServerGrid(servers));
        }
        else
        {
            UpdateServerGrid(servers);
        }
    }

    private void UpdateServerGrid(List<ServerInfo> servers)
    {
        serverGrid.Rows.Clear();
        
        foreach (var server in servers)
        {
            var statusIcon = server.Status.ToLower() switch
            {
                "running" => "🟢 Running",
                "starting" => "🟡 Starting",
                "stopping" => "🟡 Stopping",
                "stopped" => "🔴 Stopped",
                "idle" => "🔵 Idle",
                _ => $"⚪ {server.Status}"
            };
            
            var players = $"{server.Players}/{server.MaxPlayers}";
            var map = string.IsNullOrEmpty(server.Map) ? "-" : server.Map;
            var uptime = string.IsNullOrEmpty(server.Uptime) ? "-" : server.Uptime;
            
            serverGrid.Rows.Add(
                server.Id.ToString(),
                server.Name,
                statusIcon,
                server.GamePort.ToString(),
                server.VoicePort.ToString(),
                players,
                map,
                uptime
            );
        }
        
        statusLabel.Text = $"Loaded {servers.Count} servers";
    }

    private async Task RefreshMetricsAsync()
    {
        if (!_isConnected) return;
        
        var status = await _apiService.GetStatusAsync();
        var metrics = await _apiService.GetMetricsAsync();
        
        if (InvokeRequired)
        {
            Invoke(() => UpdateStats(status, metrics));
        }
        else
        {
            UpdateStats(status, metrics);
        }
    }

    private void UpdateStats(ApiStatus? status, ApiMetrics? metrics)
    {
        if (status != null)
        {
            _serversOnlineValue?.BeginInvoke(() => 
                _serversOnlineValue.Text = $"{status.RunningServers} / {status.ServerCount}");
            _totalPlayersValue?.BeginInvoke(() => 
                _totalPlayersValue.Text = status.TotalPlayers.ToString());
            _uptimeValue?.BeginInvoke(() => 
                _uptimeValue.Text = status.Uptime);
        }
        
        if (metrics != null)
        {
            _gamesTodayValue?.BeginInvoke(() => 
                _gamesTodayValue.Text = metrics.GamesToday.ToString());
        }
    }

    private async Task RefreshStatusAsync()
    {
        if (!_isConnected)
        {
            // Try to reconnect
            _isConnected = await _apiService.CheckConnectionAsync();
            UpdateConnectionStatus();
        }
        
        if (_isConnected)
        {
            await RefreshServersAsync();
            await RefreshMetricsAsync();
        }
    }

    #endregion

    #region Server Control Methods

    private async void StartAllServers()
    {
        if (!_isConnected)
        {
            AppendLog("[WARN]", "Not connected to API", Color.Yellow);
            return;
        }
        
        statusLabel.Text = "Starting all servers...";
        AppendLog("[INFO]", "Starting all servers...", Color.Cyan);
        
        if (await _apiService.StartAllServersAsync())
        {
            await Task.Delay(1000);
            await RefreshServersAsync();
        }
    }

    private async void StopAllServers()
    {
        if (!_isConnected)
        {
            AppendLog("[WARN]", "Not connected to API", Color.Yellow);
            return;
        }
        
        statusLabel.Text = "Stopping all servers...";
        AppendLog("[INFO]", "Stopping all servers...", Color.Yellow);
        
        if (await _apiService.StopAllServersAsync())
        {
            await Task.Delay(1000);
            await RefreshServersAsync();
        }
    }

    private async void RefreshServerList()
    {
        statusLabel.Text = "Refreshing...";
        await RefreshServersAsync();
        await RefreshMetricsAsync();
        statusLabel.Text = "Refreshed";
    }

    private async Task StartSelectedServerAsync()
    {
        if (serverGrid.SelectedRows.Count > 0)
        {
            var serverId = int.Parse(serverGrid.SelectedRows[0].Cells["Id"].Value?.ToString() ?? "0");
            if (serverId > 0)
            {
                AppendLog("[INFO]", $"Starting server {serverId}...", Color.Cyan);
                await _apiService.StartServerAsync(serverId);
                await Task.Delay(500);
                await RefreshServersAsync();
            }
        }
    }

    private async Task StopSelectedServerAsync()
    {
        if (serverGrid.SelectedRows.Count > 0)
        {
            var serverId = int.Parse(serverGrid.SelectedRows[0].Cells["Id"].Value?.ToString() ?? "0");
            if (serverId > 0)
            {
                AppendLog("[INFO]", $"Stopping server {serverId}...", Color.Yellow);
                await _apiService.StopServerAsync(serverId);
                await Task.Delay(500);
                await RefreshServersAsync();
            }
        }
    }

    private async Task RestartSelectedServerAsync()
    {
        if (serverGrid.SelectedRows.Count > 0)
        {
            var serverId = int.Parse(serverGrid.SelectedRows[0].Cells["Id"].Value?.ToString() ?? "0");
            if (serverId > 0)
            {
                AppendLog("[INFO]", $"Restarting server {serverId}...", Color.Cyan);
                await _apiService.RestartServerAsync(serverId);
                await Task.Delay(500);
                await RefreshServersAsync();
            }
        }
    }

    #endregion

    #region Configuration Methods

    private async Task LoadConfigurationAsync()
    {
        if (!_isConnected) return;
        
        var config = await _apiService.GetConfigurationAsync();
        if (config == null) return;
        
        if (InvokeRequired)
        {
            Invoke(() => PopulateConfigFields(config));
        }
        else
        {
            PopulateConfigFields(config);
        }
        
        AppendLog("[INFO]", "Configuration loaded from API", Color.LightGreen);
    }

    private void PopulateConfigFields(ApiConfiguration config)
    {
        SetConfigField("honInstallDirectory", config.HonInstallDirectory);
        SetConfigField("honHomeDirectory", config.HonHomeDirectory);
        SetConfigField("serverName", config.ServerName);
        SetConfigField("login", config.Login);
        SetConfigField("totalServers", config.TotalServers.ToString());
        SetConfigField("startingGamePort", config.StartingGamePort.ToString());
        SetConfigField("masterServer", config.MasterServer);
    }

    private void SetConfigField(string fieldName, string value)
    {
        if (_configFields.TryGetValue(fieldName, out var textBox))
        {
            textBox.Text = value;
        }
    }

    private async Task SaveConfigurationAsync()
    {
        if (!_isConnected)
        {
            MessageBox.Show("Not connected to API", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        
        var config = new ApiConfiguration
        {
            HonInstallDirectory = GetConfigField("honInstallDirectory"),
            HonHomeDirectory = GetConfigField("honHomeDirectory"),
            ServerName = GetConfigField("serverName"),
            Login = GetConfigField("login"),
            TotalServers = int.TryParse(GetConfigField("totalServers"), out var ts) ? ts : 0,
            StartingGamePort = int.TryParse(GetConfigField("startingGamePort"), out var gp) ? gp : 11001,
            MasterServer = GetConfigField("masterServer")
        };
        
        if (await _apiService.SaveConfigurationAsync(config))
        {
            MessageBox.Show("Configuration saved successfully!", "Success", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show("Failed to save configuration", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string GetConfigField(string fieldName)
    {
        return _configFields.TryGetValue(fieldName, out var textBox) ? textBox.Text : "";
    }

    #endregion

    private void OpenSettings()
    {
        tabControl.SelectedIndex = 2; // Switch to config tab
    }

    private void ViewServerLogs()
    {
        tabControl.SelectedIndex = 1; // Switch to logs tab
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to exit?",
            "Confirm Exit",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.No)
        {
            e.Cancel = true;
        }
        else
        {
            _apiService.Dispose();
            refreshTimer.Stop();
        }

        base.OnFormClosing(e);
    }
}
