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

namespace ThoNohT.NohBoard.Keyboard
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using Extra;
    using Styles;

    [DataContract(Name = "KeyboardStyle", Namespace = "")]
    public class KeyboardStyle
    {
        private string FileName => this.Name + StyleExtension;

        public const string StyleExtension = ".style";

        public string Name { get; set; } = "unnamed";

        public bool IsGlobal => !this.ElementStyles.Any();

        #region The keyboard itself

        [DataMember]
        public SerializableColor BackgroundColor { get; set; } = Color.FromArgb(0, 0, 100);

        [DataMember]
        public string BackgroundImageFileName { get; set; }

        #endregion The keyboard itself

        #region Defaults for elements

        [DataMember]
        public KeyStyle DefaultKeyStyle { get; set; } = new KeyStyle();

        [DataMember]
        public MouseSpeedIndicatorStyle DefaultMouseSpeedIndicatorStyle { get; set; } = new MouseSpeedIndicatorStyle();

        #endregion Defaults for elements

        [DataMember]
        public Dictionary<int, ElementStyle> ElementStyles { get; set; } = new Dictionary<int, ElementStyle>();

        public KeyboardStyle Clone()
        {
            return new KeyboardStyle
            {
                Name = this.Name,
                BackgroundColor = this.BackgroundColor,
                BackgroundImageFileName = this.BackgroundImageFileName,
                DefaultKeyStyle = (KeyStyle) this.DefaultKeyStyle.Clone(),
                DefaultMouseSpeedIndicatorStyle =
                    (MouseSpeedIndicatorStyle) this.DefaultMouseSpeedIndicatorStyle.Clone(),
                ElementStyles = this.ElementStyles.Select(s => (s.Key, s.Value.Clone()))
                    .ToDictionary(s => s.Item1, s => s.Item2)
            };
        }

        public bool ElementIsStyled(int elementId)
        {
            return this.ElementStyles.ContainsKey(elementId);
        }

        public KeyboardStyle SetElementStyle(int id, ElementStyle style)
        {
            var result = this.Clone();
            if (this.ElementStyles.TryGetValue(id, out _))
            {
                result.ElementStyles[id] = style.Clone();
                return result;
            }
            else
            {
                result.ElementStyles.Add(id, style.Clone());
                return result;
            }
        }

        public KeyboardStyle RemoveElementStyle(int id)
        {
            var result = this.Clone();
            result.ElementStyles.Remove(id);
            return result;
        }

        public bool IsChanged(KeyboardStyle other)
        {
            if (this.BackgroundColor.IsChanged(other.BackgroundColor)) return true;
            if (this.BackgroundImageFileName != other.BackgroundImageFileName) return true;
            if (this.BackgroundColor != other.BackgroundColor) return true;
            if (this.DefaultKeyStyle.IsChanged(other.DefaultKeyStyle)) return true;
            if (this.DefaultMouseSpeedIndicatorStyle.IsChanged(other.DefaultMouseSpeedIndicatorStyle)) return true;

            if (!this.ElementStyles.Keys.ToSet().SetEquals(other.ElementStyles.Keys)) return true;

            return this.ElementStyles.Any(e => e.Value.IsChanged(other.ElementStyles[e.Key]));
        }

        public void Save(bool global)
        {
            if (global && !this.IsGlobal)
                throw new InvalidOperationException("Cannot save a non-global style globally.");

            var cDef = GlobalSettings.CurrentDefinition;
            var filename = global
                ? FileHelper.FromKbs(Constants.GlobalStylesFolder, this.FileName).FullName
                : FileHelper.FromKbs(cDef.Category, cDef.Name, this.FileName).FullName;

            FileHelper.EnsurePathExists(filename);
            FileHelper.Serialize(filename, this);

            GlobalSettings.UnsavedStyleChanges = false;
        }

        public static KeyboardStyle Load(string name, bool global)
        {
            var cDef = GlobalSettings.CurrentDefinition;
            var filePath = global
                ? FileHelper.FromKbs(Constants.GlobalStylesFolder, $"{name}{StyleExtension}").FullName
                : FileHelper.FromKbs(cDef.Category, cDef.Name, $"{name}{StyleExtension}").FullName;

            var currentPath = global ? Constants.GlobalStylesFolder : $"{cDef.Category}/{cDef.Name}";

            if (!File.Exists(filePath))
                throw new Exception($"Style file not found for {currentPath}/{name}.");

            var kbStyle = FileHelper.Deserialize<KeyboardStyle>(filePath);

            kbStyle.Name = name;
            return kbStyle;
        }

        public static void CopyDefinitionStyleToGlobal(
            string category,
            string definitionName,
            string sourceStyleName,
            string targetGlobalStyleName)
        {
            if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException(nameof(category));
            if (string.IsNullOrWhiteSpace(definitionName)) throw new ArgumentException(nameof(definitionName));
            if (string.IsNullOrWhiteSpace(sourceStyleName)) throw new ArgumentException(nameof(sourceStyleName));
            if (string.IsNullOrWhiteSpace(targetGlobalStyleName)) throw new ArgumentException(nameof(targetGlobalStyleName));

            var targetName = targetGlobalStyleName.Trim();

            var srcPath = FileHelper.FromKbs(category, definitionName, $"{sourceStyleName}{StyleExtension}").FullName;
            if (!File.Exists(srcPath))
                throw new FileNotFoundException($"Style file not found: {srcPath}");

            var loaded = FileHelper.Deserialize<KeyboardStyle>(srcPath);
            if (loaded == null)
                throw new InvalidOperationException("Could not read the style file.");

            var copy = loaded.Clone();
            copy.Name = targetName;

            var dstPath = FileHelper.FromKbs(Constants.GlobalStylesFolder, $"{targetName}{StyleExtension}").FullName;
            FileHelper.EnsurePathExists(dstPath);
            FileHelper.Serialize(dstPath, copy);

            CopyReferencedStyleImagesFromCategoryToGlobal(category, copy);
        }

        private static void CollectImageFileNames(KeySubStyle sub, ISet<string> into)
        {
            if (sub == null || string.IsNullOrWhiteSpace(sub.BackgroundImageFileName)) return;
            into.Add(sub.BackgroundImageFileName.Trim());
        }

        private static void CollectImageFileNames(KeyStyle keyStyle, ISet<string> into)
        {
            if (keyStyle == null) return;
            CollectImageFileNames(keyStyle.Loose, into);
            CollectImageFileNames(keyStyle.Pressed, into);
        }

        private static void CollectReferencedStyleImages(KeyboardStyle style, ISet<string> into)
        {
            if (!string.IsNullOrWhiteSpace(style.BackgroundImageFileName))
                into.Add(style.BackgroundImageFileName.Trim());

            CollectImageFileNames(style.DefaultKeyStyle, into);

            foreach (var pair in style.ElementStyles)
            {
                if (pair.Value is KeyStyle ks)
                    CollectImageFileNames(ks, into);
            }
        }

        private static void CopyReferencedStyleImagesFromCategoryToGlobal(string category, KeyboardStyle style)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectReferencedStyleImages(style, names);
            if (names.Count == 0) return;

            var srcRoot = FileHelper.FromKbs(category, Constants.ImagesFolder).FullName;
            var dstRoot = FileHelper.FromKbs(Constants.GlobalStylesFolder, Constants.ImagesFolder).FullName;
            if (!Directory.Exists(srcRoot)) return;

            foreach (var name in names)
            {
                var src = Path.Combine(srcRoot, name);
                var dst = Path.Combine(dstRoot, name);
                if (!File.Exists(src)) continue;

                FileHelper.EnsurePathExists(dst);
                File.Copy(src, dst, overwrite: true);
            }
        }

        public T TryGetElementStyle<T>(int id) where T: ElementStyle
        {
            var success = this.ElementStyles.TryGetValue(id, out var style);

            if (!success) return null;

            return style as T;
        }
    }
}
