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

namespace ThoNohT.NohBoard
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;

    public static class Constants
    {
        public const int KeyboardVersion = 2;

        public const string KeyboardsFolder = "keyboards";

        public const string GlobalStylesFolder = "global";

        public const string DefinitionFilename = "keyboard.json";

        public const string ImagesFolder = "images";

        public const string DefaultLoadedCategory = "Input Overlay";

        public const string DefaultLoadedKeyboard = "Input overlay layout";

        public const string DefaultLoadedStyle = "Input overlay style v2";

        public const int DefaultElementSize = 40;

        public static Graphics G => Graphics.FromHwndInternal(new Form().Handle);

        public const string AppId = "bm-nohboard";

        public const string ProcessName = "bm-nohboard";

        public static string SettingsFilename => $"{AppId}.json";

        public const string RepositoryUrl = "https://github.com/BoringMan314/bm-nohboard";

        public const string ReleasesUrl = "https://github.com/BoringMan314/bm-nohboard/releases";

        public static string SettingsFilePath =>
            Path.Combine(string.IsNullOrEmpty(ExePath) ? AppDomain.CurrentDomain.BaseDirectory : ExePath, SettingsFilename);

        public static string ExePath => Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        public static Brush HighlightBrush = new SolidBrush(Color.FromArgb(80, 0, 180, 255));

        public static Color SelectedColor = Color.DarkMagenta;

        public static Color SelectedColorSpecial = Color.OrangeRed;
    }
}
