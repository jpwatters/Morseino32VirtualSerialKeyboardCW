using System;
using System.Drawing;
using System.Windows.Forms;

namespace MorserinoWinKeyboard
{
    /// <summary>
    /// Windows counterpart of the Mac app's AppDelegate + MainMenu.xib:
    /// a device picker, a live compose field, ENTER/DELETE buttons, and
    /// sixteen renamable quick-macro buttons (a 10-button grid plus six
    /// more below), all driving a SerialKeyboard transport that speaks the
    /// Morserino-32's M32 protocol.
    /// </summary>
    public sealed class MainForm : Form
    {
        // Same default labels as the Mac app's quick-macro grid (top 10) and
        // its six originally-preset buttons (bottom 6).
        private static readonly string[] DefaultQuickMacroTitles =
        {
            "CQ", "CQ DX", "DX DE ME", "X1XX", "RIG ANT",
            "WX", "QRL?", "BK", "73 TU EE", "TNX FER...",
            "Preset 1", "Preset 2", "Preset 3", "Preset 4", "Preset 5", "Preset 6",
        };

        private readonly SerialKeyboard _transport = new SerialKeyboard();
        private readonly QuickMacroStore _macroStore = new QuickMacroStore();
        private readonly Button[] _quickMacroButtons = new Button[16];
        private readonly System.Windows.Forms.Timer _statusResetTimer = new System.Windows.Forms.Timer();

        private ComboBox _portCombo;
        private Button _refreshButton;
        private Button _connectButton;
        private Label _statusLabel;
        private TextBox _composeField;
        private Button _enterButton;
        private Button _deleteButton;

        public MainForm()
        {
            Text = "Morserino32 Serial Keyboard";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ClientSize = new Size(500, 520);
            StartPosition = FormStartPosition.CenterScreen;

            BuildDeviceRow();
            BuildQuickMacroGrid();
            BuildComposeRow();
            BuildEnterDeleteRow();
            BuildPresetButtons();
            SetUpQuickMacroButtons();
            SetFieldsEnabled(false);
            RefreshPorts();

            _statusResetTimer.Interval = 2500;
            _statusResetTimer.Tick += (_, _) =>
            {
                _statusResetTimer.Stop();
                UpdateStatusLabel();
            };

            _transport.Connected += (_, e) =>
            {
                SetFieldsEnabled(true);
                _connectButton.Text = "Disconnect";
                UpdateStatusLabel();
            };
            _transport.Disconnected += (_, _) =>
            {
                SetFieldsEnabled(false);
                _connectButton.Text = "Connect";
                UpdateStatusLabel();
            };
            _transport.Failed += (_, e) =>
            {
                ShowTransientStatus(e.Message ?? "Connection failed");
            };

            FormClosing += (_, _) => _transport.Disconnect();
        }

        // ------------------------------------------------------------------
        // Layout
        // ------------------------------------------------------------------

        private void BuildDeviceRow()
        {
            Label deviceLabel = new Label
            {
                Text = "Device:",
                AutoSize = true,
                Location = new Point(12, 18),
            };

            _portCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(70, 14),
                Width = 230,
            };

            _refreshButton = new Button
            {
                Text = "Refresh",
                Location = new Point(308, 13),
                Width = 70,
                Height = 25,
            };
            _refreshButton.Click += (_, _) => RefreshPorts();

            _connectButton = new Button
            {
                Text = "Connect",
                Location = new Point(383, 13),
                Width = 90,
                Height = 25,
            };
            _connectButton.Click += ConnectButton_Click;

            _statusLabel = new Label
            {
                Text = "Not Connected",
                AutoSize = false,
                Location = new Point(12, 46),
                Size = new Size(461, 18),
                ForeColor = Color.DimGray,
            };

            Controls.Add(deviceLabel);
            Controls.Add(_portCombo);
            Controls.Add(_refreshButton);
            Controls.Add(_connectButton);
            Controls.Add(_statusLabel);
        }

        private void BuildQuickMacroGrid()
        {
            const int left = 12;
            const int top = 80;
            const int buttonWidth = 90;
            const int buttonHeight = 32;
            const int gapX = 6;
            const int gapY = 8;

            for (int i = 0; i < 10; i++)
            {
                int col = i % 5;
                int row = i / 5;
                int x = left + col * (buttonWidth + gapX);
                int y = top + row * (buttonHeight + gapY);

                Button button = new Button
                {
                    Tag = i,
                    Text = DefaultQuickMacroTitles[i],
                    Location = new Point(x, y),
                    Size = new Size(buttonWidth, buttonHeight),
                };
                button.Click += QuickMacroButton_Click;
                AttachRenameMenu(button);

                _quickMacroButtons[i] = button;
                Controls.Add(button);
            }
        }

        private void BuildComposeRow()
        {
            _composeField = new TextBox
            {
                Location = new Point(12, 165),
                Size = new Size(476, 26),
            };
            _composeField.KeyDown += ComposeField_KeyDown;
            Controls.Add(_composeField);
        }

