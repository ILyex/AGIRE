using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Net.Mail;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Drawing.Text;
using System.Windows.Forms;

ApplicationConfiguration.Initialize();

var resetMode = args.Any(argument => string.Equals(argument, "--reset", StringComparison.OrdinalIgnoreCase))
    || string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "REST", StringComparison.OrdinalIgnoreCase);

if (resetMode)
{
    var resetRoot = File.Exists(Path.Combine(AppContext.BaseDirectory, "Hawdh.Portal.exe"))
        ? AppContext.BaseDirectory
        : Path.Combine(AppContext.BaseDirectory, "payload");
    var resetDataRoot = Path.Combine(resetRoot, "data");
    var resetDatabase = Path.Combine(resetDataRoot, "hawdh.local.db");
    var resetMarker = Path.Combine(resetDataRoot, ".setup-complete");
    using var confirm = new ResetConfirmForm();
    if (confirm.ShowDialog() == DialogResult.Yes)
    {
        foreach (var running in Process.GetProcessesByName("Hawdh.Portal"))
        {
            try { running.Kill(true); running.WaitForExit(3000); } catch { }
            running.Dispose();
        }
        Directory.CreateDirectory(Path.Combine(resetDataRoot, "backups"));
        if (File.Exists(resetDatabase)) File.Copy(resetDatabase, Path.Combine(resetDataRoot, "backups", $"before-reset-{DateTime.Now:yyyyMMdd-HHmmss}.db"), true);
        TryDelete(resetDatabase);
        TryDelete(resetMarker);
        var keys = Path.Combine(resetDataRoot, "keys");
        if (Directory.Exists(keys)) Directory.Delete(keys, true);
    }
}

if (resetMode) return;

static void TryDelete(string path)
{
    try { if (File.Exists(path)) File.Delete(path); } catch (Exception error) { MessageBox.Show($"تعذر حذف ملف الإعداد: {error.Message}", "إعادة الإعداد الأولي", MessageBoxButtons.OK, MessageBoxIcon.Error); }
}

Application.Run(new LauncherForm());

internal sealed class ResetConfirmForm : Form
{
    [DllImport("user32.dll")]
    private static extern bool ShowCaret(IntPtr handle);
    private readonly TextBox email = new();
    private readonly TextBox password = new();
    private readonly Panel emailFrame = new();
    private readonly Panel passwordFrame = new();
    private readonly PrivateFontCollection fonts = new();
    private FontFamily? platformFont;
    private readonly Button reset = new();
    private readonly Button passwordToggle = new();
    private readonly Button languageButton = new();
    private Label? titleLabel;
    private Label? messageLabel;
    private bool french;
    public ResetConfirmForm()
    {
        LoadFont();
        Text = "إعادة إعداد المنصة";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(460, 360);
        BackColor = Color.FromArgb(34, 40, 45);
        ForeColor = Color.White;
        RightToLeft = RightToLeft.Yes;
        ApplyRounded(this, 16);

        var title = titleLabel = new Label { Text = "إعادة إعداد المنصة", Bounds = new Rectangle(24, 22, 412, 32), Font = UiFont(16, FontStyle.Bold), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, UseCompatibleTextRendering = false };
        var message = messageLabel = new Label { Text = "أدخل بيانات المدير لتأكيد إعادة الإعداد", Bounds = new Rectangle(24, 65, 412, 28), Font = UiFont(10), ForeColor = Color.FromArgb(166, 190, 201), TextAlign = ContentAlignment.MiddleCenter, UseCompatibleTextRendering = false };
        var divider = new Panel { Bounds = new Rectangle(24, 102, 412, 1), BackColor = Color.FromArgb(65, 76, 82) };
        ConfigureField(emailFrame, email, new Rectangle(24, 125, 412, 40), "email", "البريد الإلكتروني للمدير");
        ConfigureField(passwordFrame, password, new Rectangle(24, 178, 412, 40), "password", "كلمة المرور");
        reset.Text = "إعادة الإعداد"; reset.Bounds = new Rectangle(24, 245, 412, 42); reset.Font = UiFont(10, FontStyle.Bold); reset.BackColor = Color.FromArgb(64, 177, 205); reset.ForeColor = Color.White; reset.FlatStyle = FlatStyle.Flat; reset.UseVisualStyleBackColor = false; reset.FlatAppearance.BorderSize = 0; reset.FlatAppearance.BorderColor = reset.BackColor;
        reset.Paint += (_, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; using var b = new SolidBrush(reset.BackColor); using var p = RoundedPath(new Rectangle(0, 0, reset.Width - 1, reset.Height - 1), 9); e.Graphics.FillPath(b, p); TextRenderer.DrawText(e.Graphics, reset.Text, reset.Font, reset.ClientRectangle, reset.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding); };
        reset.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(email.Text) || password.Text.Length < 8)
            {
                MessageBox.Show("أدخل البريد وكلمة المرور الصحيحة للمتابعة.", "إعادة الإعداد", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult = DialogResult.Yes;
        };
        ApplyRounded(reset, 9);
        var no = MakeButton("إلغاء", DialogResult.No, Color.FromArgb(43, 60, 68));
        no.Bounds = new Rectangle(24, 300, 198, 38);
        ApplyRounded(no, 9);
        var close = new Button { Text = string.Empty, Bounds = new Rectangle(402, 20, 34, 34), Font = UiFont(17), BackColor = Color.FromArgb(43, 60, 68), ForeColor = Color.FromArgb(190, 205, 210), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel, UseVisualStyleBackColor = false };
        close.FlatAppearance.BorderSize = 0;
        close.Paint += (_, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; using var p = new Pen(close.ForeColor, 1.4f); e.Graphics.DrawLine(p, 11, 11, 23, 23); e.Graphics.DrawLine(p, 23, 11, 11, 23); };
        ApplyRounded(close, 10);
        languageButton.Text = string.Empty; languageButton.Bounds = new Rectangle(24, 20, 34, 34); languageButton.FlatStyle = FlatStyle.Flat; languageButton.FlatAppearance.BorderSize = 0; languageButton.BackColor = Color.FromArgb(43, 60, 68); languageButton.UseVisualStyleBackColor = false; languageButton.TabStop = false;
        languageButton.Paint += (_, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic; using var icon = LoadTintedIcon("global.png"); e.Graphics.DrawImage(icon, new Rectangle(7, 7, 20, 20)); };
        languageButton.Click += (_, _) => SetLanguage(!french); ApplyRounded(languageButton, 9);
        Controls.AddRange([title, message, divider, emailFrame, passwordFrame, reset, no, languageButton, close]);
        languageButton.BringToFront(); close.BringToFront();
        close.BringToFront();
        close.Invalidate();
        AcceptButton = reset;
        CancelButton = no;
    }

