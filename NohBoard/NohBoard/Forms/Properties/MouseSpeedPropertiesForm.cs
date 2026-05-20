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

namespace ThoNohT.NohBoard.Forms.Properties
{
    using System;
    using System.Windows.Forms;
    using Keyboard.ElementDefinitions;
    using ThoNohT.NohBoard.Controls;
    using ThoNohT.NohBoard.Extra;

    public partial class MouseSpeedPropertiesForm : Form
    {
        #region Fields

        private readonly MouseSpeedIndicatorDefinition initialDefinition;

        private MouseSpeedIndicatorDefinition currentDefinition;

        #endregion Fields

        #region Events

        public event Action<MouseSpeedIndicatorDefinition> DefinitionChanged;

        public event Action DefinitionSaved;

        #endregion Events

        public MouseSpeedPropertiesForm(MouseSpeedIndicatorDefinition initialDefinition)
        {
            this.initialDefinition = initialDefinition ?? throw new ArgumentNullException(nameof(initialDefinition));
            this.currentDefinition = (MouseSpeedIndicatorDefinition)this.initialDefinition.Clone();
            this.InitializeComponent();
        }

        private void MouseSpeedPropertiesForm_Load(object sender, EventArgs e)
        {
            this.ApplyLocalizedUiTexts();

            this.txtLocation.X = this.initialDefinition.Location.X;
            this.txtLocation.Y = this.initialDefinition.Location.Y;
            this.udRadius.Value = this.initialDefinition.Radius;

            this.txtLocation.ValueChanged += this.txtLocation_ValueChanged;
            this.udRadius.ValueChanged += this.udRadius_ValueChanged;
        }

        private void txtLocation_ValueChanged(VectorTextBox sender, TPoint newLocation)
        {
            var diff = newLocation - this.currentDefinition.Location;
            this.currentDefinition = (MouseSpeedIndicatorDefinition)this.currentDefinition.Translate(diff.Width, diff.Height);
            this.DefinitionChanged?.Invoke(this.currentDefinition);
        }

        private void udRadius_ValueChanged(object sender, EventArgs e)
        {
            this.currentDefinition = this.currentDefinition.SetRadius((int)this.udRadius.Value);
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
            this.Text = PropertyDialogsLocalization.MouseSpeedIndicatorPropertiesTitle;
            this.lblLocation.Text = PropertyDialogsLocalization.LocationLabel;
            this.lblRadius.Text = PropertyDialogsLocalization.RadiusLabel;
            this.CancelButton2.Text = PropertyDialogsLocalization.Cancel;
            this.AcceptButton2.Text = PropertyDialogsLocalization.Accept;
        }
    }
}
