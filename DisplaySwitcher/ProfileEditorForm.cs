using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DisplaySwitcher
{
    /// <summary>
    /// Wizard-style form for creating or editing a display profile.
    /// Lets the user pick active monitors and audio device.
    /// </summary>
    public class ProfileEditorForm : Form
    {
        private readonly string _profileName;
        private readonly DisplayProfile? _existing;

        // UI Controls
        private Label _lblTitle = null!;
        private Label _lblMonitors = null!;
        private CheckedListBox _clbMonitors = null!;
        private Label _lblAudio = null!;
        private ComboBox _cmbAudio = null!;
        private Label _lblAudioInput = null!;
        private ComboBox _cmbAudioInput = null!;
        private Button _btnRefresh = null!;
        private Button _btnOk = null!;
        private Button _btnCancel = null!;
        private Label _lblHint = null!;

        private List<MonitorInfo> _monitors = new();
        private List<AudioManager.AudioDevice> _audioDevices = new();
        private List<AudioManager.AudioDevice> _audioInputDevices = new();
        private bool _suppressClose = false;

        public DisplayProfile? ResultProfile { get; private set; }

        public ProfileEditorForm(string profileName, DisplayProfile? existing = null)
        {
            _profileName = profileName;
            _existing = existing;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = $"Profil konfigurieren – {_profileName}";
            Size = new Size(520, 530);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(30, 30, 35);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10f);

            _lblTitle = new Label
            {
                Text = $"⚙ Profil: {_profileName}",
                Location = new Point(20, 15),
                Size = new Size(460, 28),
                Font = new Font("Segoe UI Semibold", 13f),
                ForeColor = Color.FromArgb(120, 200, 255)
            };

            _lblMonitors = new Label
            {
                Text = "Aktive Monitore in diesem Profil:",
                Location = new Point(20, 55),
                Size = new Size(460, 22),
                ForeColor = Color.FromArgb(200, 200, 210)
            };

            _clbMonitors = new CheckedListBox
            {
                Location = new Point(20, 80),
                Size = new Size(460, 160),
                BackColor = Color.FromArgb(45, 45, 52),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f),
                CheckOnClick = true
            };

            _lblHint = new Label
            {
                Text = "💡 Tipp: Erkenne deine Monitore über Windows-Einstellungen → Anzeige → Identifizieren",
                Location = new Point(20, 248),
                Size = new Size(460, 35),
                ForeColor = Color.FromArgb(150, 150, 160),
                Font = new Font("Segoe UI", 8.5f)
            };

            _lblAudio = new Label
            {
                Text = "Ausgabegerät (Lautsprecher/Kopfhörer):",
                Location = new Point(20, 295),
                Size = new Size(460, 22),
                ForeColor = Color.FromArgb(200, 200, 210)
            };

            _cmbAudio = new ComboBox
            {
                Location = new Point(20, 320),
                Size = new Size(460, 30),
                BackColor = Color.FromArgb(45, 45, 52),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            _cmbAudio.DrawItem += CmbDrawItem;

            _lblAudioInput = new Label
            {
                Text = "Eingabegerät (Mikrofon):",
                Location = new Point(20, 362),
                Size = new Size(460, 22),
                ForeColor = Color.FromArgb(200, 200, 210)
            };

            _cmbAudioInput = new ComboBox
            {
                Location = new Point(20, 387),
                Size = new Size(370, 30),
                BackColor = Color.FromArgb(45, 45, 52),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            _cmbAudioInput.DrawItem += CmbDrawItem;

            _btnRefresh = new Button
            {
                Text = "↻",
                Location = new Point(398, 385),
                Size = new Size(82, 32),
                BackColor = Color.FromArgb(55, 55, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12f)
            };
            _btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 95);
            _btnRefresh.Click += (s, e) => LoadData();

            _btnOk = new Button
            {
                Text = "✓  Speichern",
                Location = new Point(350, 458),
                Size = new Size(130, 36),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10f),
                DialogResult = DialogResult.OK
            };
            _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.Click += BtnOk_Click;

            _btnCancel = new Button
            {
                Text = "Abbrechen",
                Location = new Point(210, 458),
                Size = new Size(130, 36),
                BackColor = Color.FromArgb(55, 55, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f),
                DialogResult = DialogResult.Cancel
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 95);

            Controls.AddRange(new Control[]
            {
                _lblTitle, _lblMonitors, _clbMonitors, _lblHint,
                _lblAudio, _cmbAudio,
                _lblAudioInput, _cmbAudioInput,
                _btnRefresh,
                _btnOk, _btnCancel
            });
        }

        private void LoadData()
        {
            // Load monitors
            _clbMonitors.Items.Clear();
            _monitors = DisplayConfig.GetAllMonitors();

            if (_monitors.Count == 0)
            {
                _clbMonitors.Items.Add("⚠ Keine Monitore erkannt – bitte als Administrator ausführen");
            }
            else
            {
                foreach (var mon in _monitors)
                {
                    string label = $"{mon.FriendlyName}";
                    if (!string.IsNullOrEmpty(mon.GdiDeviceName))
                        label += $"  [{mon.GdiDeviceName}]";
                    if (mon.IsActive)
                    {
                        if (mon.Width > 0)
                        {
                            int hz = mon.VSyncFreqD > 0 ? (int)Math.Round((double)mon.VSyncFreqN / mon.VSyncFreqD) : 0;
                            label += $"  {mon.Width}\u00d7{mon.Height} @{hz}Hz";
                            if (mon.IsPrimary) label += " ★Primary";
                        }
                        label += "  (aktiv)";
                    }

                    bool check = _existing != null
                        ? _existing.Monitors.Any(m => m.DevicePath == mon.DevicePath)
                        : mon.IsActive;

                    _clbMonitors.Items.Add(label, check);
                }
            }

            // Load output (playback) devices
            _cmbAudio.Items.Clear();
            _audioDevices = AudioManager.GetPlaybackDevices();
            _cmbAudio.Items.Add("— kein Wechsel —");
            foreach (var dev in _audioDevices)
                _cmbAudio.Items.Add(dev);

            if (_existing?.AudioDeviceId != null)
            {
                var match = _audioDevices.Find(d => d.Id == _existing.AudioDeviceId);
                _cmbAudio.SelectedItem = match ?? _cmbAudio.Items[0]!;
            }
            else
            {
                var defaultId = AudioManager.GetDefaultPlaybackDeviceId();
                var def = defaultId != null ? _audioDevices.Find(d => d.Id == defaultId) : null;
                _cmbAudio.SelectedItem = def ?? _cmbAudio.Items[0]!;
            }

            // Load input (recording) devices
            _cmbAudioInput.Items.Clear();
            _audioInputDevices = AudioManager.GetRecordingDevices();
            _cmbAudioInput.Items.Add("— kein Wechsel —");
            foreach (var dev in _audioInputDevices)
                _cmbAudioInput.Items.Add(dev);

            if (_existing?.AudioInputDeviceId != null)
            {
                var match = _audioInputDevices.Find(d => d.Id == _existing.AudioInputDeviceId);
                _cmbAudioInput.SelectedItem = match ?? _cmbAudioInput.Items[0]!;
            }
            else
            {
                var defaultId = AudioManager.GetDefaultRecordingDeviceId();
                var def = defaultId != null ? _audioInputDevices.Find(d => d.Id == defaultId) : null;
                _cmbAudioInput.SelectedItem = def ?? _cmbAudioInput.Items[0]!;
            }
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            var selectedMonitors = new List<ProfileMonitor>();
            for (int i = 0; i < _clbMonitors.CheckedIndices.Count; i++)
            {
                int idx = _clbMonitors.CheckedIndices[i];
                if (idx < _monitors.Count)
                {
                    var mon = _monitors[idx];
                    selectedMonitors.Add(new ProfileMonitor
                    {
                        FriendlyName = mon.FriendlyName,
                        DevicePath = mon.DevicePath,
                        GdiDeviceName = mon.GdiDeviceName,
                        AdapterIdLow = mon.AdapterId.LowPart,
                        AdapterIdHigh = mon.AdapterId.HighPart,
                        TargetId = mon.TargetId,
                        SourceId = mon.SourceId,
                        Width = mon.Width,
                        Height = mon.Height,
                        PositionX = mon.PositionX,
                        PositionY = mon.PositionY,
                        IsPrimary = mon.IsPrimary,
                        PixelFormat = mon.PixelFormat,
                        PixelRate = mon.PixelRate,
                        HSyncFreqN = mon.HSyncFreqN,
                        HSyncFreqD = mon.HSyncFreqD,
                        VSyncFreqN = mon.VSyncFreqN,
                        VSyncFreqD = mon.VSyncFreqD,
                        ActiveWidth = mon.ActiveWidth,
                        ActiveHeight = mon.ActiveHeight,
                        TotalWidth = mon.TotalWidth,
                        TotalHeight = mon.TotalHeight,
                        VideoStandard = mon.VideoStandard,
                        ScanLineOrdering = mon.ScanLineOrdering
                    });
                }
            }

            if (selectedMonitors.Count == 0)
            {
                _suppressClose = true;
                MessageBox.Show(this, "Bitte mindestens einen Monitor auswählen.", "Hinweis",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _suppressClose = false;
                DialogResult = DialogResult.None;
                return;
            }

            string? audioId = null;
            string? audioName = null;
            if (_cmbAudio.SelectedItem is AudioManager.AudioDevice selAudio)
            {
                audioId = selAudio.Id;
                audioName = selAudio.Name;
            }

            string? audioInputId = null;
            string? audioInputName = null;
            if (_cmbAudioInput.SelectedItem is AudioManager.AudioDevice selInput)
            {
                audioInputId = selInput.Id;
                audioInputName = selInput.Name;
            }

            ResultProfile = new DisplayProfile
            {
                Name = _profileName,
                Monitors = selectedMonitors,
                AudioDeviceId = audioId,
                AudioDeviceName = audioName,
                AudioInputDeviceId = audioInputId,
                AudioInputDeviceName = audioInputName
            };
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_ACTIVATEAPP = 0x001C;
            if (m.Msg == WM_ACTIVATEAPP && m.WParam == IntPtr.Zero && !_suppressClose)
                Close();
            base.WndProc(ref m);
        }

        private void CmbDrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || sender is not ComboBox cmb) return;
            bool isEdit = (e.State & DrawItemState.ComboBoxEdit) != 0;
            bool isSelected = (e.State & DrawItemState.Selected) != 0;
            using var bg = new SolidBrush((isSelected && !isEdit) ? Color.FromArgb(0, 90, 160) : Color.FromArgb(45, 45, 52));
            e.Graphics.FillRectangle(bg, e.Bounds);
            TextRenderer.DrawText(e.Graphics, cmb.Items[e.Index]?.ToString(), e.Font,
                e.Bounds, Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }
}
