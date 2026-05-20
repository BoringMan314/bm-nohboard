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
    using System.Collections.Generic;
    using System.Drawing;
    using System.Runtime.Serialization;

    [DataContract(Name = "Font", Namespace = "")]
    public class SerializableFont
    {
        public SerializableFont()
        {
        }

        public SerializableFont(Font font, string downloadUrl)
        {
            this.FontFamily = font.FontFamily.Name;
            this.Style = (SerializableFontStyle)font.Style;
            this.Size = font.Size;
            this.DownloadUrl = downloadUrl;
        }

        [DataMember]
        public string FontFamily { get; set; }

        public string AlternateFontFamily { get; set; }

        public string UsedFontFamily => this.AlternateFontFamily ?? this.FontFamily;

        [DataMember]
        public float Size { get; set; }

        [DataMember]
        public SerializableFontStyle Style { get; set; }

        [DataMember]
        public string DownloadUrl { get; set; }

        public static implicit operator Font(SerializableFont src)
        {
            return new Font(new FontFamily(src.UsedFontFamily), src.Size, (FontStyle)src.Style);
        }

        public static implicit operator SerializableFont(Font src)
        {
            return new SerializableFont
            {
                FontFamily = src.FontFamily.Name,
                Size = src.Size,
                Style = (SerializableFontStyle)src.Style
            };
        }

        public SerializableFont Clone()
        {
            return (SerializableFont)this.MemberwiseClone();
        }

        public bool IsChanged(SerializableFont other)
        {
            return this.FontFamily != other.FontFamily ||
                this.AlternateFontFamily != other.AlternateFontFamily ||
                this.DownloadUrl != other.DownloadUrl;
        }

        public class FamilyComparer : IEqualityComparer<SerializableFont>
        {
            public bool Equals(SerializableFont x, SerializableFont y)
            {
                return Equals(x.FontFamily, y.FontFamily);
            }

            public int GetHashCode(SerializableFont obj)
            {
                return obj.FontFamily.GetHashCode();
            }
        }
    }

    [DataContract(Name = "FontStyle", Namespace = "")]
    public enum SerializableFontStyle
    {
        [EnumMember]
        Regular = 0,

        [EnumMember]
        Bold = 1,

        [EnumMember]
        Italic = 2,

        [EnumMember]
        Underline = 4,

        [EnumMember]
        Strikeout = 8
    }
}
