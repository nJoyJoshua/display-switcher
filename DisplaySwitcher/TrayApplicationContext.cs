using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DisplaySwitcher
{
    /// <summary>
    /// The main application context. Runs as a system tray icon with no visible window.
    /// Right-click → switch profiles / configure.
    /// </summary>
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon = null!;
        private AppSettings _settings = null!;
        private ContextMenuStrip? _menu;
        private readonly SynchronizationContext _syncContext;

        public TrayApplicationContext()
        {
            _syncContext = SynchronizationContext.Current ?? new System.Windows.Forms.WindowsFormsSynchronizationContext();
            _settings = SettingsManager.Load();
            _trayIcon = new NotifyIcon
            {
                Visible = true,
                Text = "DisplaySwitcher",
                Icon = CreateTrayIcon(Color.FromArgb(0, 120, 215))
            };

            _trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                    ShowContextMenu();
            };

            // First launch: guide user to set up profiles
            if (_settings.Profiles.Count == 0)
            {
                ShowBalloon("DisplaySwitcher gestartet",
                    "Rechtsklick auf das Icon um Profile einzurichten.", ToolTipIcon.Info);
            }
        }

        private void ShowContextMenu()
        {
            _menu?.Dispose();
            _menu = new ContextMenuStrip();
            _menu.BackColor = Color.FromArgb(30, 30, 35);
            _menu.ForeColor = Color.White;
            _menu.Font = new Font("Segoe UI", 10f);
            _menu.Renderer = new DarkMenuRenderer();
            _menu.ShowImageMargin = false;
            _menu.Padding = new Padding(2, 6, 2, 6);

            // Header (non-clickable)
            var header = new ToolStripLabel("  🖥  DisplaySwitcher")
            {
                Font = new Font("Segoe UI Semibold", 10f),
                ForeColor = Color.FromArgb(120, 200, 255),
                Enabled = false
            };
            _menu.Items.Add(header);
            _menu.Items.Add(new ToolStripSeparator());

            // Profile switch items
            if (_settings.Profiles.Count == 0)
            {
                var noProfiles = new ToolStripMenuItem("Noch keine Profile konfiguriert")
                { Enabled = false, ForeColor = Color.Gray };
                _menu.Items.Add(noProfiles);
            }
            else
            {
                for (int i = 0; i < _settings.Profiles.Count; i++)
                {
                    var profile = _settings.Profiles[i];
                    int capturedIndex = i;
                    bool isActive = _settings.ActiveProfileIndex == i;

                    string monitorNames = string.Join(", ",
                        profile.Monitors.Select(m => m.FriendlyName.Split('(')[0].Trim()));
                    string audioHint = profile.AudioDeviceName != null
                        ? $"  🔊 {profile.AudioDeviceName}" : "";
                    string audioInputHint = profile.AudioInputDeviceName != null
                        ? $"  🎤 {profile.AudioInputDeviceName}" : "";

                    var item = new ToolStripMenuItem(
                        $"{(isActive ? "✔ " : "   ")}{profile.Name}")
                    {
                        ToolTipText = $"Monitore: {monitorNames}{audioHint}{audioInputHint}",
                        Font = isActive
                            ? new Font("Segoe UI Semibold", 10f)
                            : new Font("Segoe UI", 10f),
                        ForeColor = isActive
                            ? Color.FromArgb(120, 220, 120)
                            : Color.White
                    };
                    item.Click += (s, e) => ApplyProfile(capturedIndex);
                    _menu.Items.Add(item);
                }
            }

            _menu.Items.Add(new ToolStripSeparator());

            // Configure profiles submenu
            var configMenu = new ToolStripMenuItem("⚙  Profile konfigurieren");
            configMenu.ForeColor = Color.FromArgb(200, 200, 215);

            // Edit existing profiles
            if (_settings.Profiles.Count > 0)
            {
                foreach (var p in _settings.Profiles)
                {
                    var prof = p;
                    var editItem = new ToolStripMenuItem($"✏  {prof.Name} bearbeiten")
                    { ForeColor = Color.White };
                    editItem.Click += (s, e) => EditProfile(prof);
                    configMenu.DropDownItems.Add(editItem);
                }
                configMenu.DropDownItems.Add(new ToolStripSeparator());
            }

            // Add new profile
            var addItem = new ToolStripMenuItem("➕  Neues Profil erstellen")
            { ForeColor = Color.White };
            addItem.Click += (s, e) => CreateNewProfile();
            configMenu.DropDownItems.Add(addItem);

            // Remove profile
            if (_settings.Profiles.Count > 0)
            {
                var removeMenu = new ToolStripMenuItem("🗑  Profil entfernen")
                { ForeColor = Color.White };
                foreach (var p in _settings.Profiles)
                {
                    var prof = p;
                    var ri = new ToolStripMenuItem(prof.Name)
                    { ForeColor = Color.White };
                    ri.Click += (s, e) => RemoveProfile(prof);
                    removeMenu.DropDownItems.Add(ri);
                }
                ApplyDarkDropDown(removeMenu);
                configMenu.DropDownItems.Add(removeMenu);
            }

            ApplyDarkDropDown(configMenu);
            _menu.Items.Add(configMenu);
            _menu.Items.Add(new ToolStripSeparator());

            // Autostart toggle
            bool autostart = _settings.StartWithWindows;
            var autostartItem = new ToolStripMenuItem(
                $"{(autostart ? "✔ " : "   ")}Autostart mit Windows")
            {
                ForeColor = autostart ? Color.FromArgb(120, 220, 120) : Color.FromArgb(200, 200, 215)
            };
            autostartItem.Click += (s, e) => ToggleAutostart();
            _menu.Items.Add(autostartItem);

            // Open settings folder
            var folderItem = new ToolStripMenuItem("📁  Einstellungsordner öffnen");
            folderItem.Click += (s, e) => System.Diagnostics.Process.Start("explorer.exe",
                SettingsManager.SettingsDirectory);
            _menu.Items.Add(folderItem);

            _menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("✕  Beenden");
            exitItem.Click += (s, e) => ExitApp();
            _menu.Items.Add(exitItem);

            // Show at tray position using proper Win32 focus management
            _trayIcon.ContextMenuStrip = _menu;
            var method = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(_trayIcon, null);
        }

        private void ApplyProfile(int profileIndex)
        {
            var profile = _settings.Profiles[profileIndex];

            _trayIcon.Icon = CreateTrayIcon(Color.FromArgb(255, 160, 0)); // orange = working
            _trayIcon.Text = $"DisplaySwitcher – wechsle zu {profile.Name}...";

            bool monOk = DisplayConfig.ApplyProfile(profile);

            if (profile.AudioDeviceId != null)
                AudioManager.SetDefaultPlaybackDevice(profile.AudioDeviceId);

            if (profile.AudioInputDeviceId != null)
                AudioManager.SetDefaultRecordingDevice(profile.AudioInputDeviceId);

            _settings.ActiveProfileIndex = profileIndex;
            SettingsManager.Save(_settings);

            Color iconColor = monOk
                ? Color.FromArgb(0, 200, 80)
                : Color.FromArgb(200, 60, 60);

            _trayIcon.Icon = CreateTrayIcon(iconColor);
            _trayIcon.Text = $"DisplaySwitcher – {profile.Name}";

            string msg = monOk
                ? $"Profil \"{profile.Name}\" aktiviert."
                : $"Profil \"{profile.Name}\" angewendet (Monitore: Fehler aufgetreten).";
            if (profile.AudioDeviceName != null)
                msg += $"\n🔊 Ausgabe: {profile.AudioDeviceName}";
            if (profile.AudioInputDeviceName != null)
                msg += $"\n🎤 Eingabe: {profile.AudioInputDeviceName}";

            ShowBalloon(monOk ? "Profil gewechselt" : "Teilweise erfolgreich", msg,
                monOk ? ToolTipIcon.Info : ToolTipIcon.Warning);

            // Reset icon to blue after 3s
            var t = new System.Threading.Timer(_ =>
            {
                _syncContext.Post(__ =>
                {
                    if (_trayIcon != null)
                        _trayIcon.Icon = CreateTrayIcon(Color.FromArgb(0, 120, 215));
                }, null);
            }, null, 3000, System.Threading.Timeout.Infinite);
        }

        private void CreateNewProfile()
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox(
                "Name für das neue Profil:", "Neues Profil", $"Profil {_settings.Profiles.Count + 1}");

            if (string.IsNullOrWhiteSpace(name)) return;

            using var editor = new ProfileEditorForm(name);
            if (editor.ShowDialog() == DialogResult.OK && editor.ResultProfile != null)
            {
                _settings.Profiles.Add(editor.ResultProfile);
                SettingsManager.Save(_settings);
                ShowBalloon("Profil erstellt", $"\"{name}\" wurde gespeichert.", ToolTipIcon.Info);
            }
        }

        private void EditProfile(DisplayProfile profile)
        {
            using var editor = new ProfileEditorForm(profile.Name, profile);
            if (editor.ShowDialog() == DialogResult.OK && editor.ResultProfile != null)
            {
                int idx = _settings.Profiles.IndexOf(profile);
                _settings.Profiles[idx] = editor.ResultProfile;
                SettingsManager.Save(_settings);
                ShowBalloon("Profil aktualisiert", $"\"{profile.Name}\" wurde gespeichert.", ToolTipIcon.Info);
            }
        }

        private void RemoveProfile(DisplayProfile profile)
        {
            var confirm = MessageBox.Show(
                $"Profil \"{profile.Name}\" wirklich entfernen?",
                "Bestätigung", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                _settings.Profiles.Remove(profile);
                if (_settings.ActiveProfileIndex >= _settings.Profiles.Count)
                    _settings.ActiveProfileIndex = -1;
                SettingsManager.Save(_settings);
            }
        }

        private void ToggleAutostart()
        {
            _settings.StartWithWindows = !_settings.StartWithWindows;
            SettingsManager.Save(_settings);

            const string regKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            const string appName = "DisplaySwitcher";
            using var key = Registry.CurrentUser.OpenSubKey(regKey, true);

            if (_settings.StartWithWindows)
            {
                string exePath = Environment.ProcessPath
                    ?? System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
                key?.SetValue(appName, $"\"{exePath}\"");
                ShowBalloon("Autostart aktiviert", "DisplaySwitcher startet mit Windows.", ToolTipIcon.Info);
            }
            else
            {
                key?.DeleteValue(appName, false);
                ShowBalloon("Autostart deaktiviert", "DisplaySwitcher startet nicht mehr automatisch.", ToolTipIcon.Info);
            }
        }

        /// <summary>Apply dark theme to a submenu's DropDown.</summary>
        private void ApplyDarkDropDown(ToolStripMenuItem item)
        {
            item.DropDown.BackColor = Color.FromArgb(30, 30, 35);
            item.DropDown.ForeColor = Color.White;
            if (item.DropDown is ToolStripDropDownMenu ddm)
            {
                ddm.Renderer = new DarkMenuRenderer();
                ddm.ShowImageMargin = false;
            }
        }

        private void ExitApp()
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Application.Exit();
        }

        private void ShowBalloon(string title, string text, ToolTipIcon icon)
        {
            _trayIcon.BalloonTipTitle = title;
            _trayIcon.BalloonTipText = text;
            _trayIcon.BalloonTipIcon = icon;
            _trayIcon.ShowBalloonTip(3500);
        }

        /// <summary>
        /// Generates a crisp monitor icon for the tray in the given color.
        /// </summary>
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private static Icon CreateTrayIcon(Color color)
        {
            using var bmp = new Bitmap(32, 32);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var pen = new Pen(color, 2.5f);
            using var fillBrush = new SolidBrush(Color.FromArgb(60, color.R, color.G, color.B));
            using var dotBrush = new SolidBrush(color);

            // Monitor outline
            g.DrawRectangle(pen, 3, 4, 22, 16);
            // Screen fill
            g.FillRectangle(fillBrush, 4, 5, 21, 15);
            // Stand
            g.DrawLine(pen, 14, 20, 14, 25);
            g.DrawLine(pen, 9, 25, 19, 25);
            // Indicator dot
            g.FillEllipse(dotBrush, 25, 4, 6, 6);

            var iconHandle = bmp.GetHicon();
            var icon = (Icon)Icon.FromHandle(iconHandle).Clone();
            DestroyIcon(iconHandle);
            return icon;
        }
    }

    /// <summary>
    /// Custom dark theme renderer for the context menu.
    /// </summary>
    public class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var rect = new Rectangle(4, 1, e.Item.Width - 8, e.Item.Height - 2);
            if (e.Item.Selected && e.Item.Enabled)
            {
                using var path = RoundedRect(rect, 4);
                using var brush = new SolidBrush(Color.FromArgb(0, 90, 160));
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
            }
            else
            {
                using var brush = new SolidBrush(Color.FromArgb(30, 30, 35));
                e.Graphics.FillRectangle(brush, new Rectangle(0, 0, e.Item.Width, e.Item.Height));
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(Color.FromArgb(30, 30, 35));
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Draw a subtle rounded border around the menu
            var rect = new Rectangle(0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
            using var pen = new Pen(Color.FromArgb(60, 60, 75));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(rect, 6);
            e.Graphics.DrawPath(pen, path);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.ForeColor == SystemColors.ControlText
                ? Color.White
                : e.Item.ForeColor;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Color.FromArgb(180, 180, 200);
            base.OnRenderArrow(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using var pen = new Pen(Color.FromArgb(55, 55, 68));
            e.Graphics.DrawLine(pen, 12, y, e.Item.Width - 12, y);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder => Color.FromArgb(60, 60, 75);
        public override Color MenuItemBorder => Color.Transparent;
        public override Color ToolStripDropDownBackground => Color.FromArgb(30, 30, 35);
        public override Color ImageMarginGradientBegin => Color.FromArgb(30, 30, 35);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(30, 30, 35);
        public override Color ImageMarginGradientEnd => Color.FromArgb(30, 30, 35);
        public override Color MenuItemSelected => Color.FromArgb(0, 90, 160);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(0, 90, 160);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(0, 90, 160);
    }
}