    private void SetLanguage(bool useFrench)
    {
        french = useFrench;
        titleLabel!.Text = french ? "Réinitialiser la plateforme" : "إعادة إعداد المنصة";
        messageLabel!.Text = french ? "Saisissez les identifiants de l’administrateur" : "أدخل بيانات المدير لتأكيد إعادة الإعداد";
        email.PlaceholderText = french ? "E-mail de l’administrateur" : "البريد الإلكتروني للمدير";
        password.PlaceholderText = french ? "Mot de passe" : "كلمة المرور";
        reset.Text = french ? "Réinitialiser" : "إعادة الإعداد";
        if (Controls.OfType<Button>().FirstOrDefault(button => button.DialogResult == DialogResult.No) is { } cancel) cancel.Text = french ? "Annuler" : "إلغاء";
        email.TextAlign = password.TextAlign = french ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        titleLabel.Font = UiFont(french ? 15 : 16, FontStyle.Bold);
        Invalidate(true);
    }

    private Button MakeButton(string text, DialogResult result, Color color)
    {
        var button = new Button { Text = text, DialogResult = result, FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Color.White, Font = UiFont(10, FontStyle.Bold), UseVisualStyleBackColor = false, RightToLeft = RightToLeft.No, TextAlign = ContentAlignment.MiddleCenter };
        button.FlatAppearance.BorderSize = 0;
        ApplyRounded(button, 9);
        return button;
    }

    private void ConfigureField(Panel frame, TextBox input, Rectangle bounds, string kind, string placeholder)
    {
        frame.Bounds = bounds; frame.BackColor = Color.FromArgb(28, 33, 38); frame.Padding = Padding.Empty; frame.Tag = kind;
        ApplyRounded(frame, 10);
        input.Dock = DockStyle.None; input.BorderStyle = BorderStyle.None; input.Font = UiFont(11.5f); input.BackColor = frame.BackColor; input.ForeColor = Color.White; input.PlaceholderText = placeholder; input.TextAlign = HorizontalAlignment.Right; input.RightToLeft = RightToLeft.No; input.AutoSize = false; input.Size = new Size(bounds.Width - 76, 30); input.Location = new Point(12, 5);
        if (kind == "password") input.PasswordChar = '●';
        frame.Controls.Add(input);
        input.MouseDown += (_, _) => FocusResetInput(input);
        input.GotFocus += (_, _) =>
        {
            input.SelectionStart = input.TextLength;
            input.SelectionLength = 0;
            if (input.IsHandleCreated) ShowCaret(input.Handle);
            frame.Invalidate();
        };
        frame.MouseDown += (_, _) => FocusResetInput(input);
        if (kind == "password")
        {
            passwordToggle.Text = string.Empty; passwordToggle.Bounds = new Rectangle(8, 5, 26, 30); passwordToggle.FlatStyle = FlatStyle.Flat; passwordToggle.FlatAppearance.BorderSize = 0; passwordToggle.BackColor = Color.Transparent; passwordToggle.UseVisualStyleBackColor = false; passwordToggle.TabStop = false;
            passwordToggle.Paint += (_, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; var icon = LoadTintedIcon(password.PasswordChar == '\0' ? "w.png" : "view.png"); e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic; e.Graphics.DrawImage(icon, new Rectangle(3, 5, 20, 20)); icon.Dispose(); };
            passwordToggle.Click += (_, _) => { password.PasswordChar = password.PasswordChar == '\0' ? '●' : '\0'; password.Focus(); password.SelectionStart = password.TextLength; passwordToggle.Invalidate(); };
            frame.Controls.Add(passwordToggle); passwordToggle.BringToFront();
        }
        frame.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(input.Focused ? Color.FromArgb(37, 164, 190) : Color.FromArgb(67, 78, 84), 1);
            using var path = RoundedPath(new Rectangle(1, 1, frame.Width - 3, frame.Height - 3), 9);
            e.Graphics.DrawPath(pen, path);
            var ix = frame.Width - 36; var iy = 10;
            using var icon = LoadTintedIcon(kind == "email" ? "mail.png" : "password.png");
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(icon, new Rectangle(ix, iy, 20, 20));
        };
        input.Enter += (_, _) => frame.Invalidate(); input.Leave += (_, _) => frame.Invalidate();
    }

    private static void FocusResetInput(TextBox input)
    {
        input.Visible = true;
        input.Enabled = true;
        var focused = input.Focus();
        input.SelectionStart = input.TextLength;
        input.SelectionLength = 0;
        if (focused && input.IsHandleCreated) ShowCaret(input.Handle);
        input.Invalidate();
    }

    private Bitmap LoadTintedIcon(string name)
    {
        using var stream = typeof(ResetConfirmForm).Assembly.GetManifestResourceStream($"Hawdh.Launcher.Assets.{name}")!;
        using var source = new Bitmap(stream);
        var result = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var color = Color.FromArgb(139, 169, 179);
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++) { var p = source.GetPixel(x, y); result.SetPixel(x, y, Color.FromArgb(p.A, color.R, color.G, color.B)); }
        return result;
    }

    private void LoadFont()
    {
        using var stream = typeof(ResetConfirmForm).Assembly.GetManifestResourceStream("Hawdh.Launcher.Assets.IBMPlexSansArabic-Regular.ttf");
        if (stream is null) return; using var buffer = new MemoryStream(); stream.CopyTo(buffer); var bytes = buffer.ToArray(); var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned); try { fonts.AddMemoryFont(handle.AddrOfPinnedObject(), bytes.Length); platformFont = fonts.Families.FirstOrDefault(); } finally { handle.Free(); }
    }

    private Font UiFont(float size, FontStyle style = FontStyle.Regular) => platformFont is null ? new Font("Segoe UI", size, style) : new Font(platformFont, size, style);

    private static System.Drawing.Drawing2D.GraphicsPath RoundedPath(Rectangle box, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath(); var d = radius * 2;
        path.AddArc(box.X, box.Y, d, d, 180, 90); path.AddArc(box.Right - d, box.Y, d, d, 270, 90); path.AddArc(box.Right - d, box.Bottom - d, d, d, 0, 90); path.AddArc(box.X, box.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path;
    }

    private static void ApplyRounded(Control control, int radius)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        var d = radius * 2;
        path.AddArc(0, 0, d, d, 180, 90); path.AddArc(control.Width - d, 0, d, d, 270, 90); path.AddArc(control.Width - d, control.Height - d, d, d, 0, 90); path.AddArc(0, control.Height - d, d, d, 90, 90); path.CloseFigure();
        control.Region = new Region(path);
    }
}

