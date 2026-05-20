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

namespace ThoNohT.NohBoard.Keyboard.Styles
{
    using System.Drawing;
    using System.Runtime.Serialization;
    using Extra;

    [DataContract(Name = "KeySubStyle", Namespace = "")]
    public class KeySubStyle
    {
        #region Properties

        [DataMember]
        public SerializableColor Background { get; set; }

        [DataMember]
        public SerializableColor Text { get; set; }

        [DataMember]
        public SerializableColor Outline { get; set; }

        [DataMember]
        public bool ShowOutline { get; set; }

        [DataMember]
        public int OutlineWidth { get; set; } = 1;

        [DataMember]
        public SerializableFont Font { get; set; } = new Font(FontFamily.GenericMonospace, 10);

        [DataMember]
        public string BackgroundImageFileName { get; set; }

        #endregion Properties

        public KeySubStyle Clone()
        {
            return new KeySubStyle
            {
                Background = this.Background.Clone(),
                BackgroundImageFileName = this.BackgroundImageFileName,
                Text = this.Text.Clone(),
                Outline = this.Outline.Clone(),
                OutlineWidth = this.OutlineWidth,
                ShowOutline = this.ShowOutline,
                Font = this.Font.Clone()
            };
        }

        public void DrawBackground(Graphics g, Point[] polygon, Rectangle boundingBox, int transparencyPercent)
        {
            var p = OverlayTransparency.ClampPercent(transparencyPercent);
            if (p >= 100)
                return;

            if (this.BackgroundImageFileName == null || !FileHelper.StyleImageExists(this.BackgroundImageFileName))
            {
                using var brush = new SolidBrush(OverlayTransparency.Apply(this.Background, transparencyPercent));
                g.FillPolygon(brush, polygon);
                return;
            }

            var img = ImageCache.Get(this.BackgroundImageFileName);
            var state = g.Save();
            try
            {
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    path.AddPolygon(polygon);
                    g.SetClip(path);
                    OverlayTransparency.DrawImage(g, img, boundingBox, transparencyPercent);
                }
            }
            finally
            {
                g.Restore(state);
            }
        }

        public Brush GetBackgroundBrush(Rectangle boundingBox)
        {
            return this.BackgroundImageFileName == null || !FileHelper.StyleImageExists(this.BackgroundImageFileName)
                ? new SolidBrush(this.Background)
                : this.BrushFromImage(boundingBox, this.BackgroundImageFileName);
        }

        private Brush BrushFromImage(Rectangle boundingBox, string fileName)
        {
            var img = ImageCache.Get(fileName);
            var gu = GraphicsUnit.Pixel;
            var imgBb = img.GetBounds(ref gu);

            var tex = new TextureBrush(img, imgBb);
            tex.TranslateTransform(boundingBox.Left, boundingBox.Top);
            tex.ScaleTransform(boundingBox.Width / imgBb.Width, boundingBox.Height / imgBb.Height);

            return tex;
        }

        public bool IsChanged(KeySubStyle other)
        {
            if (this.Background.IsChanged(other.Background)) return true;
            if (this.BackgroundImageFileName != other.BackgroundImageFileName) return true;
            if (this.Font.IsChanged(other.Font)) return true;
            if (this.Text != other.Text) return true;
            if (this.Outline.IsChanged(other.Outline)) return true;
            if (this.OutlineWidth != other.OutlineWidth) return true;
            if (this.ShowOutline != other.ShowOutline) return true;

            return false;
        }
    }
}
