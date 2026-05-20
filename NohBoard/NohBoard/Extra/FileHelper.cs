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
    using System.IO;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    public static class FileHelper
    {
        public static void EnsurePathExists(string path)
        {
            var file = new FileInfo(path);
            var dir = file.Directory;

            if (dir.Exists) return;

            EnsurePathExists(dir.Parent.FullName);
            dir.Create();
        }

        public static T Deserialize<T>(string filename) where T: class
        {
            if (!File.Exists(filename)) return null;

            using (var fileStream = new FileStream(filename, FileMode.Open))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(
                    fileStream,
                    Encoding.UTF8,
                    XmlDictionaryReaderQuotas.Max,
                    dictionaryReader => { }))
                {
                    return (T)serializer.ReadObject(reader);
                }
            }
        }

        public static void Serialize<T>(string filename, T obj)
        {
            using (var fileStream = new FileStream(filename, FileMode.Create))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(fileStream, Encoding.UTF8, true, true))
                {
                    serializer.WriteObject(writer, obj);
                }
            }
        }

        public static string Serialize<T>(T obj)
        {
            using (var memoryStream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(memoryStream, Encoding.UTF8, true, true))
                {
                    serializer.WriteObject(writer, obj);
                }

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

        public static string SanitizeFilename(this string filename)
        {
            return filename == null ? null : Regex.Replace(filename, @"[^\w\d\.\-_~]", "");
        }

        public static DirectoryInfo FromKbs(params string[] parts)
        {
            var array = new List<string> { Constants.ExePath, Constants.KeyboardsFolder };
            array.AddRange(parts);
            return new DirectoryInfo(Path.Combine(array.ToArray()));
        }

        public static bool StyleImageExists(string fileName)
        {
            return File.Exists(GetStyleImagePath(fileName));
        }

        public static string GetStyleImagePath(string filename)
        {
            var s = GlobalSettings.Settings;
            return Path.Combine(
                s.LoadedGlobalStyle
                    ? FromKbs(Constants.GlobalStylesFolder, Constants.ImagesFolder).FullName
                    : FromKbs(s.LoadedCategory, Constants.ImagesFolder).FullName,
                filename);
        }
    }
}
