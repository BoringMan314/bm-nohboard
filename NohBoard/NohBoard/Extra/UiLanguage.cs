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
    public static class UiLanguageCode
    {
        public const string EnUs = "en_US";
        public const string JaJp = "ja_JP";
        public const string ZhTw = "zh_TW";
        public const string ZhCn = "zh_CN";

        public static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return ZhTw;

            var v = value.Trim().Replace('-', '_');
            return v.ToUpperInvariant() switch
            {
                "EN_US" => EnUs,
                "JA_JP" => JaJp,
                "ZH_TW" => ZhTw,
                "ZH_CN" => ZhCn,
                _ => ZhTw,
            };
        }
    }
}
