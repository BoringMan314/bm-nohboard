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

    [DataContract(Name = "KeyboardKey", Namespace = "")]
    public class KeyboardKeyDefinition : KeyDefinition
    {
        #region Properties

        [DataMember]
        public string ShiftText { get; private set; }

        [DataMember]
        public bool ChangeOnCaps { get; private set; }

        #endregion Properties

        public KeyboardKeyDefinition(
            int id,
            List<TPoint> boundaries,
            List<int> keyCodes,
            string normalText,
            string shiftText,
            bool changeOnCaps,
            TPoint textPosition = null,
            ElementManipulation manipulation = null) : base(id, boundaries, keyCodes, normalText, textPosition, manipulation)
        {
            this.ShiftText = shiftText;
            this.ChangeOnCaps = changeOnCaps;
        }

        public void Render(Graphics g, bool pressed, bool shift, bool capsLock)
        {
            var style = GlobalSettings.CurrentStyle.TryGetElementStyle<KeyStyle>(this.Id)
                            ?? GlobalSettings.CurrentStyle.DefaultKeyStyle;
            var defaultStyle = GlobalSettings.CurrentStyle.DefaultKeyStyle;
            var subStyle = pressed ? style?.Pressed ?? defaultStyle?.Pressed : style?.Loose ?? defaultStyle?.Loose;
            if (subStyle == null)
                return;

            var txtSize = g.MeasureString(this.GetText(shift, capsLock), subStyle.Font);
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
            g.DrawString(this.GetText(shift, capsLock), subStyle.Font, new SolidBrush(subStyle.Text), (Point)txtPoint);
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

        public bool BordersWith(KeyboardKeyDefinition otherKey)
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
            return new KeyboardKeyDefinition(
                this.Id,
                this.Boundaries.Select(b => b.Translate(dx, dy)).ToList(),
                this.KeyCodes,
                this.Text,
                this.ShiftText,
                this.ChangeOnCaps,
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

            var txtSize = g.MeasureString(this.GetText(false, false), subStyle.Font);
            var txtPoint = new TPoint(
                this.TextPosition.X - (int)(txtSize.Width / 2),
                this.TextPosition.Y - (int)(txtSize.Height / 2));

            g.SetClip(this.GetBoundingBox());
            g.DrawString(this.GetText(false, false), subStyle.Font, new SolidBrush(subStyle.Text), (Point)txtPoint);
            g.ResetClip();

        }

        protected override KeyDefinition MoveBoundary(int index, Size diff)
        {
            if (index < 0 || index >= this.Boundaries.Count)
                throw new Exception("Attempting to move a non-existent boundary.");

            return new KeyboardKeyDefinition(
                this.Id,
                this.Boundaries.Select((b, i) => i != index ? b : b + diff).ToList(),
                this.KeyCodes,
                this.Text,
                this.ShiftText,
                this.ChangeOnCaps,
                GlobalSettings.Settings.UpdateTextPosition ? null : this.TextPosition,
                this.CurrentManipulation);
        }

        protected override KeyDefinition MoveText(Size diff)
        {
            return new KeyboardKeyDefinition(
                this.Id,
                this.Boundaries.ToList(),
                this.KeyCodes,
                this.Text,
                this.ShiftText,
                this.ChangeOnCaps,
                this.TextPosition + diff,
                this.CurrentManipulation);
        }

        protected override KeyDefinition MoveEdge(int index, Size diff)
        {
            if (index < 0 || index >= this.Boundaries.Count)
                throw new Exception("Attempting to move a non-existent edge.");

            bool doUpdate(int i) => i == index || i == (index + 1) % this.Boundaries.Count;

            var edgeBoundaries = this.Boundaries.Where((b, i) => doUpdate(i)).ToList();
            var edgeVector = (SizeF)(edgeBoundaries[1] - edgeBoundaries[0]);
            var othogonalVector = edgeVector.RotateDegrees(90);
            var projectedDiff = ((SizeF)diff).ProjectOn(othogonalVector);

            return new KeyboardKeyDefinition(
                this.Id,
                this.Boundaries.Select((b, i) => !doUpdate(i) ? b : b + projectedDiff).ToList(),
                this.KeyCodes,
                this.Text,
                this.ShiftText,
                this.ChangeOnCaps,
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

            return new KeyboardKeyDefinition(
                this.Id,
                this.Boundaries.Where((b, i) => i != this.RelevantManipulation.Index).ToList(),
                this.KeyCodes,
                this.Text,
                this.ShiftText,
                this.ChangeOnCaps,
                GlobalSettings.Settings.UpdateTextPosition ? null : this.TextPosition,
                this.RelevantManipulation);
        }

        public override KeyDefinition AddBoundary(TPoint location)
        {
            if (this.RelevantManipulation == null) return this;
            if (this.RelevantManipulation.Type != ElementManipulationType.MoveEdge)
                throw new Exception("Attempting to add a boundary to something other than an edge.");

            var newBoundaries = this.Boundaries.ToList();
            newBoundaries.Insert(this.RelevantManipulation.Index + 1, location);

            return new KeyboardKeyDefinition(
                this.Id,
                newBoundaries,
                this.KeyCodes,
                this.Text,
                this.ShiftText,
                this.ChangeOnCaps,
                GlobalSettings.Settings.UpdateTextPosition ? null : this.TextPosition,
                this.CurrentManipulation);
        }

        public override KeyDefinition UnionWith(List<KeyDefinition> keys)
        {
            return this.UnionWith(keys.ConvertAll(x => (KeyboardKeyDefinition)x));
        }

        public override KeyDefinition ModifyMouse(List<TPoint> boundaries = null, int? keyCode = null, string text = null, TPoint textPosition = null)
        {
            throw new Exception("Cannot modify  the mouse properties of a keyboard key.");
        }

        public KeyboardKeyDefinition Modify(
            List<TPoint> boundaries = null, List<int> keyCodes = null, string text = null, string shiftText = null,
            bool? changeOnCaps = null, TPoint textPosition = null)
        {
            return new KeyboardKeyDefinition(
                this.Id,
                boundaries ?? this.Boundaries.Select(x => x.Clone()).ToList(),
                keyCodes ?? this.KeyCodes.ToList(),
                text ?? this.Text,
                shiftText ?? this.ShiftText,
                changeOnCaps ?? this.ChangeOnCaps,
                textPosition ?? this.TextPosition,
                this.CurrentManipulation);
        }

        #endregion Transformations

        #region Private methods

        private string GetText(bool shift, bool capsLock)
        {
            if (GlobalSettings.Settings.Capitalization != CapitalizationMethod.FollowKeys)
            {
                capsLock = GlobalSettings.Settings.Capitalization == CapitalizationMethod.Capitalize;

                var preserveShift = GlobalSettings.Settings.FollowShiftForCapsInsensitive && !this.ChangeOnCaps
                                    || GlobalSettings.Settings.FollowShiftForCapsSensitive && this.ChangeOnCaps;
                shift &= preserveShift;
            }

            var capitalize = this.ChangeOnCaps && (capsLock ^ shift) || !this.ChangeOnCaps && shift;
            return capitalize ? this.ShiftText : this.Text;
        }

        private KeyboardKeyDefinition UnionWith(IList<KeyboardKeyDefinition> keys)
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

            return new KeyboardKeyDefinition(
                this.Id,
                newBoundaries,
                this.KeyCodes,
                this.Text,
                this.ShiftText,
                this.ChangeOnCaps);
        }

        public override ElementDefinition Clone()
        {
            return new KeyboardKeyDefinition(
                this.Id,
                this.Boundaries.Select(x => x.Clone()).ToList(),
                this.KeyCodes.ToList(),
                this.Text,
                this.ShiftText,
                this.ChangeOnCaps,
                this.TextPosition,
                this.CurrentManipulation);
        }

        public override bool IsChanged(ElementDefinition other)
        {
            if (!(other is KeyboardKeyDefinition kkd)) return true;

            if (this.Text != kkd.Text) return true;
            if (this.ShiftText != kkd.ShiftText) return true;
            if (this.ChangeOnCaps != kkd.ChangeOnCaps) return true;
            if (this.TextPosition.IsChanged(kkd.TextPosition)) return true;
            if (!this.KeyCodes.ToSet().SetEquals(kkd.KeyCodes)) return true;

            if (this.Boundaries.Count != kkd.Boundaries.Count) return true;

            for (var i = 0; i < this.Boundaries.Count; i++)
            {
                if (this.Boundaries[i].IsChanged(kkd.Boundaries[i])) return true;
            }

            return false;
        }

        #endregion Private methods
    }
}
