/*
Copyright (C) 2016 by Marius Becker <marius.becker.8@gmail.com>

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
    using ThoNohT.NohBoard.Extra;

    public partial class RectangleBoundaryForm : Form
    {
        public event Action<TRectangle> DimensionsSet;

        public RectangleBoundaryForm()
        {
            InitializeComponent();
            this.ApplyLocalizedUiTexts();
        }

        public RectangleBoundaryForm(TRectangle rectangle)
        {
            InitializeComponent();
            this.ApplyLocalizedUiTexts();

            var position = rectangle.Position;
            this.txtPosition.X = position.X;
            this.txtPosition.Y = position.Y;

            var size = rectangle.Size;
            this.txtSize.X = size.Width;
            this.txtSize.Y = size.Height;
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            var position = new TPoint(txtPosition.X, txtPosition.Y);
            var size = new TPoint(txtSize.X, txtSize.Y);
            var rectangle = new TRectangle(position, size);
            this.DimensionsSet?.Invoke(rectangle);

            this.DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void ApplyLocalizedUiTexts()
        {
            this.Text = PropertyDialogsLocalization.RectangleBoundaryTitle;
            this.lblPosition.Text = PropertyDialogsLocalization.PositionLabel;
            this.lblSize.Text = PropertyDialogsLocalization.SizeLabel;
            this.btnCancel.Text = PropertyDialogsLocalization.Cancel;
            this.btnApply.Text = PropertyDialogsLocalization.Apply;
        }
    }
}
