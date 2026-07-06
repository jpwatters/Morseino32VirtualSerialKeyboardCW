using System.Drawing;
using System.Windows.Forms;

namespace MorserinoWinKeyboard
{
    /// <summary>
    /// A minimal "type a value, then Save or Cancel" dialog -- the Windows
    /// counterpart of the Mac app's NSAlert-with-accessory-text-field used
    /// in AppDelegate's -renameQuickMacro:. WinForms has no built-in
    /// input-box control, so this is a small reusable Form instead of
    /// pulling in an extra dependency for one dialog.
    /// </summary>
    internal sealed class PromptForm : Form
    {
        private readonly TextBox _textBox;

        public string InputText => _textBox.Text;

        public PromptForm(string title, string message, string initialValue)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(320, 118);

            Label label = new Label
            {
                Text = message,
                AutoSize = true,
                Location = new Point(12, 12),
                MaximumSize = new Size(296, 0),
            };

            _textBox = new TextBox
            {
                Text = initialValue,
                Location = new Point(12, 40),
                Width = 296,
            };

            Button saveButton = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                Location = new Point(152, 74),
                Width = 75,
            };

            Button cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(233, 74),
                Width = 75,
            };

            Controls.Add(label);
            Controls.Add(_textBox);
            Controls.Add(saveButton);
            Controls.Add(cancelButton);

            AcceptButton = saveButton;
            CancelButton = cancelButton;

            Shown += (_, _) =>
            {
                _textBox.SelectAll();
                _textBox.Focus();
            };
        }
    }
}
