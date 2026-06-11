using System.Text;
using MermaidToDiagrams.Shared;

namespace MermaidToDiagrams.GUI;

public sealed partial class MainForm : Form
{
    private readonly CliRunner _cliRunner = new();
    private readonly EligibilityChecker _eligibilityChecker = new();
    private string? _loadedFilePath;
    private string? _lastValidatedInputPath;
    private bool _lastValidationSucceeded;

    private TextBox _sourceText = null!;
    private TextBox _outputBaseText = null!;
    private ComboBox _formatCombo = null!;
    private ComboBox _themeCombo = null!;
    private ListBox _issuesList = null!;
    private TextBox _logText = null!;
    private Button _validateButton = null!;
    private Button _convertButton = null!;
    private Label _sourceLabel = null!;
    private Label _statusLabel = null!;

    public MainForm()
    {
        InitializeComponent();
        ResetWorkflow();
    }

    private void InitializeComponent()
    {
        Text = "Mermaid To Diagrams";
        Width = 1180;
        Height = 820;
        MinimumSize = new Size(940, 640);
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        Controls.Add(root);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false
        };

        var loadButton = CreateButton("Load file", OnLoadFile);
        var pasteButton = CreateButton("Paste", OnPaste);
        _validateButton = CreateButton("Analyze eligibility", OnValidate);
        _convertButton = CreateButton("Start conversion", OnConvert);
        var resetButton = CreateButton("Start over", (_, _) => ResetWorkflow());
        _convertButton.Enabled = false;

        toolbar.Controls.AddRange([loadButton, pasteButton, _validateButton, _convertButton, resetButton]);
        root.Controls.Add(toolbar, 0, 0);

