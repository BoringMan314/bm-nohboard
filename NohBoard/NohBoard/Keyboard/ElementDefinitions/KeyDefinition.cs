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
    using ThoNohT.NohBoard.Keyboard.Styles;

    [DataContract(Name = "Key", Namespace = "")]
    [KnownType(typeof(KeyboardKeyDefinition))]
    [KnownType(typeof(MouseKeyDefinition))]
    [KnownType(typeof(MouseScrollDefinition))]
    public abstract class KeyDefinition : ElementDefinition
    {
        #region Fields

        private Dictionary<bool, Brush> backgroundBrushes;

        private int cachedOverlayTransparency = -1;

        #endregion Fields

        #region Properties

        [DataMember]
        public List<TPoint> Boundaries { get; private set; }

        [DataMember]
        public TPoint TextPosition { get; private set; }

        [DataMember]
        public List<int> KeyCodes { get; private set; }

        [DataMember]
        public string Text { get; private set; }

        #endregion Properties

        protected KeyDefinition(
            int id,
            List<TPoint> boundaries,
            List<int> keyCodes,
            string text,
            TPoint textPosition = null,
            ElementManipulation manipulation = null)
        {
            this.Id = id;
            this.Boundaries = boundaries;
            this.KeyCodes = keyCodes;
            this.Text = text;
            this.CurrentManipulation = manipulation;

            var bb = this.GetBoundingBoxImpl();
            this.TextPosition = textPosition ?? (TPoint)bb.Location + new Size(bb.Width / 2, bb.Height / 2);
        }

        public override Rectangle GetBoundingBox()
        {
            return this.GetBoundingBoxImpl();
        }

        public bool BordersWith(KeyDefinition otherKey)
        {
            var clipper = new Clipper();

            clipper.AddPath(this.GetPath(), PolyType.ptSubject, true);
            clipper.AddPath(otherKey.GetPath(), PolyType.ptClip, true);

            var union = new List<List<IntPoint>>();
            clipper.Execute(ClipType.ctUnion, union);

            return union.Count == 1;
        }

        public override bool Inside(Point point)
        {
            return Clipper.PointInPolygon((TPoint)point, this.GetPath()) != 0;
        }

        public override void RenderEditing(Graphics g)
        {
            g.FillPolygon(Brushes.Silver, this.Boundaries.ConvertAll<Point>(x => x).ToArray());
            g.DrawPolygon(new Pen(Brushes.White, 1), this.Boundaries.ConvertAll<Point>(x => x).ToArray());
        }

        public override void RenderHighlight(Graphics g)
        {
            g.FillPolygon(Constants.HighlightBrush, this.Boundaries.ConvertAll<Point>(x => x).ToArray());

            if (this.RelevantManipulation == null) return;

            switch (this.RelevantManipulation.Type)
            {
                case ElementManipulationType.MoveBoundary:
                    var boundary = this.Boundaries[this.RelevantManipulation.Index];
                    g.FillRectangle(Brushes.White, boundary.X - 3, boundary.Y - 3, 6, 6);
                    break;

                case ElementManipulationType.MoveEdge:
                    var index = this.RelevantManipulation.Index;
                    Func<int, bool> doUpdate = i => i == index || i == (index + 1) % this.Boundaries.Count;
                    var edgeBoundaries = this.Boundaries.Where((b, i) => doUpdate(i)).ToList();
                    g.DrawLine(new Pen(Color.White, 3), edgeBoundaries[0], edgeBoundaries[1]);
                    break;
            }
        }

        public override void RenderSelected(Graphics g)
        {
            g.DrawPolygon(new Pen(Constants.SelectedColor, 3), this.Boundaries.ConvertAll<Point>(x => x).ToArray());

            if (this.RelevantManipulation == null) return;

            switch (this.RelevantManipulation.Type)
            {
                case ElementManipulationType.MoveBoundary:
                    var boundary = this.Boundaries[this.RelevantManipulation.Index];
                    var specialBrush = new SolidBrush(Constants.SelectedColorSpecial);
                    g.FillRectangle(specialBrush, boundary.X - 3, boundary.Y - 3, 6, 6);
                    break;

                case ElementManipulationType.MoveEdge:
                    var index = this.RelevantManipulation.Index;
                    Func<int, bool> doUpdate = i => i == index || i == (index + 1) % this.Boundaries.Count;
                    var edgeBoundaries = this.Boundaries.Where((b, i) => doUpdate(i)).ToList();
                    g.DrawLine(new Pen(Constants.SelectedColorSpecial, 4), edgeBoundaries[0], edgeBoundaries[1]);
                    break;
            }
        }

        public override bool StartManipulating(Point point, bool altDown, bool preview = false, bool translateOnly = false)
        {
            if (!this.Inside(point))
            {
                this.PreviewManipulation = null;
                return false;
            }

            var activeBoundary = this.Boundaries.FirstOrDefault(
                b => point.X <= b.X + 4 &&
                     point.X >= b.X - 4 &&
                     point.Y <= b.Y + 4 &&
                     point.Y >= b.Y - 4);

            if (activeBoundary != null  && !translateOnly)
            {
                this.SetManipulation(
                    new ElementManipulation
                    {
                        Type = ElementManipulationType.MoveBoundary,
                        Index = this.Boundaries.IndexOf(activeBoundary)
                    },
                    preview);
                return true;
            }

            var boundaries2 = this.Boundaries.Skip(1).ToList();
            boundaries2.Add(this.Boundaries.First());

            var activeEdge = this.Boundaries.Zip(boundaries2, Tuple.Create)
                .FirstOrDefault(
                    e =>
                    {
                        if (Math.Min(e.Item1.X, e.Item2.X) - 4 > point.X) return false;
                        if (Math.Max(e.Item1.X, e.Item2.X) + 4 < point.X) return false;
                        if (Math.Min(e.Item1.Y, e.Item2.Y) - 4 > point.Y) return false;
                        if (Math.Max(e.Item1.Y, e.Item2.Y) + 4 < point.Y) return false;

                        var ac = (e.Item1 - point).Length();
                        var cb = (e.Item2 - point).Length();
                        var ab = (e.Item2 - e.Item1).Length();

                        return Math.Abs(ac + cb - ab) < 4;
                    });

            if (activeEdge != null && !translateOnly)
            {
                this.SetManipulation(
                    new ElementManipulation
                    {
                        Type = ElementManipulationType.MoveEdge,
                        Index = this.Boundaries.IndexOf(activeEdge.Item1)
                    },
                    preview);
                return true;
            }

            if (altDown)
            {
                this.SetManipulation(
                    new ElementManipulation
                    {
                        Type = ElementManipulationType.MoveText,
                        Index = 0
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
            if (this.CurrentManipulation == null) return this;

            switch (this.CurrentManipulation.Type)
            {
                case ElementManipulationType.Translate:
                    return this.Translate(diff.Width, diff.Height);

                case ElementManipulationType.MoveBoundary:
                    return this.MoveBoundary(this.RelevantManipulation.Index, diff);

                case ElementManipulationType.MoveEdge:
                    return this.MoveEdge(this.RelevantManipulation.Index, diff);

                case ElementManipulationType.MoveText:
                    return this.MoveText(diff);

                default:
                    return this;
            }
        }

        protected abstract KeyDefinition MoveText(Size diff);

        protected abstract KeyDefinition MoveEdge(int index, Size diff);

        protected abstract KeyDefinition MoveBoundary(int index, Size diff);

        public abstract KeyDefinition AddBoundary(TPoint location);

        public abstract KeyDefinition RemoveBoundary();

        public abstract KeyDefinition UnionWith(List<KeyDefinition> keys);

        #region Private methods

        protected List<IntPoint> GetPath()
        {
            return this.Boundaries.ConvertAll<IntPoint>(x => x);
        }

        protected Brush GetBackgroundBrush(KeySubStyle subStyle, bool pressed)
        {
            var transparency = GlobalSettings.Settings?.OverlayTransparencyPercent ?? 0;

            if (this.backgroundBrushes == null) this.backgroundBrushes = new Dictionary<bool, Brush>();
            if (this.StyleVersion != GlobalSettings.StyleDependencyCounter
                || this.cachedOverlayTransparency != transparency)
            {
                foreach (var b in this.backgroundBrushes.Values)
                    b.Dispose();

                this.backgroundBrushes.Clear();
                this.StyleVersion = GlobalSettings.StyleDependencyCounter;
                this.cachedOverlayTransparency = transparency;
            }

            if (this.backgroundBrushes.ContainsKey(pressed))
                return this.backgroundBrushes[pressed];

            if (subStyle == null || OverlayTransparency.HidesFrameAndFill(transparency))
                return Brushes.Transparent;

            var brush = OverlayTransparency.ApplyToBrush(
                subStyle.GetBackgroundBrush(this.GetBoundingBox()),
                transparency);

            this.backgroundBrushes.Add(pressed, brush);
            return brush;
        }

        private Rectangle GetBoundingBoxImpl()
        {
            var xPositions = this.Boundaries.Select(b => b.X).ToList();
            var yPositions = this.Boundaries.Select(b => b.Y).ToList();

            var min = new Point(xPositions.Min(), yPositions.Min());
            var max = new Point(xPositions.Max(), yPositions.Max());

            return new Rectangle(min, new Size(max.X - min.X, max.Y - min.Y));
        }

        public abstract KeyDefinition ModifyMouse(
            List<TPoint> boundaries = null, int? keyCode = null, string text = null, TPoint textPosition = null);

        #endregion Private methods
    }
}
