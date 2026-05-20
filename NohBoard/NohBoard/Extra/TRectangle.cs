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

namespace ThoNohT.NohBoard.Extra
{
    using System;
    using System.Drawing;

    public class TRectangle
    {
        #region Constructors

        public TRectangle(int left, int top, int right, int bottom)
        {
            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
        }

        public TRectangle(TPoint position, TPoint size)
        {
            this.Left = position.X;
            this.Top = position.Y;
            this.Right = position.X + size.X;
            this.Bottom = position.Y + size.Y;
        }

        #endregion Constructors

        #region Edges

        public int Left { get; private set; }

        public int Top { get; private set; }

        public int Right { get; private set; }

        public int Bottom { get; private set; }

        #endregion Edges

        #region Corner points

        public TPoint Position => this.TopLeft;

        public Size Size => new Size(this.Right - this.Left, this.Bottom - this.Top);

        public TPoint TopLeft => new TPoint(this.Left, this.Top);

        public TPoint TopRight => new TPoint(this.Right, this.Top);

        public TPoint BottomLeft => new TPoint(this.Left, this.Bottom);

        public TPoint BottomRight => new TPoint(this.Right, this.Bottom);

        #endregion Corner points

        #region Methods

        public static TRectangle FromPointList(TPoint[] points)
        {
            var left = int.MaxValue;
            var top = int.MaxValue;
            var right = int.MinValue;
            var bottom = int.MinValue;

            foreach (var point in points)
            {
                left = Math.Min(left, point.X);
                top = Math.Min(top, point.Y);
                right = Math.Max(right, point.X);
                bottom = Math.Max(bottom, point.Y);
            }

            return new TRectangle(left, top, right, bottom);
        }

        #endregion Methods
    }
}
