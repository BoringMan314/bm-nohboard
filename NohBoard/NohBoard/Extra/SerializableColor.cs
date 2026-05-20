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
    using System.Drawing;
    using System.Runtime.Serialization;

    [DataContract(Name = "Color", Namespace = "")]
    public class SerializableColor
    {
        [DataMember]
        public byte Red { get; set; }

        [DataMember]
        public byte Green { get; set; }

        [DataMember]
        public byte Blue { get; set; }

        public static implicit operator Color(SerializableColor src)
        {
            return Color.FromArgb(src.Red, src.Green, src.Blue);
        }

        public static implicit operator SerializableColor(Color src)
        {
            return new SerializableColor
            {
                Red = src.R,
                Green = src.G,
                Blue = src.B
            };
        }

        public SerializableColor Clone()
        {
            return (SerializableColor)this.MemberwiseClone();
        }

        public bool IsChanged(SerializableColor other)
        {
            return this.Red != other.Red || this.Green != other.Green || this.Blue != other.Blue;
        }
    }

}
