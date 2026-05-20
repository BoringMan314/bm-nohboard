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

namespace ThoNohT.NohBoard.Extra
{
    using System;
    using System.Drawing;
    using System.Runtime.Serialization;
    using ClipperLib;

    [DataContract(Name = "TPoint", Namespace = "")]
    public class TPoint
    {
        [DataMember]
        public int X { get; private set; }

        [DataMember]
        public int Y { get; private set; }

        public TPoint(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public TPoint(float x, float y)
        {
            this.X = (int)x;
            this.Y = (int)y;
        }

        public TPoint(double x, double y)
        {
            this.X = (int)x;
            this.Y = (int)y;
        }

        #region Conversion

        public static implicit operator IntPoint(TPoint point)
        {
            return new IntPoint(point.X, point.Y);
        }

        public static implicit operator Point(TPoint point)
        {
            return new Point(point.X, point.Y);
        }

        public static implicit operator TPoint(IntPoint intPoint)
        {
            return new TPoint(intPoint.X, intPoint.Y);
        }

        public static implicit operator TPoint(Point point)
        {
            return new TPoint(point.X, point.Y);
        } 

        #endregion Conversion

        #region Modification

        public static TPoint operator +(TPoint point, Size size)
        {
            return point.Translate(size);
        }

        public static TPoint operator +(TPoint point, SizeF size)
        {
            return point.Translate(size);
        }

        public static Size operator -(TPoint point, TPoint point2)
        {
            return new Size(point.X - point2.X, point.Y - point2.Y);
        }

        public TPoint Translate(Size size)
        {
            return this.Translate(size.Width, size.Height);
        }

        public TPoint Translate(SizeF size)
        {
            return this.Translate(size.Width, size.Height);
        }

        public TPoint Translate(int dx, int dy)
        {
            return new TPoint(this.X + dx, this.Y + dy);
        }

        public TPoint Translate(float dx, float dy)
        {
            return new TPoint(this.X + dx, this.Y + dy);
        }

        #endregion Modification

        public override string ToString()
        {
            return $"({this.X}, {this.Y})";
        }

        public TPoint Clone()
        {
            return new TPoint(this.X, this.Y);
        }
        internal bool IsChanged(TPoint other)
        {
            return this.X != other.X || this.Y != other.Y;
        }
    }
}
