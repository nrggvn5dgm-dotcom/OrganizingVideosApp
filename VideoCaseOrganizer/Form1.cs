using System.Text.Json;

namespace VideoCaseOrganizer;

public partial class Form1 : Form
{
    private static readonly string[] Genres =
    [
        "映画",
        "スポーツ",
        "音楽",
        "学習",
        "旅行",
        "その他"
    ];

    private readonly string _stateFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VideoCaseOrganizer",
        "state.json");
    private readonly string _storageDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VideoCaseOrganizer",
        "StoredVideos");

    private readonly AppState _state;
    private readonly Label _welcomeLabel = new();
    private readonly ComboBox _genreSelect = new();
    private readonly Panel _contentPanel = new();
    private readonly Button _signInButton = new();
    private readonly Button _signOutButton = new();
    private readonly Font _titleFont = new("Yu Gothic UI", 24, FontStyle.Bold);
    private readonly Font _headingFont = new("Yu Gothic UI", 14, FontStyle.Bold);
    private readonly Font _bodyFont = new("Yu Gothic UI", 10);

    private string? _lastTransferId;

    public Form1()
    {
        InitializeComponent();
        _state = LoadState();
        BuildShell();
        ShowMainMenu();
    }

    private AppState LoadState()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                return JsonSerializer.Deserialize<AppState>(File.ReadAllText(_stateFilePath)) ?? new AppState();
            }
        }
        catch
        {
            // Broken state should not prevent the app from opening.
        }

        return new AppState();
    }

    private void SaveState()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_stateFilePath)!);
        File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void BuildShell()
    {
        Text = "動画整理ケース";
        MinimumSize = new Size(980, 640);
        Size = new Size(1120, 720);
        BackColor = Color.FromArgb(245, 247, 251);
        Font = _bodyFont;
        AllowDrop = true;
        DragEnter += (_, e) => e.Effect = ContainsVideoFiles(e.Data) ? DragDropEffects.Copy : DragDropEffects.None;
        DragDrop += (_, e) => HandleDroppedFiles(e.Data);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            RowCount = 2,
            ColumnCount = 1,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var topbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
        };
        topbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        topbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

        var titleStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
        };
        titleStack.Controls.Add(new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(22, 160, 133),
            Font = new Font("Yu Gothic UI", 9, FontStyle.Bold),
            Text = "Video Case Organizer",
        });
        titleStack.Controls.Add(new Label
        {
            AutoSize = true,
            Font = _titleFont,
            Text = "動画整理ケース",
        });

        var accountPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
        };
        _signOutButton.Text = "サインアウト";
        _signOutButton.Width = 120;
        _signOutButton.Height = 40;
        _signOutButton.Click += (_, _) => SignOut();
        _signInButton.Text = "サインイン";
        _signInButton.Width = 110;
        _signInButton.Height = 40;
        _signInButton.BackColor = Color.FromArgb(24, 32, 47);
        _signInButton.ForeColor = Color.White;
        _signInButton.FlatStyle = FlatStyle.Flat;
        _signInButton.Click += (_, _) => ShowSignIn();
        _welcomeLabel.AutoSize = true;
        _welcomeLabel.Width = 300;
        _welcomeLabel.TextAlign = ContentAlignment.MiddleRight;
        accountPanel.Controls.Add(_signOutButton);
        accountPanel.Controls.Add(_signInButton);
        accountPanel.Controls.Add(_welcomeLabel);

        topbar.Controls.Add(titleStack, 0, 0);
        topbar.Controls.Add(accountPanel, 1, 0);

        _contentPanel.Dock = DockStyle.Fill;
        root.Controls.Add(topbar, 0, 0);
        root.Controls.Add(_contentPanel, 0, 1);
        Controls.Add(root);
    }

    private void ShowMainMenu()
    {
        UpdateWelcome();
        SwapContent();

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));

        var menu = CreateCard();
        menu.Controls.Add(CreateHeading("メインメニュー"));
        menu.Controls.Add(CreateLargeButton("あなたのMyケース", "ジャンルごとに整理した動画を確認", (_, _) => ShowCases()));
        menu.Controls.Add(CreateLargeButton("最近転送した動画", "転送履歴を時系列で確認", (_, _) => ShowHistory()));

        var transfer = CreateCard();
        var headingRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, FlowDirection = FlowDirection.LeftToRight };
        headingRow.Controls.Add(CreateHeading("ファイル転送装置"));
        _genreSelect.DropDownStyle = ComboBoxStyle.DropDownList;
        _genreSelect.Width = 150;
        _genreSelect.Items.Clear();
        _genreSelect.Items.AddRange(Genres);
        _genreSelect.SelectedIndex = 0;
        headingRow.Controls.Add(_genreSelect);

        var dropZone = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 251, 255),
            BorderStyle = BorderStyle.FixedSingle,
            AllowDrop = true,
            Padding = new Padding(24),
        };
        dropZone.DragEnter += (_, e) => e.Effect = ContainsVideoFiles(e.Data) ? DragDropEffects.Copy : DragDropEffects.None;
        dropZone.DragDrop += (_, e) => HandleDroppedFiles(e.Data);
        dropZone.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Yu Gothic UI", 15, FontStyle.Bold),
            Text = "動画ファイルをここにドラッグ&ドロップ\r\nMP4 / MOV / AVI / MKV / WEBM など",
        });

        var chooseButton = CreatePrimaryButton("動画ファイルを選択");
        chooseButton.Dock = DockStyle.Bottom;
        chooseButton.Click += (_, _) => ChooseFiles();

        transfer.Controls.Add(dropZone);
        transfer.Controls.Add(chooseButton);
        transfer.Controls.Add(headingRow);

        grid.Controls.Add(menu, 0, 0);
        grid.Controls.Add(transfer, 1, 0);
        _contentPanel.Controls.Add(grid);
    }

    private void ShowCases()
    {
        UpdateWelcome();
        SwapContent();

        var root = CreateViewWithHeader("あなたのMyケース", ShowMainMenu);
        var cases = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            AutoScroll = true,
        };
        cases.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        cases.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        cases.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

        for (var i = 0; i < Genres.Length; i++)
        {
            cases.Controls.Add(CreateCaseCard(Genres[i]), i % 3, i / 3);
        }

        root.Controls.Add(cases);
        _contentPanel.Controls.Add(root);
    }

    private void ShowHistory()
    {
        UpdateWelcome();
        SwapContent();

        var root = CreateViewWithHeader("最近転送した動画", ShowMainMenu);
        var list = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
        };

        if (_state.History.Count == 0)
        {
            list.Controls.Add(CreateMutedLabel("転送履歴はまだありません。"));
        }
        else
        {
            foreach (var item in _state.History.OrderByDescending(x => x.TransferredAt))
            {
                list.Controls.Add(CreateVideoRow(item, $"{item.Action}: "));
            }
        }

        root.Controls.Add(list);
        _contentPanel.Controls.Add(root);
    }

    private void ShowSignIn()
    {
        SwapContent();

        var box = CreateCard();
        box.Width = 460;
        box.Dock = DockStyle.Top;
        box.Controls.Add(CreateHeading("サインイン"));

        var input = new TextBox { Height = 36, Dock = DockStyle.Top, PlaceholderText = "例: 山田" };
        input.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                SignIn(input.Text);
            }
        };

        var submit = CreatePrimaryButton("サインインする");
        submit.Dock = DockStyle.Top;
        submit.Click += (_, _) => SignIn(input.Text);

        var back = CreateBackButton(ShowMainMenu);
        back.Dock = DockStyle.Top;

        box.Controls.Add(submit);
        box.Controls.Add(input);
        box.Controls.Add(new Label { Text = "ユーザー名", Dock = DockStyle.Top, Height = 28 });
        box.Controls.Add(back);
        _contentPanel.Controls.Add(box);
    }

    private void SignIn(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("ユーザー名を入力してください。", "サインイン", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _state.CurrentUser = name.Trim();
        SaveState();
        ShowMainMenu();
    }

    private void SignOut()
    {
        if (MessageBox.Show("サインアウトしますか？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        _state.CurrentUser = "";
        SaveState();
        ShowMainMenu();
    }

    private void ChooseFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "動画ファイル|*.mp4;*.mov;*.avi;*.mkv;*.webm;*.wmv;*.m4v|すべてのファイル|*.*",
            Multiselect = true,
            Title = "動画ファイルを選択",
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            HandleFiles(dialog.FileNames);
        }
    }

    private void HandleDroppedFiles(IDataObject? data)
    {
        if (data?.GetData(DataFormats.FileDrop) is string[] paths)
        {
            HandleFiles(paths);
        }
    }

    private bool ContainsVideoFiles(IDataObject? data)
    {
        return data?.GetData(DataFormats.FileDrop) is string[] paths && paths.Any(IsKnownVideo);
    }

    private void HandleFiles(IEnumerable<string> paths)
    {
        var files = paths.Where(File.Exists).Where(IsKnownVideo).ToList();
        if (files.Count == 0)
        {
            MessageBox.Show("対応している動画ファイルを選択してください。", "動画整理ケース", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        foreach (var path in files)
        {
            AddTransfer(path);
        }

        var lastVideo = _state.Videos.FirstOrDefault(video => video.Id == _lastTransferId);
        if (lastVideo != null)
        {
            var result = MessageBox.Show(
                $"転送しました。\r\n\r\n動画名: {lastVideo.Name}\r\nサイズ: {FormatSize(lastVideo.Size)}\r\nケース: {lastVideo.Genre}\r\n\r\n最後の転送をキャンセルしますか？",
                "転送完了",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                CancelLastTransfer();
            }
        }
    }

    private void AddTransfer(string path)
    {
        var info = new FileInfo(path);
        var now = DateTimeOffset.Now;
        var storedPath = StoreVideoFile(info);
        var video = new VideoItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = info.Name,
            Path = info.FullName,
            StoredPath = storedPath,
            Size = info.Length,
            Genre = _genreSelect.SelectedItem?.ToString() ?? Genres[0],
            TransferredAt = now,
            Action = "転送",
        };

        _state.Videos.Insert(0, video);
        _state.History.Insert(0, video with { Action = "転送" });
        _lastTransferId = video.Id;
        SaveState();
    }

    private string StoreVideoFile(FileInfo source)
    {
        Directory.CreateDirectory(_storageDirectory);

        var extension = source.Extension;
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var destination = Path.Combine(_storageDirectory, fileName);
        File.Copy(source.FullName, destination, overwrite: false);

        return destination;
    }

    private void CancelLastTransfer()
    {
        if (_lastTransferId == null)
        {
            return;
        }

        var removed = _state.Videos.FirstOrDefault(video => video.Id == _lastTransferId);
        if (removed != null)
        {
            _state.Videos.Remove(removed);
            _state.History.Insert(0, removed with { Action = "転送キャンセル", TransferredAt = DateTimeOffset.Now });
        }

        _lastTransferId = null;
        SaveState();
        ShowMainMenu();
    }

    private void UpdateWelcome()
    {
        var userName = string.IsNullOrWhiteSpace(_state.CurrentUser) ? "ゲスト" : _state.CurrentUser;
        _welcomeLabel.Text = $"ようこそ、{userName}さん";
    }

    private void SwapContent()
    {
        _contentPanel.Controls.Clear();
    }

    private Panel CreateCard()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(20),
            Margin = new Padding(8),
        };
    }

    private Label CreateHeading(string text)
    {
        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 44,
            Font = _headingFont,
            Text = text,
        };
    }

    private Control CreateLargeButton(string title, string description, EventHandler click)
    {
        var button = new Button
        {
            Dock = DockStyle.Top,
            Height = 112,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = $"{title}\r\n{description}",
            BackColor = Color.FromArgb(251, 252, 255),
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 0, 16),
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(217, 224, 234);
        button.Click += click;
        return button;
    }

    private Button CreatePrimaryButton(string text)
    {
        var button = new Button
        {
            Height = 42,
            Text = text,
            BackColor = Color.FromArgb(31, 111, 235),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private Button CreateBackButton(Action action)
    {
        var button = new Button
        {
            Width = 170,
            Height = 40,
            Text = "← メインメニュー",
        };
        button.Click += (_, _) => action();
        return button;
    }

    private TableLayoutPanel CreateViewWithHeader(string title, Action backAction)
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        header.Controls.Add(CreateBackButton(backAction));
        header.Controls.Add(new Label
        {
            AutoSize = true,
            Font = _headingFont,
            Text = title,
            Padding = new Padding(12, 8, 0, 0),
        });
        root.Controls.Add(header, 0, 0);
        return root;
    }

    private Control CreateCaseCard(string genre)
    {
        var panel = CreateCard();
        panel.Dock = DockStyle.Fill;
        panel.Controls.Add(CreateHeading($"{genre}ケース"));

        var list = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
        };

        var videos = _state.Videos.Where(video => video.Genre == genre).OrderByDescending(video => video.TransferredAt).ToList();
        if (videos.Count == 0)
        {
            list.Controls.Add(CreateMutedLabel("動画はまだありません。"));
        }
        else
        {
            foreach (var video in videos)
            {
                list.Controls.Add(CreateVideoRow(video, showDownloadButton: true));
            }
        }

        panel.Controls.Add(list);
        return panel;
    }

    private Control CreateVideoRow(VideoItem video, string prefix = "", bool showDownloadButton = false)
    {
        var row = new TableLayoutPanel
        {
            AutoSize = true,
            Width = 680,
            Height = 72,
            ColumnCount = showDownloadButton ? 2 : 1,
            Padding = new Padding(6),
            Margin = new Padding(0, 0, 0, 8),
            BorderStyle = BorderStyle.FixedSingle,
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        if (showDownloadButton)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        }

        row.Controls.Add(new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = $"{prefix}{video.Name}\r\n{FormatSize(video.Size)} / {video.Genre}ケース / {video.TransferredAt.LocalDateTime:g}",
        }, 0, 0);

        if (showDownloadButton)
        {
            var downloadButton = new Button
            {
                Dock = DockStyle.Fill,
                Text = "ダウンロード",
            };
            downloadButton.Click += (_, _) => DownloadVideo(video);
            row.Controls.Add(downloadButton, 1, 0);
        }

        return row;
    }

    private void DownloadVideo(VideoItem video)
    {
        var sourcePath = File.Exists(video.StoredPath) ? video.StoredPath : video.Path;
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            MessageBox.Show("保管された動画ファイルが見つかりません。", "ダウンロード", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            FileName = video.Name,
            Filter = "動画ファイル|*" + Path.GetExtension(video.Name) + "|すべてのファイル|*.*",
            Title = "動画をダウンロード",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        File.Copy(sourcePath, dialog.FileName, overwrite: true);
        MessageBox.Show("動画を保存しました。", "ダウンロード", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private Label CreateMutedLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 112, 132),
            Padding = new Padding(8),
            Text = text,
        };
    }

    private static bool IsKnownVideo(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mov", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".avi", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mkv", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webm", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".wmv", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".m4v", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatSize(long size)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)size;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.#} {units[unit]}";
    }
}

public sealed class AppState
{
    public string CurrentUser { get; set; } = "";
    public List<VideoItem> Videos { get; set; } = [];
    public List<VideoItem> History { get; set; } = [];
}

public sealed record VideoItem
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public string StoredPath { get; init; } = "";
    public long Size { get; init; }
    public string Genre { get; init; } = "";
    public DateTimeOffset TransferredAt { get; init; }
    public string Action { get; init; } = "";
}