        var sourcePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        sourcePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sourcePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _sourceLabel = new Label
        {
            AutoSize = true,
            Text = "Mermaid source",
            Padding = new Padding(0, 8, 0, 4)
        };
        _sourceText = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font(FontFamily.GenericMonospace, 10)
        };
        _sourceText.TextChanged += (_, _) =>
        {
            _lastValidationSucceeded = false;
            _convertButton.Enabled = false;
            _statusLabel.Text = "Source changed. Analyze eligibility before conversion.";
        };

        sourcePanel.Controls.Add(_sourceLabel, 0, 0);
        sourcePanel.Controls.Add(_sourceText, 0, 1);
        root.Controls.Add(sourcePanel, 0, 1);

        var settings = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 8,
            RowCount = 1,
            Padding = new Padding(0, 8, 0, 8),
            AutoSize = true
        };
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _outputBaseText = new TextBox { Dock = DockStyle.Fill };
        var browseOutputButton = CreateButton("Browse output", OnBrowseOutput);
        _formatCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
        _formatCombo.Items.AddRange(["png", "svg", "pdf"]);
        _formatCombo.SelectedItem = "png";
        _themeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
        _themeCombo.Items.AddRange(["azure-modern", "azure-dark"]);
        _themeCombo.SelectedItem = "azure-modern";
        _statusLabel = new Label { AutoSize = true, Text = "Ready." };

        settings.Controls.Add(new Label { Text = "Output base:", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 0);
        settings.Controls.Add(_outputBaseText, 1, 0);
        settings.Controls.Add(browseOutputButton, 2, 0);
        settings.Controls.Add(new Label { Text = "Format:", AutoSize = true, Padding = new Padding(12, 6, 8, 0) }, 3, 0);
        settings.Controls.Add(_formatCombo, 4, 0);
        settings.Controls.Add(new Label { Text = "Theme:", AutoSize = true, Padding = new Padding(12, 6, 8, 0) }, 5, 0);
        settings.Controls.Add(_themeCombo, 6, 0);
        settings.Controls.Add(_statusLabel, 7, 0);
        root.Controls.Add(settings, 0, 2);

        var bottom = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 470
        };
        _issuesList = new ListBox { Dock = DockStyle.Fill, HorizontalScrollbar = true };
        _logText = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font(FontFamily.GenericMonospace, 9)
        };

        bottom.Panel1.Controls.Add(WrapWithLabel("Eligibility and conversion issues", _issuesList));
        bottom.Panel2.Controls.Add(WrapWithLabel("CLI output", _logText));
        root.Controls.Add(bottom, 0, 3);
    }

    private static Control WrapWithLabel(string label, Control control)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { AutoSize = true, Text = label, Padding = new Padding(0, 0, 0, 4) }, 0, 0);
        panel.Controls.Add(control, 0, 1);
        return panel;
    }

    private static Button CreateButton(string text, EventHandler handler)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(0, 0, 8, 0)
        };
        button.Click += handler;
        return button;
    }

    private void OnLoadFile(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Mermaid files (*.mmd;*.mermaid)|*.mmd;*.mermaid|Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Select Mermaid file"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _loadedFilePath = dialog.FileName;
        _sourceText.Text = File.ReadAllText(dialog.FileName);
        _sourceLabel.Text = $"Mermaid source: {dialog.FileName}";
        _outputBaseText.Text = Path.Combine(
            Path.GetDirectoryName(dialog.FileName) ?? Environment.CurrentDirectory,
            Path.GetFileNameWithoutExtension(dialog.FileName));
        ClearResults();
        _statusLabel.Text = "File loaded. Analyze eligibility before conversion.";
    }

    private void OnPaste(object? sender, EventArgs e)
    {
        if (Clipboard.ContainsText())
        {
            _loadedFilePath = null;
            _sourceText.Text = Clipboard.GetText();
            _sourceLabel.Text = "Mermaid source: pasted text";
            if (string.IsNullOrWhiteSpace(_outputBaseText.Text))
            {
                _outputBaseText.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "mermaid-diagram");
            }
            ClearResults();
            _statusLabel.Text = "Text pasted. Analyze eligibility before conversion.";
        }
        else
        {
            MessageBox.Show(this, "The clipboard does not contain text.", "Paste", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void OnValidate(object? sender, EventArgs e)
    {
        await ValidateCurrentSourceAsync();
    }

    private async void OnConvert(object? sender, EventArgs e)
    {
        if (!_lastValidationSucceeded)
        {
            await ValidateCurrentSourceAsync();
            if (!_lastValidationSucceeded)
            {
                return;
            }
        }

        var outputBase = _outputBaseText.Text.Trim();
        if (string.IsNullOrWhiteSpace(outputBase))
        {
            AddIssue("Error: Output base path is required.");
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            $"Start conversion now?{Environment.NewLine}{Environment.NewLine}Input: {_lastValidatedInputPath}{Environment.NewLine}Output: {outputBase}.{_formatCombo.SelectedItem}",
            "Confirm conversion",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        await RunConversionAsync(outputBase);
    }

    private void OnBrowseOutput(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Output base path (*.*)|*.*",
            Title = "Choose output file base path",
            FileName = string.IsNullOrWhiteSpace(_outputBaseText.Text) ? "diagram" : Path.GetFileName(_outputBaseText.Text)
        };

        if (!string.IsNullOrWhiteSpace(_outputBaseText.Text))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(_outputBaseText.Text));
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _outputBaseText.Text = Path.Combine(
                Path.GetDirectoryName(dialog.FileName) ?? Environment.CurrentDirectory,
                Path.GetFileNameWithoutExtension(dialog.FileName));
        }
    }

    private async Task ValidateCurrentSourceAsync()
    {
        ClearResults();
        _lastValidationSucceeded = false;
        _convertButton.Enabled = false;

        var source = _sourceText.Text;
        var localIssues = _eligibilityChecker.Check(source);
        foreach (var issue in localIssues)
        {
            AddIssue(issue.ToDisplayText());
        }

        if (localIssues.Any(i => i.Severity == EligibilitySeverity.Error))
        {
            _statusLabel.Text = "Eligibility analysis failed.";
            return;
        }

        var inputPath = await MaterializeInputAsync(source);
        _lastValidatedInputPath = inputPath;
        var result = await _cliRunner.RunAsync(["validate", inputPath]);
        AppendCliResult(result);

        if (result.ExitCode == 0)
        {
            _lastValidationSucceeded = true;
            _convertButton.Enabled = true;
            _statusLabel.Text = "Eligible for conversion.";
            if (_issuesList.Items.Count == 0)
            {
                AddIssue("No eligibility issues detected.");
            }
        }
        else
        {
            AddIssue("Error: CLI validation failed. Inspect CLI output for details.");
            _statusLabel.Text = "CLI validation failed.";
        }
    }

    private async Task RunConversionAsync(string outputBase)
    {
        if (_lastValidatedInputPath is null)
        {
            AddIssue("Error: No validated input is available.");
            return;
        }

        _validateButton.Enabled = false;
        _convertButton.Enabled = false;
        _statusLabel.Text = "Conversion running...";

        try
        {
            var args = new[]
            {
                "render",
                _lastValidatedInputPath,
                "--output",
                outputBase,
                "--format",
                _formatCombo.SelectedItem?.ToString() ?? "png",
                "--theme",
                _themeCombo.SelectedItem?.ToString() ?? "azure-modern",
                "--emit-python",
                "--strict"
            };

            var result = await _cliRunner.RunAsync(args);
            AppendCliResult(result);

            if (result.ExitCode == 0)
            {
                AddIssue("Conversion completed successfully.");
                _statusLabel.Text = "Conversion completed.";
            }
            else
            {
                AddIssue($"Error: CLI conversion failed with exit code {result.ExitCode}.");
                _statusLabel.Text = "Conversion failed.";
            }
        }
        finally
        {
            _validateButton.Enabled = true;
            _convertButton.Enabled = _lastValidationSucceeded;
        }
    }

    private async Task<string> MaterializeInputAsync(string source)
    {
        if (_loadedFilePath is not null && File.Exists(_loadedFilePath))
        {
            return _loadedFilePath;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "m2d-gui");
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, $"pasted-{DateTime.UtcNow:yyyyMMddHHmmssfff}.mmd");
        await File.WriteAllTextAsync(path, source, Encoding.UTF8);
        return path;
    }

    private void ResetWorkflow()
    {
        _loadedFilePath = null;
        _lastValidatedInputPath = null;
        _lastValidationSucceeded = false;
        _sourceText.Text = "";
        _sourceLabel.Text = "Mermaid source";
        _outputBaseText.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "mermaid-diagram");
        _formatCombo.SelectedItem = "png";
        _themeCombo.SelectedItem = "azure-modern";
        _convertButton.Enabled = false;
        ClearResults();
        _statusLabel.Text = "Ready.";
    }

    private void ClearResults()
    {
        _issuesList.Items.Clear();
        _logText.Clear();
    }

    private void AddIssue(string issue)
    {
        _issuesList.Items.Add(issue);
    }

    private void AppendCliResult(CliRunResult result)
    {
        _logText.AppendText($"> {result.CommandLine}{Environment.NewLine}");
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            _logText.AppendText(result.StandardOutput.TrimEnd() + Environment.NewLine);
        }
        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            _logText.AppendText(result.StandardError.TrimEnd() + Environment.NewLine);
        }
        _logText.AppendText($"Exit code: {result.ExitCode}{Environment.NewLine}{Environment.NewLine}");
    }
}
