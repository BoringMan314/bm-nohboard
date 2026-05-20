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

    public static class ImageCache
    {
        private static Dictionary<string, Image> Buffer { get; } = new Dictionary<string, Image>();

        public static void Clear()
        {
            foreach (var image in Buffer.Values)
                image.Dispose();

            Buffer.Clear();
        }

        public static Image Get(string filename)
        {
            var specificPath = FileHelper.GetStyleImagePath(filename);
            if (!Buffer.ContainsKey(specificPath))
                Buffer.Add(specificPath, Image.FromFile(specificPath));

            return Buffer[specificPath];
        }
    }
}
