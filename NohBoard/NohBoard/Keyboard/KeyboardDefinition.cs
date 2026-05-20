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
    using System.Text.RegularExpressions;
    using ElementDefinitions;
    using Extra;

    [DataContract(Name = "Keyboard", Namespace = "")]
    public class KeyboardDefinition
    {
        #region Properties

        public string Name { get; set; }

        public string Category { get; set; }

        [DataMember]
        public int Version { get; set; }

        [DataMember]
        public int Width { get; set; }

        [DataMember]
        public int Height { get; set; }

        [DataMember]
        public List<ElementDefinition> Elements { get; set; }

        #endregion Properties

        #region Modification

        public KeyboardDefinition RemoveElement(ElementDefinition element)
        {
            var newElements = this.Elements.Where(e => e.Id != element.Id).ToList();
            if (newElements.Count != this.Elements.Count - 1)
                throw new Exception($"Keyboard contains no, or too many elements with id {element.Id}.");

            return new KeyboardDefinition
            {
                Category = this.Category,
                Elements = newElements,
                Width = this.Width,
                Height = this.Height,
                Name = this.Name,
                Version = this.Version
            };
        }

        public KeyboardDefinition MoveElementDown(ElementDefinition element, int diff)
        {
            if (!this.Elements.Contains(element)) throw new Exception("Attempting to move a non-existent element.");
            var index = this.Elements.IndexOf(element);

            if (diff == int.MaxValue) diff = int.MaxValue - index;
            if (index + diff < 0) diff = -index;
            if (index + diff >= this.Elements.Count) diff = this.Elements.Count - index - 1;

            var newPosition = index + diff;
            var oldElements = this.Elements.Except(element.Singleton()).ToList();

            var newElements = Enumerable.Range(0, this.Elements.Count)
                .Select(i => i < newPosition ? oldElements[i] : (i == newPosition ? element : oldElements[i - 1]))
                .ToList();

            return new KeyboardDefinition
            {
                Category = this.Category,
                Elements = newElements,
                Width = this.Width,
                Height = this.Height,
                Name = this.Name,
                Version = this.Version
            };
        }

        public KeyboardDefinition AddElement(ElementDefinition element, int? index = null)
        {
            if (this.Elements.Any(e => e.Id == element.Id))
                throw new Exception($"Keyboard already contains an element with id {element.Id}.");

            List<ElementDefinition> newElements;
            if (index == null)
            {
                newElements = this.Elements.Union(element.Singleton()).ToList();
            }
            else
            {
                newElements = Enumerable.Range(0, this.Elements.Count + 1)
                    .Select(i => i < index ? this.Elements[i] : (i == index ? element : this.Elements[i - 1]))
                    .ToList();
            }

            return new KeyboardDefinition
            {
                Category = this.Category,
                Elements = newElements,
                Width = this.Width,
                Height = this.Height,
                Name = this.Name,
                Version = this.Version
            };
        }

        public KeyboardDefinition Resize(Size newSize)
        {
            return new KeyboardDefinition
            {
                Category = this.Category,
                Elements = this.Elements.Select(x => x.Clone()).ToList(),
                Width = newSize.Width,
                Height = newSize.Height,
                Name = this.Name,
                Version = this.Version
            };
        }

        public KeyboardDefinition Clone()
        {
            return new KeyboardDefinition
            {
                Category = this.Category,
                Elements = this.Elements.Select(x => x.Clone()).ToList(),
                Width = this.Width,
                Height = this.Height,
                Name = this.Name,
                Version = this.Version
            };
        }

        public bool IsChanged(KeyboardDefinition other)
        {
            if (this.Category != other.Category) return true;
            if (this.Width != other.Width) return true;
            if (this.Height != other.Height) return true;

            if (!this.Elements.Select(e => e.Id).ToSet().SetEquals(other.Elements.Select(e => e.Id)))
                return true;

            var otherElements = other.Elements.ToDictionary(e => e.Id);

            return this.Elements.Any(e => e.IsChanged(otherElements[e.Id]));
        }

        public int GetNextId()
        {
            return this.Elements.Select(e => e.Id).DefaultIfEmpty(0).Max() + 1;
        }

        #endregion Modification

        public Rectangle GetBoundingBox()
        {
            var minX = this.Elements.Select(x => x.GetBoundingBox().X).Min();
            var minY = this.Elements.Select(x => x.GetBoundingBox().Y).Min();

            return new Rectangle(
                new Point(minX, minY),
                new Size(
                    this.Elements.Select(x => x.GetBoundingBox().Right).Max() - minX,
                    this.Elements.Select(x => x.GetBoundingBox().Bottom).Max() - minY));
        }

        public void Save()
        {
            var filename = Path.Combine(
                Constants.ExePath,
                Constants.KeyboardsFolder,
                this.Category,
                this.Name,
                Constants.DefinitionFilename);

            FileHelper.EnsurePathExists(filename);
            FileHelper.Serialize(filename, this);

            GlobalSettings.UnsavedDefinitionChanges = false;
        }

        public static KeyboardDefinition Load(string category, string name) => Load(category, name, null);

        public static KeyboardDefinition Load(string category, string name, string styleName)
        {
            var categoryPath = FileHelper.FromKbs(category);
            if (!categoryPath.Exists)
                throw new ArgumentException($"Category {category} does not exist.");

            var keyboardPath = Path.Combine(categoryPath.FullName, name);
            if (!Directory.Exists(keyboardPath))
                throw new ArgumentException($"Keyboard {name} does not exist.");

            var filePath = ResolveDefinitionFilePath(keyboardPath, styleName);
            if (!File.Exists(filePath))
                throw new Exception($"Keyboard definition file not found for {category}/{name}.");

            var readLines = File.ReadLines(filePath);
            var versionLine = readLines.SingleOrDefault(l => l.Contains("\"Version\": "));
            if (versionLine == null) throw new Exception("Keyboard does not contain version information.");
            int version;
            try
            {
                version = int.Parse(
                    versionLine.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries).Last().TrimEnd(','));
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to determine version info.", ex);
            }

            if (version < Constants.KeyboardVersion)
            {
                throw new Exception(
                    $"This version of NohBoard requires keyboards of version {Constants.KeyboardVersion}, " +
                    $"but this keyboard is of version {version}.");
            }

            var kbDef = FileHelper.Deserialize<KeyboardDefinition>(filePath);

            var elementIds = kbDef.Elements.Select(e => e.Id);
            if (elementIds.Count() != elementIds.Distinct().Count())
                throw new Exception("Not all element ids are unique in this keyboard definition.");

            kbDef.Category = category;
            kbDef.Name = name;
            return kbDef;
        }

        private static string ResolveDefinitionFilePath(string keyboardPath, string styleName)
        {
            var defaultPath = Path.Combine(keyboardPath, Constants.DefinitionFilename);
            if (string.IsNullOrWhiteSpace(styleName))
                return defaultPath;

            var trimmed = styleName.Trim();
            var match = Regex.Match(trimmed, @"\bv(\d+)$", RegexOptions.IgnoreCase);
            if (!match.Success)
                return defaultPath;

            var variantPath = Path.Combine(keyboardPath, $"keyboard.v{match.Groups[1].Value}.json");
            return File.Exists(variantPath) ? variantPath : defaultPath;
        }
    }
}