        private void BuildEnterDeleteRow()
        {
            _enterButton = new Button
            {
                Text = "ENTER",
                Location = new Point(12, 200),
                Size = new Size(234, 32),
            };
            _enterButton.Click += (_, _) => _transport.SendEnter();

            _deleteButton = new Button
            {
                Text = "DELETE",
                Location = new Point(254, 200),
                Size = new Size(234, 32),
            };
            _deleteButton.Click += (_, _) => _transport.SendDelete();

            Controls.Add(_enterButton);
            Controls.Add(_deleteButton);
        }

        private void BuildPresetButtons()
        {
            const int left = 12;
            const int top = 244;
            const int width = 476;
            const int height = 32;
            const int gapY = 8;

            for (int i = 0; i < 6; i++)
            {
                int tag = 10 + i;
                int y = top + i * (height + gapY);

                Button button = new Button
                {
                    Tag = tag,
                    Text = DefaultQuickMacroTitles[tag],
                    Location = new Point(left, y),
                    Size = new Size(width, height),
                };
                button.Click += QuickMacroButton_Click;
                AttachRenameMenu(button);

                _quickMacroButtons[tag] = button;
                Controls.Add(button);
            }
        }

        // ------------------------------------------------------------------
        // Device connection
        // ------------------------------------------------------------------

        private void RefreshPorts()
        {
            string previouslySelected = _portCombo.SelectedItem as string;

            _portCombo.Items.Clear();
            string[] ports = SerialKeyboard.AvailablePortNames();
            foreach (string port in ports)
            {
                _portCombo.Items.Add(port);
            }

            if (ports.Length == 0)
            {
                return;
            }

            int indexToSelect = Array.IndexOf(ports, previouslySelected);
            _portCombo.SelectedIndex = indexToSelect >= 0 ? indexToSelect : 0;
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            if (_transport.IsConnected)
            {
                _transport.Disconnect();
                return;
            }

            if (_portCombo.SelectedItem is not string portName)
            {
                ShowTransientStatus("No serial devices found");
                return;
            }

            if (!_transport.Connect(portName, out string error))
            {
                ShowTransientStatus(error ?? "Couldn't open serial port");
            }
        }

        private void UpdateStatusLabel()
        {
            _statusLabel.Text = _transport.IsConnected
                ? "Connected: " + _transport.ConnectedDeviceName
                : "Not Connected";
            _statusLabel.ForeColor = _transport.IsConnected ? Color.DarkGreen : Color.DimGray;
        }

        private void ShowTransientStatus(string message)
        {
            _statusLabel.Text = message;
            _statusLabel.ForeColor = Color.Firebrick;
            _statusResetTimer.Stop();
            _statusResetTimer.Start();
        }

        // ------------------------------------------------------------------
        // Compose field: Enter sends the whole current line
        // ------------------------------------------------------------------

        private void ComposeField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.SuppressKeyPress = true;
            e.Handled = true;

            string text = _composeField.Text;
            if (text.Length > 0)
            {
                _transport.SendString(text);
                _composeField.Clear();
            }
        }

        // ------------------------------------------------------------------
        // Quick-macro buttons: send on click, rename via right-click menu
        // ------------------------------------------------------------------

        private void SetFieldsEnabled(bool enabled)
        {
            _composeField.Enabled = enabled;
            _enterButton.Enabled = enabled;
            _deleteButton.Enabled = enabled;
            foreach (Button button in _quickMacroButtons)
            {
                button.Enabled = enabled;
            }
        }

        private void SetUpQuickMacroButtons()
        {
            foreach (Button button in _quickMacroButtons)
            {
                int tag = (int)button.Tag;
                string savedTitle = _macroStore.GetTitle(tag);
                if (!string.IsNullOrEmpty(savedTitle))
                {
                    button.Text = savedTitle;
                }
            }
        }

        private void AttachRenameMenu(Button button)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem renameItem = new ToolStripMenuItem("Rename...");
            renameItem.Click += (_, _) => RenameQuickMacro(button);
            menu.Items.Add(renameItem);
            button.ContextMenuStrip = menu;
        }

        private void QuickMacroButton_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            if (button.Text.Length > 0)
            {
                _transport.SendString(button.Text);
            }
        }

        private void RenameQuickMacro(Button button)
        {
            using PromptForm prompt = new PromptForm(
                "Rename Quick-Send Button",
                "This text is sent when the button is clicked.",
                button.Text);

            if (prompt.ShowDialog(this) == DialogResult.OK && prompt.InputText.Length > 0)
            {
                button.Text = prompt.InputText;
                int tag = (int)button.Tag;
                if (!_macroStore.SetTitle(tag, prompt.InputText))
                {
                    MessageBox.Show(
                        this,
                        "Couldn't save this label to QuickMacroButtons.json next to the "
                        + "application (read-only install location?). The new label will "
                        + "work for this session but won't persist after you restart the app.",
                        "Couldn't Save Label",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }
    }
}