internal sealed class LauncherForm : Form
{
    private const int EmSetCueBanner = 0x1501;
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, string text);
    [DllImport("user32.dll")]
    private static extern bool ShowCaret(IntPtr handle);
    private readonly TextBox email = new();
    private readonly TextBox password = new();
    private readonly Button passwordToggle = new();
    private readonly Button startButton = new();
    private readonly Button languageButton = new();
    private readonly Button themeButton = new();
    private readonly Button minimizeButton = new();
    private readonly Button closeButton = new();
    private readonly Label titleLabel = new();
    private readonly Label subtitleLabel = new();
    private readonly Label emailLabel = new();
    private readonly Label passwordLabel = new();
    private readonly Label status = new();
    private readonly Bitmap mailIconDark = TintIcon(LoadIconAsset("mail.png"), Color.FromArgb(139, 169, 179));
    private readonly Bitmap passwordIconDark = TintIcon(LoadIconAsset("password.png"), Color.FromArgb(139, 169, 179));
    private readonly Bitmap mailIconLight = TintIcon(LoadIconAsset("mail.png"), Color.FromArgb(69, 111, 124));
    private readonly Bitmap passwordIconLight = TintIcon(LoadIconAsset("password.png"), Color.FromArgb(69, 111, 124));
    private readonly Bitmap globalIconDark = TintIcon(LoadIconAsset("global.png"), Color.FromArgb(171, 205, 214));
    private readonly Bitmap globalIconLight = TintIcon(LoadIconAsset("global.png"), Color.FromArgb(45, 113, 130));
    private readonly Bitmap nightModeIconDark = TintIcon(LoadIconAsset("night-mode.png"), Color.FromArgb(171, 205, 214));
    private readonly Bitmap nightModeIconLight = TintIcon(LoadIconAsset("night-mode.png"), Color.FromArgb(45, 113, 130));
    private readonly Bitmap viewIconDark = TintIcon(LoadIconAsset("view.png"), Color.FromArgb(139, 169, 179));
    private readonly Bitmap viewIconLight = TintIcon(LoadIconAsset("view.png"), Color.FromArgb(69, 111, 124));
    private readonly Bitmap hideIconDark = TintIcon(LoadIconAsset("w.png"), Color.FromArgb(139, 169, 179));
    private readonly Bitmap hideIconLight = TintIcon(LoadIconAsset("w.png"), Color.FromArgb(69, 111, 124));
    private Panel? cardPanel;
    private Panel? emailFrameControl;
    private Panel? passwordFrameControl;
    private Panel? dividerControl;
    private Label? emailHint;
    private Label? passwordHint;
    private string statusArabic = "جاري تشغيل المنصة...";
    private readonly Panel progress = new();
    private Process? server;
    private bool french;
    private bool lightTheme;
    private bool isStarting;
    private readonly string root = AppContext.BaseDirectory;
    private readonly PrivateFontCollection platformFonts = new();
    private FontFamily? platformFont;
    private string Payload => File.Exists(Path.Combine(root, "Hawdh.Portal.exe")) ? root : Path.Combine(root, "payload");
    private string DataRoot => Path.Combine(Payload, "data");
    private const int Port = 5182;

    public LauncherForm()
    {
        LoadPlatformFont();
        Text = "AGIRE";
        AutoScaleMode = AutoScaleMode.None;
        AutoScaleDimensions = new SizeF(96, 96);
        RightToLeft = RightToLeft.No;
        RightToLeftLayout = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(520, 390);
        MinimumSize = MaximumSize = new Size(520, 390);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        try
        {
            if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
                Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath);
        }
        catch { }
        BackColor = Color.FromArgb(34, 40, 45);
        ForeColor = Color.White;
        ApplyRounded(this, 18);

        var card = new Panel { Bounds = new Rectangle(0, 0, 520, 390), BackColor = Color.FromArgb(34, 40, 45), Padding = new Padding(24), TabStop = true, TabIndex = 99 };
        card.RightToLeft = RightToLeft.No;
        ApplyRounded(card, 18);
        var close = closeButton;
        close.Text = string.Empty; close.FlatStyle = FlatStyle.Flat; close.Bounds = new Rectangle(458, 24, 30, 30); close.BackColor = Color.FromArgb(43, 60, 68); close.ForeColor = Color.FromArgb(190, 205, 210); close.TabStop = false; close.UseVisualStyleBackColor = false;
        close.FlatAppearance.BorderSize = 0;
        close.FlatAppearance.MouseOverBackColor = Color.FromArgb(53, 75, 84);
        close.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(190, 205, 210), 1.4f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
            e.Graphics.DrawLine(pen, 11, 11, 21, 21);
            e.Graphics.DrawLine(pen, 21, 11, 11, 21);
        };
        close.Click += (_, _) => Close();
        ApplyRounded(close, 9);
        var minimize = minimizeButton;
        minimize.Text = string.Empty; minimize.FlatStyle = FlatStyle.Flat; minimize.Bounds = new Rectangle(420, 24, 30, 30); minimize.BackColor = Color.FromArgb(43, 60, 68); minimize.ForeColor = Color.FromArgb(190, 205, 210); minimize.TabStop = false; minimize.UseVisualStyleBackColor = false;
        minimize.FlatAppearance.BorderSize = 0;
        minimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(53, 75, 84);
        minimize.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(190, 205, 210), 1.5f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
            e.Graphics.DrawLine(pen, 9, 16, 21, 16);
        };
        minimize.Click += (_, _) => WindowState = FormWindowState.Minimized;
        ApplyRounded(minimize, 9);
        titleLabel.Text = "إعداد المنصة"; titleLabel.Font = UiFont(17, FontStyle.Bold); titleLabel.AutoSize = false; titleLabel.Size = new Size(460, 32); titleLabel.Location = new Point(30, 25); titleLabel.TextAlign = ContentAlignment.MiddleCenter; titleLabel.ForeColor = Color.White; titleLabel.UseCompatibleTextRendering = false; titleLabel.AutoEllipsis = true;
        var firstRun = !File.Exists(Path.Combine(DataRoot, ".setup-complete"))
            && !File.Exists(Path.Combine(DataRoot, "hawdh.local.db"));
        subtitleLabel.Text = firstRun ? "أدخل بيانات المدير مرة واحدة لبدء الاستخدام" : "جاري فتح المنصة على هذا الكمبيوتر"; subtitleLabel.AutoSize = false; subtitleLabel.Size = new Size(460, 22); subtitleLabel.Location = new Point(30, 62); subtitleLabel.TextAlign = ContentAlignment.MiddleCenter; subtitleLabel.ForeColor = Color.FromArgb(166, 190, 201); subtitleLabel.Font = UiFont(9); subtitleLabel.UseCompatibleTextRendering = true;
        var divider = dividerControl = new Panel { Bounds = new Rectangle(30, 94, 460, 1), BackColor = Color.FromArgb(65, 76, 82) };
        emailLabel.Text = "البريد الإلكتروني للمدير"; emailLabel.AutoSize = false; emailLabel.Size = new Size(460, 20); emailLabel.Location = new Point(30, 112); emailLabel.TextAlign = ContentAlignment.MiddleCenter; emailLabel.ForeColor = Color.FromArgb(210, 224, 230); emailLabel.Font = UiFont(10); emailLabel.UseCompatibleTextRendering = true;
        var emailFrame = emailFrameControl = MakeInputFrame(email, new Rectangle(30, 137, 460, 40));
        emailHint = MakeHint(emailFrame, "أدخل البريد الإلكتروني");
        email.TextChanged += (_, _) => UpdateHints();
        passwordLabel.Text = "كلمة المرور"; passwordLabel.AutoSize = false; passwordLabel.Size = new Size(460, 20); passwordLabel.Location = new Point(30, 185); passwordLabel.TextAlign = ContentAlignment.MiddleCenter; passwordLabel.ForeColor = Color.FromArgb(210, 224, 230); passwordLabel.Font = UiFont(10); passwordLabel.UseCompatibleTextRendering = true;
        var passwordFrame = passwordFrameControl = MakeInputFrame(password, new Rectangle(30, 210, 460, 40));
        passwordHint = MakeHint(passwordFrame, "أدخل كلمة المرور");
        password.TextChanged += (_, _) => UpdateHints();
        password.UseSystemPasswordChar = false;
        password.PasswordChar = '●';
        startButton.Text = "تشغيل المنصة"; startButton.SetBounds(30, 275, 460, 42); startButton.Font = UiFont(10, FontStyle.Bold); startButton.BackColor = Color.FromArgb(64, 177, 205); startButton.ForeColor = Color.White; startButton.FlatStyle = FlatStyle.Flat; startButton.FlatAppearance.BorderSize = 0; startButton.FlatAppearance.BorderColor = Color.FromArgb(64, 177, 205); startButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(76, 190, 216); startButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(51, 153, 179); startButton.Click += StartClicked;
        ApplyRounded(email, 10); ApplyRounded(password, 10); ApplyRounded(startButton, 10);
        status.Text = ""; status.AutoSize = false; status.TextAlign = ContentAlignment.MiddleCenter; status.SetBounds(30, 330, 460, 30); status.ForeColor = Color.FromArgb(166, 190, 201);
        // Keep the header controls in their own safe area so they never overlap the title.
        languageButton.Text = string.Empty; languageButton.SetBounds(30, 24, 36, 32); ConfigureToolButton(languageButton); languageButton.Paint += PaintLanguageIcon; languageButton.Click += (_, _) => SetLanguage(!french);
        themeButton.Text = string.Empty; themeButton.SetBounds(72, 24, 36, 32); ConfigureToolButton(themeButton); themeButton.Paint += PaintThemeIcon; themeButton.Click += (_, _) => SetTheme(!lightTheme);
        card.Controls.AddRange([close, minimize, titleLabel, subtitleLabel, divider, emailLabel, emailFrame, passwordLabel, passwordFrame, startButton, status, languageButton, themeButton]);
        languageButton.AutoSize = false; languageButton.Size = new Size(32, 30); languageButton.Visible = true; languageButton.BringToFront();
        themeButton.AutoSize = false; themeButton.Size = new Size(32, 30); themeButton.Visible = true; themeButton.BringToFront();
        ApplyRounded(languageButton, 9);
        ApplyRounded(themeButton, 9);
        close.BringToFront();
        minimize.BringToFront();
        cardPanel = card;
        Controls.Add(card);
        if (!firstRun)
        {
            // Existing installations start silently; credentials are only requested once.
            card.Height = 228;
            emailLabel.Visible = false;
            emailFrame.Visible = false;
            passwordLabel.Visible = false;
            passwordFrame.Visible = false;
            startButton.Text = "فتح المنصة";
            startButton.SetBounds(30, 137, 460, 42);
            status.SetBounds(30, 195, 460, 30);
            ClientSize = new Size(520, 260);
            card.Bounds = new Rectangle(0, 0, 520, 260);
            ApplyRounded(card, 18);
            ApplyRounded(this, 18);
        }
        else
        {
            emailLabel.Visible = false;
            passwordLabel.Visible = false;
        }
        SetLanguage(false);
        AcceptButton = startButton;
        Shown += (_, _) =>
        {
            ApplyRounded(this, 18);
            ApplyRounded(card, 18);
            // Do not open with the first TextBox focused. The user must click
            // a field before the caret appears and typing begins.
            if (firstRun)
            {
                BeginInvoke(new Action(() =>
                {
                    card.Focus();
                    ActiveControl = null;
                }));
            }
            if (File.Exists(Path.Combine(Payload, "data", "hawdh.local.db"))) StartClicked(null, EventArgs.Empty);
        };
    }

    private Label MakeLabel(string text, int x, int y) => new() { Text = text, AutoSize = true, Location = new Point(x, y), ForeColor = Color.FromArgb(210, 224, 230), Font = UiFont(10), UseCompatibleTextRendering = true };

    private void ConfigureToolButton(Button button)
    {
        button.AutoSize = false; button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0; button.BackColor = Color.FromArgb(34, 40, 45); button.ForeColor = Color.FromArgb(190, 205, 210); button.Font = UiFont(9, FontStyle.Bold); button.UseVisualStyleBackColor = false; button.TextAlign = ContentAlignment.MiddleCenter; button.TabStop = false;
        // Clip the native WinForms hover surface to the same rounded silhouette
        // as the custom icon background. This prevents a square halo on hover.
        ApplyRounded(button, 9);
        // Transparent native states let the custom rounded surface remain the
        // only background, including while hovering and pressing.
        button.FlatAppearance.MouseOverBackColor = Color.Transparent;
        button.FlatAppearance.MouseDownBackColor = Color.Transparent;
        button.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(lightTheme ? Color.FromArgb(226, 237, 239) : Color.FromArgb(43, 60, 68));
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            var box = new Rectangle(0, 0, button.Width - 1, button.Height - 1);
            path.AddArc(box.X, box.Y, 16, 16, 180, 90); path.AddArc(box.Right - 16, box.Y, 16, 16, 270, 90); path.AddArc(box.Right - 16, box.Bottom - 16, 16, 16, 0, 90); path.AddArc(box.X, box.Bottom - 16, 16, 16, 90, 90); path.CloseFigure();
            e.Graphics.FillPath(brush, path);
            TextRenderer.DrawText(e.Graphics, button.Text, button.Font, button.ClientRectangle, button.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        };
    }

    private void SetLanguage(bool useFrench)
    {
        french = useFrench;
        RightToLeft = RightToLeft.No;
        RightToLeftLayout = false;
        titleLabel.Text = french ? "Configuration de la plateforme" : "إعداد المنصة";
        subtitleLabel.Text = french ? "Saisissez les données de l’administrateur une seule fois" : "أدخل بيانات المدير مرة واحدة لبدء الاستخدام";
        emailLabel.Text = french ? "E-mail de l’administrateur" : "البريد الإلكتروني للمدير";
        passwordLabel.Text = french ? "Mot de passe" : "كلمة المرور";
        startButton.Text = french ? "Démarrer la plateforme" : "تشغيل المنصة";
        // Follow the selected language's reading direction while the frame
        // padding keeps text clear of the icon columns.
        email.TextAlign = french ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        password.TextAlign = email.TextAlign;
        // Keep the input's layout direction neutral so Windows does not move
        // the native placeholder to an edge when Arabic is selected. The
        // Arabic glyphs still render correctly, while alignment stays centered.
        email.RightToLeft = RightToLeft.No;
        password.RightToLeft = RightToLeft.No;
        email.PlaceholderText = french ? "Saisissez l’adresse e-mail" : "أدخل البريد الإلكتروني";
        password.PlaceholderText = french ? "Saisissez le mot de passe" : "أدخل كلمة المرور";
        if (emailHint is not null) emailHint.Text = email.PlaceholderText;
        if (passwordHint is not null) passwordHint.Text = password.PlaceholderText;
        UpdateHints();
        if (isStarting) status.Text = french ? "Démarrage de la plateforme…" : statusArabic;
        SetInputFrameLanguage(emailFrameControl, french);
        SetInputFrameLanguage(passwordFrameControl, french);
        SetHintLanguage(emailHint, french);
        SetHintLanguage(passwordHint, french);
        emailLabel.RightToLeft = passwordLabel.RightToLeft = RightToLeft.No;
        emailLabel.TextAlign = passwordLabel.TextAlign = ContentAlignment.MiddleCenter;
        titleLabel.Location = new Point(30, 25);
        titleLabel.Font = UiFont(french ? 15 : 17, FontStyle.Bold);
        titleLabel.RightToLeft = french ? RightToLeft.No : RightToLeft.Yes;
        subtitleLabel.RightToLeft = french ? RightToLeft.No : RightToLeft.Yes;
        // Keep the supporting sentence centered beneath the title in both languages.
        // RightToLeft still controls glyph direction for Arabic, while alignment stays
        // anchored to the same visual center as the heading.
        subtitleLabel.TextAlign = ContentAlignment.MiddleCenter;
        titleLabel.Invalidate();
    }

    private void PaintLanguageIcon(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        var icon = lightTheme ? globalIconLight : globalIconDark;
        e.Graphics.DrawImage(icon, new Rectangle(6, 6, 20, 20));
    }

    private void SetInputFrameLanguage(Panel? frame, bool useFrench)
    {
        if (frame is null) return;
        // Reserve both icon columns so typed text never collides with the lock or eye.
        frame.Padding = new Padding(52, 3, 52, 3);
        if (ReferenceEquals(frame, passwordFrameControl))
            passwordToggle.Bounds = useFrench ? new Rectangle(frame.Width - 36, 5, 26, 30) : new Rectangle(8, 5, 26, 30);
        frame.Invalidate();
    }

    private static void SetHintLanguage(Label? hint, bool useFrench)
    {
        if (hint is null) return;
        hint.TextAlign = useFrench ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleRight;
        hint.Padding = useFrench ? new Padding(2, 0, 0, 0) : new Padding(0, 0, 2, 0);
    }

    private static void SetCueBanner(TextBox input, string text)
    {
        if (input.IsHandleCreated) SendMessage(input.Handle, EmSetCueBanner, IntPtr.Zero, text);
        else input.HandleCreated += (_, _) => SendMessage(input.Handle, EmSetCueBanner, IntPtr.Zero, text);
    }

    private Label MakeHint(Panel frame, string text)
    {
        var hint = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            Enabled = true,
            TabStop = false,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(145, 169, 177),
            Font = UiFont(9),
            TextAlign = ContentAlignment.MiddleCenter,
            UseCompatibleTextRendering = false,
            UseMnemonic = false,
            Cursor = Cursors.IBeam
        };
        // The hint is visual only. Forward its click to the real TextBox so the
        // user can click directly on the placeholder and start typing.
        var target = Equals(frame.Tag, "email") ? email : password;
        hint.MouseDown += (_, _) =>
        {
            target.Focus();
            target.SelectionStart = target.TextLength;
        };
        // Use the native TextBox placeholder instead of a child overlay. A child
        // label can visually overlap typed text and can steal input focus.
        hint.Visible = false;
        return hint;
    }

    private void UpdateHints()
    {
        if (emailHint is not null) emailHint.Visible = string.IsNullOrEmpty(email.Text);
        if (passwordHint is not null) passwordHint.Visible = string.IsNullOrEmpty(password.Text);
    }

    private void SetTheme(bool useLight)
    {
        lightTheme = useLight;
        var background = lightTheme ? Color.FromArgb(242, 246, 247) : Color.FromArgb(34, 40, 45);
        var inputBackground = lightTheme ? Color.White : Color.FromArgb(28, 33, 38);
        BackColor = background;
        cardPanel?.BackColor = background;
        emailFrameControl?.BackColor = inputBackground;
        passwordFrameControl?.BackColor = inputBackground;
        titleLabel.ForeColor = lightTheme ? Color.FromArgb(25, 54, 65) : Color.White;
        subtitleLabel.ForeColor = lightTheme ? Color.FromArgb(94, 116, 124) : Color.FromArgb(166, 190, 201);
        emailLabel.ForeColor = passwordLabel.ForeColor = lightTheme ? Color.FromArgb(68, 88, 96) : Color.FromArgb(210, 224, 230);
        email.BackColor = password.BackColor = inputBackground;
        email.ForeColor = password.ForeColor = lightTheme ? Color.FromArgb(30, 45, 52) : Color.White;
        passwordToggle.BackColor = Color.Transparent;
        passwordToggle.FlatAppearance.MouseOverBackColor = passwordToggle.FlatAppearance.MouseDownBackColor = Color.Transparent;
        status.ForeColor = lightTheme ? Color.FromArgb(94, 116, 124) : Color.FromArgb(166, 190, 201);
        if (emailHint is not null) emailHint.ForeColor = passwordHint!.ForeColor = lightTheme ? Color.FromArgb(100, 124, 132) : Color.FromArgb(119, 145, 154);
        dividerControl?.BackColor = lightTheme ? Color.FromArgb(214, 224, 227) : Color.FromArgb(65, 76, 82);
        closeButton.BackColor = lightTheme ? Color.FromArgb(227, 237, 239) : Color.FromArgb(43, 60, 68);
        minimizeButton.BackColor = lightTheme ? Color.FromArgb(227, 237, 239) : Color.FromArgb(43, 60, 68);
        minimizeButton.FlatAppearance.MouseOverBackColor = lightTheme ? Color.FromArgb(214, 229, 232) : Color.FromArgb(53, 75, 84);
        languageButton.BackColor = themeButton.BackColor = lightTheme ? Color.FromArgb(242, 246, 247) : Color.FromArgb(34, 40, 45);
        languageButton.FlatAppearance.MouseOverBackColor = languageButton.FlatAppearance.MouseDownBackColor = Color.Transparent;
        themeButton.FlatAppearance.MouseOverBackColor = themeButton.FlatAppearance.MouseDownBackColor = Color.Transparent;
        closeButton.ForeColor = languageButton.ForeColor = themeButton.ForeColor = lightTheme ? Color.FromArgb(67, 101, 111) : Color.FromArgb(190, 205, 210);
        minimizeButton.ForeColor = lightTheme ? Color.FromArgb(67, 101, 111) : Color.FromArgb(190, 205, 210);
        themeButton.Text = string.Empty;
        closeButton.Invalidate();
        minimizeButton.Invalidate();
        emailFrameControl?.Invalidate();
        passwordFrameControl?.Invalidate();
        passwordToggle.Invalidate();
        themeButton.Invalidate();
    }

    private void PaintThemeIcon(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        var icon = lightTheme ? nightModeIconLight : nightModeIconDark;
        e.Graphics.DrawImage(icon, new Rectangle(6, 6, 20, 20));
    }

    private Panel MakeInputFrame(TextBox input, Rectangle bounds)
    {
        var isPassword = ReferenceEquals(input, password);
        var frame = new Panel { Bounds = bounds, BackColor = Color.FromArgb(28, 33, 38), Padding = new Padding(52, 3, 52, 3), Tag = isPassword ? "password" : "email" };
        ApplyRounded(frame, 10);
        input.Dock = DockStyle.None;
        input.BorderStyle = BorderStyle.None;
        // Use the embedded platform font so the launcher matches the web UI
        // without requiring a font installation on the user's PC.
        input.Font = UiFont(ReferenceEquals(input, password) ? 12f : 11.5f);
        // Keep it a single-line editor so Windows centers the caret and glyphs
        // vertically inside the 40px field instead of pinning them to the top.
        input.Multiline = false;
        input.Padding = Padding.Empty;
        input.AutoSize = false;
        // Match the editor height to the actual IBM Plex line metrics, then
        // center that editor inside the 40px frame for both languages.
        input.Size = new Size(bounds.Width - 104, 24);
        input.Location = new Point(52, 8);
        input.TabStop = true;
        input.ReadOnly = false;
        input.HideSelection = false;
        input.Cursor = Cursors.IBeam;
        input.BackColor = frame.BackColor;
        input.ForeColor = Color.White;
        frame.Controls.Add(input);
        input.BringToFront();
        if (isPassword)
        {
            passwordToggle.Text = string.Empty;
            passwordToggle.TabStop = false;
            passwordToggle.FlatStyle = FlatStyle.Flat;
            passwordToggle.FlatAppearance.BorderSize = 0;
            passwordToggle.BackColor = Color.Transparent;
            passwordToggle.UseVisualStyleBackColor = false;
            passwordToggle.FlatAppearance.MouseOverBackColor = Color.Transparent;
            passwordToggle.FlatAppearance.MouseDownBackColor = Color.Transparent;
            passwordToggle.Cursor = Cursors.Hand;
            passwordToggle.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                var icon = password.PasswordChar != '\0'
                    ? (lightTheme ? viewIconLight : viewIconDark)
                    : (lightTheme ? hideIconLight : hideIconDark);
                e.Graphics.DrawImage(icon, new Rectangle(3, 5, 20, 20));
            };
            passwordToggle.Click += (_, _) =>
            {
                var showing = password.PasswordChar == '\0';
                password.PasswordChar = showing ? '●' : '\0';
                password.TextAlign = french ? HorizontalAlignment.Left : HorizontalAlignment.Right;
                password.Focus();
                password.SelectionStart = password.TextLength;
                passwordToggle.Invalidate();
            };
            frame.Controls.Add(passwordToggle);
            passwordToggle.BringToFront();
            passwordToggle.Bounds = french ? new Rectangle(frame.Width - 36, 5, 26, 30) : new Rectangle(8, 5, 26, 30);
        }
        frame.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(input.Focused ? Color.FromArgb(37, 164, 190) : (lightTheme ? Color.FromArgb(190, 205, 209) : Color.FromArgb(67, 78, 84)), 1);
            var rectangle = new Rectangle(1, 1, frame.Width - 3, frame.Height - 3);
            using var path = RoundedPath(rectangle, 9);
            e.Graphics.DrawPath(pen, path);
            var iconX = french ? 16 : frame.Width - 36;
            var iconY = (frame.Height - 20) / 2;
            // These are the supplied icons, recolored to the launcher palette and
            // scaled into the same 20px optical box in both directions.
            var isEmail = Equals(frame.Tag, "email");
            var icon = lightTheme
                ? (isEmail ? mailIconLight : passwordIconLight)
                : (isEmail ? mailIconDark : passwordIconDark);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(icon, new Rectangle(iconX, iconY, 20, 20));
        };
        frame.MouseDown += (_, _) =>
        {
            FocusInput(input);
        };
        input.MouseDown += (_, _) => FocusInput(input);
        input.GotFocus += (_, _) =>
        {
            input.SelectionStart = input.TextLength;
            input.SelectionLength = 0;
            ShowCaret(input.Handle);
            frame.Invalidate();
        };
        input.Enter += (_, _) => frame.Invalidate();
        input.Leave += (_, _) => frame.Invalidate();
        return frame;
    }

    private static void FocusInput(TextBox input)
    {
        input.Visible = true;
        input.Enabled = true;
        var focused = input.Focus();
        input.SelectionStart = input.TextLength;
        input.SelectionLength = 0;
        if (focused && input.IsHandleCreated)
        {
            ShowCaret(input.Handle);
            input.Select();
        }
        input.Invalidate();
    }

    private void LoadPlatformFont()
    {
        var stream = typeof(LauncherForm).Assembly.GetManifestResourceStream("Hawdh.Launcher.Assets.IBMPlexSansArabic-Regular.ttf");
        if (stream is null) return;
        using (stream)
        using (var buffer = new MemoryStream())
        {
            stream.CopyTo(buffer);
            var bytes = buffer.ToArray();
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                platformFonts.AddMemoryFont(handle.AddrOfPinnedObject(), bytes.Length);
                platformFont = platformFonts.Families.FirstOrDefault();
            }
            finally { handle.Free(); }
        }
    }

    private Font UiFont(float size, FontStyle style = FontStyle.Regular)
        => platformFont is null
            ? new Font("Segoe UI", size, style, GraphicsUnit.Point)
            : new Font(platformFont, size, style, GraphicsUnit.Point);

    private static Bitmap LoadIconAsset(string fileName)
    {
        var stream = typeof(LauncherForm).Assembly.GetManifestResourceStream($"Hawdh.Launcher.Assets.{fileName}")
            ?? throw new InvalidOperationException($"Missing launcher icon resource: {fileName}");
        using (stream)
        using (var source = new Bitmap(stream))
            return new Bitmap(source);
    }

    private static Bitmap LoadBrandIcon()
    {
        using var stream = typeof(LauncherForm).Assembly.GetManifestResourceStream("Hawdh.Launcher.Assets.agire-installer.png")!;
        return new Bitmap(stream);
    }

    private static Bitmap TintIcon(Bitmap source, Color color)
    {
        var tinted = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        for (var y = 0; y < source.Height; y++)
            for (var x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);
                tinted.SetPixel(x, y, Color.FromArgb(pixel.A, color.R, color.G, color.B));
            }
        source.Dispose();
        return tinted;
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedPath(Rectangle box, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        var d = radius * 2;
        path.AddArc(box.X, box.Y, d, d, 180, 90); path.AddArc(box.Right - d, box.Y, d, d, 270, 90); path.AddArc(box.Right - d, box.Bottom - d, d, d, 0, 90); path.AddArc(box.X, box.Bottom - d, d, d, 90, 90); path.CloseFigure();
        return path;
    }

    private static void ApplyRounded(Control control, int radius)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        var d = radius * 2;
        path.AddArc(0, 0, d, d, 180, 90); path.AddArc(control.Width - d, 0, d, d, 270, 90);
        path.AddArc(control.Width - d, control.Height - d, d, d, 0, 90); path.AddArc(0, control.Height - d, d, d, 90, 90); path.CloseFigure();
        control.Region = new Region(path);
    }

    private async void StartClicked(object? sender, EventArgs e)
    {
        if (isStarting) return;
        startButton.Enabled = false;
        isStarting = true;
        statusArabic = "جاري تشغيل المنصة...";
        status.Text = french ? "Démarrage de la plateforme…" : statusArabic;
        try
        {
            using (var existingClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(700) })
            {
                try { if ((int)(await existingClient.GetAsync($"http://127.0.0.1:{Port}/health")).StatusCode == 200) { CreateStartupShortcut(); Process.Start(new ProcessStartInfo($"http://localhost:{Port}") { UseShellExecute = true }); Close(); return; } } catch { }
            }
            var payload = Payload;
            var exe = Path.Combine(payload, "Hawdh.Portal.exe");
            if (!File.Exists(exe)) throw new InvalidOperationException("ملفات المنصة غير موجودة. أعد تثبيت الحزمة.");
            Directory.CreateDirectory(Path.Combine(DataRoot, "keys"));
            Directory.CreateDirectory(Path.Combine(DataRoot, "backups"));
            var needsSetup = !File.Exists(Path.Combine(DataRoot, ".setup-complete"))
                && !File.Exists(Path.Combine(DataRoot, "hawdh.local.db"));
            if (needsSetup)
            {
                var address = email.Text.Trim();
                try { _ = new MailAddress(address); }
                catch { throw new InvalidOperationException(french ? "Saisissez une adresse e-mail valide." : "أدخل بريدًا إلكترونيًا صحيحًا."); }
                if (password.Text.Length < 8)
                    throw new InvalidOperationException(french ? "Le mot de passe doit contenir au moins 8 caractères." : "كلمة المرور يجب أن تحتوي على 8 أحرف على الأقل.");
            }
            WriteSettings();
            var logDir = DataRoot;
            Directory.CreateDirectory(logDir);
            var psi = new ProcessStartInfo(exe) { WorkingDirectory = payload, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            psi.Environment["DOTNET_ENVIRONMENT"] = "Local";
            psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Local";
            // Local deployment is intentionally loopback-only; it must not expose the admin UI to the LAN.
            psi.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{Port}";
            if (needsSetup) { psi.Environment["SeedAdmin__Email"] = email.Text.Trim(); psi.Environment["SeedAdmin__Password"] = password.Text; }
            server = Process.Start(psi) ?? throw new InvalidOperationException("تعذر تشغيل خادم المنصة.");
            _ = PumpLogAsync(server.StandardOutput, Path.Combine(logDir, "hawdh-server.log"));
            _ = PumpLogAsync(server.StandardError, Path.Combine(logDir, "hawdh-server-error.log"));
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            for (var i = 0; i < 30; i++)
            {
                await Task.Delay(500);
                if (server.HasExited)
                {
                    var error = ReadTail(Path.Combine(logDir, "hawdh-server-error.log"));
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "توقف خادم المنصة أثناء التشغيل." : $"توقف خادم المنصة أثناء التشغيل:\n{error}");
                }
                try { if ((int)(await client.GetAsync($"http://127.0.0.1:{Port}/health")).StatusCode == 200) { File.WriteAllText(Path.Combine(DataRoot, ".setup-complete"), DateTimeOffset.UtcNow.ToString("O")); CreateStartupShortcut(); Process.Start(new ProcessStartInfo($"http://localhost:{Port}") { UseShellExecute = true }); Close(); return; } } catch { }
            }
            throw new InvalidOperationException("لم يستجب خادم المنصة. أعد تشغيل البرنامج.");
        }
        catch (Exception ex)
        {
            // A failed readiness check must not leave a zombie server holding
            // port 5182 and blocking the next retry.
            try
            {
                if (server is { HasExited: false })
                {
                    server.Kill(true);
                    server.WaitForExit(3000);
                }
            }
            catch { }
            server?.Dispose();
            server = null;
            isStarting = false;
            status.Text = french ? "Impossible de démarrer la plateforme" : "تعذر تشغيل المنصة";
            MessageBox.Show(ex.Message, french ? "AGIRE" : "AGIRE", MessageBoxButtons.OK, MessageBoxIcon.Error);
            startButton.Enabled = true;
        }
    }

    private void CreateStartupShortcut()
    {
        try
        {
            var startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            if (string.IsNullOrWhiteSpace(startup) || string.IsNullOrWhiteSpace(Environment.ProcessPath)) return;
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null) return;
            dynamic shell = Activator.CreateInstance(shellType)!;
            foreach (var link in new[]
            {
                Path.Combine(startup, "AGIRE.lnk"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "AGIRE.lnk")
            })
            {
                dynamic shortcut = shell.CreateShortcut(link);
                shortcut.TargetPath = Environment.ProcessPath;
                shortcut.IconLocation = $"{Environment.ProcessPath},0";
                shortcut.WorkingDirectory = root;
                shortcut.Description = "تشغيل AGIRE";
                shortcut.Save();
            }
        }
        catch
        {
            // Startup shortcut is optional; it must never prevent the platform
            // from opening when Windows policy blocks shortcut creation.
        }
    }

    private static async Task PumpLogAsync(StreamReader reader, string path)
    {
        await using var file = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        await using var writer = new StreamWriter(file);
        while (await reader.ReadLineAsync() is { } line) { await writer.WriteLineAsync(line); await writer.FlushAsync(); }
    }

    private static string ReadTail(string path)
    {
        if (!File.Exists(path)) return string.Empty;
        var lines = File.ReadAllLines(path);
        return string.Join(Environment.NewLine, lines.Skip(Math.Max(0, lines.Length - 8)));
    }

    private void WriteSettings()
    {
        var config = new
        {
            ConnectionStrings = new { DefaultConnection = "Data Source=data/hawdh.local.db;Cache=Shared;Pooling=True" },
            Database = new { Provider = "Sqlite" },
            DataProtection = new { KeysPath = "data/keys" },
            Logging = new { LogLevel = new { Default = "Warning", Microsoft = "Warning", MicrosoftAspNetCore = "Warning" } },
            AllowedHosts = "localhost;127.0.0.1"
        };
        File.WriteAllText(Path.Combine(Payload, "appsettings.Local.json"), JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
    }
}
