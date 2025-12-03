namespace SVG_Editor
{
    /// <summary>
    /// Диалоговая форма создания нового холста.
    /// Позволяет пользователю задать ширину и высоту холста перед началом рисования.
    /// </summary>
    public partial class NewCanvasForm : Form
    {
        private NumericUpDown _numW = null!;
        private NumericUpDown _numH = null!;

        /// <summary>
        /// Текущий выбранный размер холста.
        /// Берётся из значений числовых полей ширины и высоты.
        /// </summary>
        public SizeF CanvasSize => new SizeF((float)_numW.Value, (float)_numH.Value);

        public NewCanvasForm(SizeF current)
        {
            Text = "Новый холст";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(250, 140);

            var lblW = new Label { Text = "Ширина:", Left = 10, Top = 20, Width = 80 };
            var lblH = new Label { Text = "Высота:", Left = 10, Top = 50, Width = 80 };

            _numW = new NumericUpDown { Left = 100, Top = 18, Width = 120, Minimum = 100, Maximum = 10000, Value = (decimal)current.Width };
            _numH = new NumericUpDown { Left = 100, Top = 48, Width = 120, Minimum = 100, Maximum = 10000, Value = (decimal)current.Height };

            var btnOk = new Button { Text = "ОК", DialogResult = DialogResult.OK, Left = 60, Top = 90, Width = 70 };
            var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Left = 140, Top = 90, Width = 70 };

            Controls.AddRange(new Control[] { lblW, lblH, _numW, _numH, btnOk, btnCancel });

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }
}
