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

namespace ThoNohT.NohBoard.Keyboard.ElementDefinitions
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Runtime.Serialization;
    using Extra;
    using Styles;

    [DataContract(Name = "MouseSpeedIndicator", Namespace = "")]
    public class MouseSpeedIndicatorDefinition : ElementDefinition
    {
        #region Properties

        [DataMember]
        public TPoint Location { get; private set; }

        [DataMember]
        public int Radius { get; private set; }

        #endregion Properties

        public MouseSpeedIndicatorDefinition(int id, TPoint location, int radius)
        {
            this.Id = id;
            this.Location = location;
            this.Radius = radius;
        }

        private MouseSpeedIndicatorDefinition(int id, TPoint location, int radius, ElementManipulation manipulation)
        {
            this.Id = id;
            this.Location = location;
            this.Radius = radius;
            this.CurrentManipulation = manipulation;
        }

        public void Render(Graphics g, SizeF speed)
        {
            var subStyle = GlobalSettings.CurrentStyle.TryGetElementStyle<MouseSpeedIndicatorStyle>(this.Id)
                           ?? GlobalSettings.CurrentStyle.DefaultMouseSpeedIndicatorStyle;

            var smallRadius = (float) this.Radius / 5;

            var sensitivity = GlobalSettings.Settings.MouseSensitivity / (float) 100;

            var pointerLength = (int) Math.Min(this.Radius, sensitivity * speed.Length() * this.Radius);

            var colorMultiplier = Math.Max(0, Math.Min(1, (float) pointerLength / this.Radius));

            Color color1 = subStyle.InnerColor;
            Color outerColor = subStyle.OuterColor;
            var color2 = Color.FromArgb(
                (int) (color1.R * (1 - colorMultiplier) + outerColor.R * colorMultiplier),
                (int) (color1.G * (1 - colorMultiplier) + outerColor.G * colorMultiplier),
                (int) (color1.B * (1 - colorMultiplier) + outerColor.B * colorMultiplier));

            g.DrawEllipse(
                new Pen(color1, subStyle.OutlineWidth),
                Geom.CircleToRectangle(this.Location, this.Radius));

            if (pointerLength > 0)
            {
                var angle = speed.GetAngle();

                var pointerEnd = this.Location.CircularTranslate(pointerLength, angle);

                if (pointerEnd.X != this.Location.X || pointerEnd.Y != this.Location.Y)
                {
                    g.FillPie(
                        new LinearGradientBrush(this.Location, pointerEnd, color1, color2),
                        Geom.CircleToRectangle(this.Location, pointerLength),
                        Geom.RadToDeg(angle) - 10,
                        20);

                    var pointerEdge = this.Location.CircularTranslate(this.Radius, angle);
                    g.FillEllipse(new SolidBrush(color2), Geom.CircleToRectangle(pointerEdge, (int) smallRadius));
                }
            }

            g.FillEllipse(new SolidBrush(color1), Geom.CircleToRectangle(this.Location, (int) smallRadius));
        }

        public override void RenderEditing(Graphics g)
        {
            g.FillEllipse(new SolidBrush(Color.Silver), Geom.CircleToRectangle(this.Location, this.Radius));
            g.DrawEllipse(new Pen(Color.White, 1), Geom.CircleToRectangle(this.Location, this.Radius));
            g.FillEllipse(new SolidBrush(Color.White), Geom.CircleToRectangle(this.Location, this.Radius / 5));
        }

        public override void RenderHighlight(Graphics g)
        {
            g.FillEllipse(Constants.HighlightBrush, Geom.CircleToRectangle(this.Location, this.Radius));

            if (this.RelevantManipulation?.Type == ElementManipulationType.Scale)
                g.DrawEllipse(new Pen(Color.White, 3), Geom.CircleToRectangle(this.Location, this.Radius));
        }

        public override void RenderSelected(Graphics g)
        {
            g.DrawEllipse(new Pen(Constants.SelectedColor, 2), Geom.CircleToRectangle(this.Location, this.Radius));
            g.FillEllipse(
                new SolidBrush(Constants.SelectedColor),
                Geom.CircleToRectangle(this.Location, this.Radius / 5));

            if (this.RelevantManipulation?.Type == ElementManipulationType.Scale)
                g.DrawEllipse(
                    new Pen(Constants.SelectedColorSpecial, 3),
                    Geom.CircleToRectangle(this.Location, this.Radius));
        }

        public override Rectangle GetBoundingBox()
        {
            return new Rectangle(
                new Point(this.Location.X - this.Radius, this.Location.Y - this.Radius),
                new Size(2 * this.Radius, 2 * this.Radius));
        }

        #region Transformations

        public override ElementDefinition Translate(int dx, int dy)
        {
            return new MouseSpeedIndicatorDefinition(
                this.Id,
                new Point(this.Location.X + dx, this.Location.Y + dy),
                this.Radius,
                this.CurrentManipulation);
        }

        public MouseSpeedIndicatorDefinition SetRadius(int newRadius)
        {
            return new MouseSpeedIndicatorDefinition(
                this.Id,
                this.Location,
                newRadius,
                this.CurrentManipulation);
        }

        public override bool Inside(Point point)
        {
            var bb = this.GetBoundingBox();
            return point.X >= bb.Left && point.X <= bb.Right && point.Y >= bb.Top && point.Y <= bb.Bottom;
        }

        public override bool StartManipulating(Point point, bool altDown, bool preview = false, bool translateOnly = false)
        {
            SizeF d = point - this.Location;
            if (d.Length() > this.Radius + 2)
            {
                this.PreviewManipulation = null;
                return false;
            }

            if (Math.Sqrt(Math.Abs(Math.Pow(d.Width, 2) + Math.Pow(d.Height, 2) - Math.Pow(this.Radius, 2))) < 16 && !translateOnly)
            {
                this.SetManipulation(
                    new ElementManipulation
                    {
                        Type = ElementManipulationType.Scale,
                        Index = 0,
                        Direction = d.GetUnitVector()

                    },
                    preview);
                return true;
            }

            this.SetManipulation(
                new ElementManipulation
                {
                    Type = ElementManipulationType.Translate,
                    Index = 0
                },
                preview);

            return true;
        }

        public override ElementDefinition Manipulate(Size diff)
        {
            if (this.RelevantManipulation == null) return this;

            switch (this.RelevantManipulation.Type)
            {
                case ElementManipulationType.Translate:
                    return this.Translate(diff.Width, diff.Height);

                case ElementManipulationType.Scale:
                    return this.Scale(diff, this.RelevantManipulation.Direction);

                default:
                    return this;
            }
        }

        private ElementDefinition Scale(Size diff, SizeF direction)
        {
            var distanceToGrabPoint = direction.Multiply(this.Radius);
            var grabPoint = this.Location + distanceToGrabPoint;

            var movedGrabPoint = grabPoint + ((SizeF) diff).ProjectOn(direction);
            var movedDistance = (movedGrabPoint - this.Location).Length();

            return new MouseSpeedIndicatorDefinition(
                this.Id,
                this.Location,
                (int) movedDistance,
                this.CurrentManipulation);
        }

        #endregion Transformations

        public override ElementDefinition Clone()
        {
            return new MouseSpeedIndicatorDefinition(
                this.Id,
                this.Location.Clone(),
                this.Radius,
                this.CurrentManipulation);
        }

        public override bool IsChanged(ElementDefinition other)
        {
            if (!(other is MouseSpeedIndicatorDefinition msid)) return true;

            return this.Location.IsChanged(msid.Location) || this.Radius != msid.Radius;
        }
    }
}
