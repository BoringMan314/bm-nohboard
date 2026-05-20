/*
Copyright (C) 2017 by Eric Bataille <e.c.p.bataille@gmail.com>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Drawing;
using ThoNohT.NohBoard.Keyboard;

namespace ThoNohT.NohBoard.Forms.Properties
{
    using System;
    using System.Windows.Forms;
    using ThoNohT.NohBoard.Controls;
    using ThoNohT.NohBoard.Extra;

    public partial class KeyboardPropertiesForm : Form
    {
        #region Fields

        private readonly KeyboardDefinition initialDefinition;

        private KeyboardDefinition currentDefinition;

        #endregion Fields

        #region Events

        public event Action<KeyboardDefinition> DefinitionChanged;

        public event Action DefinitionSaved;

        #endregion Events

        public KeyboardPropertiesForm(KeyboardDefinition initialDefinition)
        {
            this.initialDefinition = initialDefinition ?? throw new ArgumentNullException(nameof(initialDefinition));
            this.currentDefinition = this.initialDefinition.Clone();
            this.InitializeComponent();
        }

        private void KeyboardPropertiesForm_Load(object sender, EventArgs e)
        {
            this.ApplyLocalizedUiTexts();

            this.txtSize.X = this.initialDefinition.Width;
            this.txtSize.Y = this.initialDefinition.Height;

            this.txtSize.ValueChanged += this.txtSize_ValueChanged;
        }

        private void txtSize_ValueChanged(VectorTextBox sender, TPoint newSize)
        {
            this.currentDefinition = this.currentDefinition.Resize(new Size(newSize.X, newSize.Y));

            if (newSize.X >= 25 && newSize.Y >= 25 && newSize.X <= 4096 && newSize.Y <= 4096)
                this.DefinitionChanged?.Invoke(this.currentDefinition);
        }

        private void AcceptButton2_Click(object sender, EventArgs e)
        {
            this.DefinitionSaved?.Invoke();
            this.DialogResult = DialogResult.OK;
        }

        private void CancelButton2_Click(object sender, EventArgs e)
        {
            this.DefinitionChanged?.Invoke(this.initialDefinition);
            this.DialogResult = DialogResult.Cancel;
        }

        private void ApplyLocalizedUiTexts()
        {
            this.Text = PropertyDialogsLocalization.KeyboardPropertiesTitle;
            this.lblSize.Text = PropertyDialogsLocalization.SizeLabel;
            this.CancelButton2.Text = PropertyDialogsLocalization.Cancel;
            this.AcceptButton2.Text = PropertyDialogsLocalization.Accept;
        }
    }
}
