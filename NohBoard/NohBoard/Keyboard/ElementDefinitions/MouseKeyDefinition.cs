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
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.Serialization;
    using ClipperLib;
    using Extra;
    using Styles;

    [DataContract(Name = "MouseKey", Namespace = "")]
    public class MouseKeyDefinition : KeyDefinition
    {
        public MouseKeyDefinition(
            int id,
            List<TPoint> boundaries,
            int keyCode,
            string text,
            TPoint textPosition = null,
            ElementManipulation manipulation = null)
            : base(id, boundaries, keyCode.Singleton(), text, textPosition, manipulation)
        {
        }

        public void Render(Graphics g, bool pressed, bool shift, bool capsLock)
        {
            var style = GlobalSettings.CurrentStyle.TryGetElementStyle<KeyStyle>(this.Id)
                            ?? GlobalSettings.CurrentStyle.DefaultKeyStyle;
            var defaultStyle = GlobalSettings.CurrentStyle.DefaultKeyStyle;
            var subStyle = pressed ? style?.Pressed ?? defaultStyle?.Pressed : style?.Loose ?? defaultStyle?.Loose;
            if (subStyle == null)
                return;

            var txtSize = g.MeasureString(this.Text, subStyle.Font);
            var txtPoint = new TPoint(
                this.TextPosition.X - (int)(txtSize.Width / 2),
                this.TextPosition.Y - (int)(txtSize.Height / 2));

            if (!OverlayTransparency.HidesFrameAndFill(
                    GlobalSettings.Settings?.OverlayTransparencyPercent ?? 0))
            {
                var backgroundBrush = this.GetBackgroundBrush(subStyle, pressed);
                g.FillPolygon(backgroundBrush, this.Boundaries.ConvertAll<Point>(x => x).ToArray());
            }

            g.SetClip(this.GetBoundingBox());
            g.DrawString(this.Text, subStyle.Font, new SolidBrush(subStyle.Text), (Point)txtPoint);
            g.ResetClip();

            if (subStyle.ShowOutline)
            {
                using var outlinePen = OverlayTransparency.CreateOutlinePen(
                    subStyle.Outline,
                    subStyle.OutlineWidth,
                    GlobalSettings.Settings?.OverlayTransparencyPercent ?? 0);
                g.DrawPolygon(outlinePen, this.Boundaries.ConvertAll<Point>(x => x).ToArray());
            }
        }

        public bool BordersWith(MouseKeyDefinition otherKey)
        {
            var clipper = new Clipper();

            clipper.AddPath(this.GetPath(), PolyType.ptSubject, true);
            clipper.AddPath(otherKey.GetPath(), PolyType.ptClip, true);

            var union = new List<List<IntPoint>>();
            clipper.Execute(ClipType.ctUnion, union);

            return union.Count == 1;
        }

        #region Transformations

        public override ElementDefinition Translate(int dx, int dy)
        {
            return new MouseKeyDefinition(
                this.Id,
                this.Boundaries.Select(b => b.Translate(dx, dy)).ToList(),
                this.KeyCodes.Single(),
                this.Text,
                this.TextPosition.Translate(dx, dy),
                this.CurrentManipulation);
        }

        public override void RenderEditing(Graphics g)
        {
            base.RenderEditing(g);

            var style = GlobalSettings.CurrentStyle.TryGetElementStyle<KeyStyle>(this.Id)
                            ?? GlobalSettings.CurrentStyle.DefaultKeyStyle;
            var defaultStyle = GlobalSettings.CurrentStyle.DefaultKeyStyle;
            var subStyle = style?.Loose ?? defaultStyle.Loose;

            var txtSize = g.MeasureString(this.Text, subStyle.Font);
            var txtPoint = new TPoint(
                this.TextPosition.X - (int)(txtSize.Width / 2),
                this.TextPosition.Y - (int)(txtSize.Height / 2));

            g.SetClip(this.GetBoundingBox());
            g.DrawString(this.Text, subStyle.Font, new SolidBrush(subStyle.Text), (Point)txtPoint);
            g.ResetClip();
        }

        protected override KeyDefinition MoveBoundary(int index, Size diff)
        {
            if (index < 0 || index >= this.Boundaries.Count)
                throw new Exception("Attempting to move a non-existent boundary.");

            return new MouseKeyDefinition(
                this.Id,
                this.Boundaries.Select((b, i) => i != index ? b : b + diff).ToList(),
                this.KeyCodes.Single(),
                this.Text,
                GlobalSettings.Settings.UpdateTextPosition ? null : this.TextPosition,
                this.CurrentManipulation);
        }

        protected override KeyDefinition MoveText(Size diff)
        {
            return new MouseKeyDefinition(
                this.Id,
                this.Boundaries.ToList(),
                this.KeyCodes.Single(),
                this.Text,
                this.TextPosition + diff,
                this.CurrentManipulation);
        }

        protected override KeyDefinition MoveEdge(int index, Size diff)
        {
            if (index < 0 || index >= this.Boundaries.Count)
                throw new Exception("Attempting to move a non-existent edge.");

            Func<int, bool> doUpdate = i => i == index || i == (index + 1) % this.Boundaries.Count;

            var edgeBoundaries = this.Boundaries.Where((b, i) => doUpdate(i)).ToList();
            var edgeVector = (SizeF)(edgeBoundaries[1] - edgeBoundaries[0]);
            var othogonalVector = edgeVector.RotateDegrees(90);
            var projectedDiff = ((SizeF)diff).ProjectOn(othogonalVector);

            return new MouseKeyDefinition(
                this.Id,
                this.Boundaries.Select((b, i) => !doUpdate(i) ? b : b + projectedDiff).ToList(),
                this.KeyCodes.Single(),
                this.Text,
                GlobalSettings.Settings.UpdateTextPosition ? null : this.TextPosition,
                this.CurrentManipulation);
        }

        public override KeyDefinition RemoveBoundary()
        {
            if (this.RelevantManipulation == null) return this;
            if (this.RelevantManipulation.Type != ElementManipulationType.MoveBoundary)
                throw new Exception("Attempting to remove something other than a boundary.");

            if (this.Boundaries.Count < 4)
                throw new Exception("Cannot have less than 3 boundary in an element.");

            return new MouseKeyDefinition(
                this.Id,
                this.Boundaries.Where((b, i) => i != this.RelevantManipulation.Index).ToList(),
                this.KeyCodes.Single(),
                this.Text,
                GlobalSettings.Settings.UpdateTextPosition ? null : this.TextPosition,
                this.CurrentManipulation);
        }

        public override KeyDefinition AddBoundary(TPoint location)
        {
            if (this.RelevantManipulation == null) return this;
            if (this.RelevantManipulation.Type != ElementManipulationType.MoveEdge)
                throw new Exception("Attempting to add a boundary to something other than an edge.");

            var newBoundaries = this.Boundaries.ToList();
            newBoundaries.Insert(this.RelevantManipulation.Index + 1, location);

            return new MouseKeyDefinition(
                this.Id,
                newBoundaries,
                this.KeyCodes.Single(),
                this.Text,
                GlobalSettings.Settings.UpdateTextPosition ? null : this.TextPosition,
                this.CurrentManipulation);
        }

        public override KeyDefinition UnionWith(List<KeyDefinition> keys)
        {
            return this.UnionWith(keys.ConvertAll(x => (MouseKeyDefinition)x));
        }

        public override KeyDefinition ModifyMouse(List<TPoint> boundaries = null, int? keyCode = null, string text = null, TPoint textPosition = null)
        {
            return new MouseKeyDefinition(
                this.Id,
                boundaries ?? this.Boundaries.Select(x => x.Clone()).ToList(),
                keyCode ?? this.KeyCodes.Single(),
                text ?? this.Text,
                textPosition ?? this.TextPosition,
                this.CurrentManipulation);
        }

        #endregion Transformations

        #region Private methods

        private MouseKeyDefinition UnionWith(IList<MouseKeyDefinition> keys)
        {
            var newBoundaries = this.Boundaries.Select(b => new TPoint(b.X, b.Y)).ToList();

            if (keys.Any())
            {
                var cl = new Clipper();
                cl.AddPath(this.GetPath(), PolyType.ptSubject, true);
                cl.AddPaths(keys.Select(x => x.GetPath()).ToList(), PolyType.ptClip, true);
                var union = new List<List<IntPoint>>();
                cl.Execute(ClipType.ctUnion, union);

                if (union.Count > 1)
                    throw new ArgumentException("Cannot union two non-overlapping keys.");

                newBoundaries = union.Single().ConvertAll<TPoint>(x => x);
            }

            return new MouseKeyDefinition(this.Id, newBoundaries, this.KeyCodes.Single(), this.Text);
        }

        public override ElementDefinition Clone()
        {
            return new MouseKeyDefinition(
                this.Id,
                this.Boundaries.Select(x => x.Clone()).ToList(),
                this.KeyCodes.Single(),
                this.Text,
                this.TextPosition,
                this.CurrentManipulation);
        }

        public override bool IsChanged(ElementDefinition other)
        {
            if (!(other is MouseKeyDefinition mkd)) return true;

            if (this.Text != mkd.Text) return true;
            if (this.TextPosition.IsChanged(mkd.TextPosition)) return true;
            if (!this.KeyCodes.ToSet().SetEquals(mkd.KeyCodes)) return true;

            if (this.Boundaries.Count != mkd.Boundaries.Count) return true;

            for (var i = 0; i < this.Boundaries.Count; i++)
            {
                if (this.Boundaries[i].IsChanged(mkd.Boundaries[i])) return true;
            }

            return false;
        }

        #endregion Private methods
    }
}
