/*
Copyright (C) 2016 by Eric Bataille <e.c.p.bataille@gmail.com>

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

namespace ThoNohT.NohBoard.Controls
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using Extra;

    public class VectorTextBox : TextBox
    {
        #region Events

        public event Action<VectorTextBox, TPoint> ValueChanged;

        #endregion Events

        #region Fields

        private int maxVal = int.MaxValue;
        private int x;
        private int y;
        private bool spacesAroundSeparator = true;
        private char separator = ';';

        #endregion Fields

        #region Properties

        public char Separator
        {
            get { return this.separator; }
            set
            {
                this.separator = value;
                this.UpdateText();
            }
        }

        public bool SpacesAroundSeparator
        {
            get { return this.spacesAroundSeparator; }
            set
            {
                this.spacesAroundSeparator = value;
                this.UpdateText();
            }
        }

        public int X
        {
            get { return this.x; }
            set
            {
                this.x = value;
                this.UpdateText();
            }
        }

        public int Y
        {
            get { return this.y; }
            set
            {
                this.y = value;
                this.UpdateText();
            }
        }

        public int MaxVal
        {
            get { return this.maxVal; }
            set
            {
                this.maxVal = value;
                if (this.X > this.maxVal) this.X = this.maxVal;
                if (this.Y > this.maxVal) this.Y = this.maxVal;
            }
        }

        private string sep => this.SpacesAroundSeparator ? $" {this.Separator} " : this.Separator.ToString();

        #endregion Properties

        #region Constructors

        public VectorTextBox()
        {
            this.TextAlign = HorizontalAlignment.Left;
            this.KeyPress += this.IgnoreKey;
            this.KeyDown += this.HandleKeyDown;
            this.GotFocus += this.HandleFocus;
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            base.SetBoundsCore(x, y, width, FixedHeightTextBox.FixedUiHeight, specified | BoundsSpecified.Height);
        }

        #endregion Constructors

        #region Methods

        private void HandleFocus(object sender, EventArgs e)
        {
            if (this.SelectionLength > 0) return;

            if (this.Text == string.Empty)
                this.Text = $"{this.x}{this.sep}{this.y}";

            var split = this.Text.Split(new[] { this.Separator, ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (this.x == 0 && this.SelectionStart == 0)
                this.SelectionLength = 1;

            if (this.y == 0 && this.SelectionStart == split[0].Length + this.sep.Length)
                this.SelectionLength = 1;
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            var key = e.KeyCode;
            if (key == Keys.Oem1) key = (Keys)';';
            var newChar = (char)key;
            var cursor = this.SelectionStart;
            var cursorLength = this.SelectionLength;
            var oldText = this.Text;
            var split = this.Text.Split(new[] { this.Separator, ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var whitelistedKeys = new[] { Keys.Right, Keys.Left, Keys.Home, Keys.End };
            if (whitelistedKeys.Contains(key))
            {
                e.Handled = false;
                return;
            }

            if (key == Keys.Back)
            {
                if (cursorLength == 0 && cursor == 0) return;
                var toDelete = cursorLength > 0
                    ? oldText.Substring(cursor, cursorLength) : oldText.Substring(cursor - 1, 1);

                var newMid = toDelete.Contains(this.separator.ToString()) ? this.sep : string.Empty;

                var newLeft = cursorLength > 0 || cursor == 0
                    ? oldText.Substring(0, cursor) : oldText.Substring(0, cursor - 1);
                var newRight = cursorLength > 0 || cursor == 0
                    ? oldText.Substring(cursor + cursorLength) : oldText.Substring(cursor);
                if (newLeft == string.Empty && newRight.IndexOf(this.separator) < 1) newLeft = "0";

                this.Text = $"{newLeft}{newMid}{newRight}";

                if (cursorLength == 0 && cursor > 0)
                    cursor--;
            }
            else if (key == Keys.Delete)
            {
                if (cursor == oldText.Length) return;
                var toDelete = cursorLength > 0
                    ? oldText.Substring(cursor, cursorLength) : oldText.Substring(cursor, 1);

                var newMid = toDelete.Contains(this.separator.ToString()) ? this.sep : string.Empty;

                var newLeft = oldText.Substring(0, cursor);
                var newRight = cursorLength > 0 || cursor == oldText.Length
                    ? oldText.Substring(cursor + cursorLength) : oldText.Substring(cursor + 1);
                if (newLeft == string.Empty && newRight.IndexOf(this.separator) < 1) newLeft = "0";

                if (cursorLength == 0 && cursor < oldText.Length && newMid != string.Empty)
                    cursor++;

                this.Text = $"{newLeft}{newMid}{newRight}";
            }
            else if (newChar.ToString().Equals(this.Separator.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                if (!oldText.Contains(this.Separator.ToString()))
                {
                    this.Text += this.Separator;
                }
                else
                {
                    if (cursor == split[0].Length && cursor < split[0].Length + 1)
                        cursor = split[0].Length + this.sep.Length;
                    else if (this.SpacesAroundSeparator)
                        cursor = split[0].Length + this.sep.Length - 1;
                }
            }
            else if (newChar == ' ')
            {
                if (!this.spacesAroundSeparator) return;

                if (cursor == split[0].Length || cursor == split[0].Length + this.sep.Length - 1)
                    cursor++;
            }
            else if (newChar >= 96 && newChar <= 107 || Regex.IsMatch(newChar.ToString(), @"\d"))
            {
                if (newChar >= 96 && newChar <= 107) newChar = (char)(newChar - 48);

                if (split.Any() && cursor > split[0].Length && cursor < split[0].Length + this.sep.Length)
                    return;

                this.Text = cursorLength > 0
                    ? $"{oldText.Substring(0, cursor)}{newChar}{oldText.Substring(cursor + cursorLength)}"
                    : $"{oldText.Substring(0, cursor)}{newChar}{oldText.Substring(cursor)}";

                cursor++;
            }
            else
            {
                return;
            }

            if (this.Text == string.Empty)
            {
                this.Text = $"0{this.sep}0";
                this.SelectionStart = 0;
                this.SelectionLength = 1;
            }

            this.FormatText(cursor);
        }

        private void FormatText(int cursor)
        {
            var split = this.Text.Split(new[] { this.Separator, ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var newX = long.Parse(split[0]);
            if (newX > this.maxVal) newX = this.maxVal;
            var newY = split.Length > 1 ? long.Parse(split[1]) : 0;
            if (newY > this.maxVal) newY = this.maxVal;

            var makeSelection = false;
            if (cursor <= split[0].Length)
            {
                var leadingZeroes = split[0].Length - newX.ToString().Length;
                cursor -= leadingZeroes;

                if (newX == 0)
                {
                    makeSelection = true;
                    cursor = Math.Max(0, cursor - 1);
                }
            }
            else
            {
                if (split.Length > 1)
                {
                    var leadingZeroes = split[1].Length - newY.ToString().Length;
                    cursor -= leadingZeroes;
                }

                if (newY == 0 && cursor == newX.ToString().Length + this.sep.Length)
                    makeSelection = true;
            }

            this.Text = $"{newX}{this.sep}{newY}";

            this.SelectionStart = cursor;
            this.SelectionLength = makeSelection ? 1 : 0;

            if (this.x == (int)newX && this.y == (int)newY) return;
            this.x = (int)newX;
            this.y = (int)newY;
            this.ValueChanged?.Invoke(this, new TPoint(this.x, this.y));
        }

        private void UpdateText()
        {
            var cursor = this.SelectionStart;
            this.Text = $"{this.x}{this.sep}{this.y}";
            this.SelectionStart = cursor;
        }

        private void IgnoreKey(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        #endregion Methods
    }

    internal static class FixedHeightTextBox
    {
        public const int FixedUiHeight = 23;
    }
}
