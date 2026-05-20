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
    using System;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows.Forms;
    using Hooking;
    using Keyboard;
    using static Hooking.Interop.Defines;

    [DataContract(Name = "GlobalSettings", Namespace = "")]
    public partial class GlobalSettings
    {
        public const int CurrentSettingsVersion = 3;

        private int? updateInterval;

        public static bool UnsavedDefinitionChanges { get; set; }

        public static bool UnsavedStyleChanges { get; set; }

        public static GlobalSettings Settings { get; private set; } = new GlobalSettings();

        public static KeyboardDefinition CurrentDefinition { get; private set; }

        public static KeyboardStyle CurrentStyle { get; private set; } = new KeyboardStyle();

        public static string Errors { get; set; }

        public static int StyleDependencyCounter { get; set; } = 0;

        #region General

        [DataMember]
        public int SettingsVersion { get; set; } = CurrentSettingsVersion;

        [DataMember]
        public string WindowTitle { get; set; } = "";

        [DataMember]
        public string UiLanguage { get; set; } = UiLanguageCode.ZhTw;

        #endregion General

        #region Input

        [DataMember]
        public int MouseSensitivity { get; set; } = 50;

        [DataMember]
        public int ScrollHold { get; set; } = 50;

        [DataMember]
        public bool MouseFromCenter { get; set; }

        [DataMember]
        public int PressHold { get; set; }

        [DataMember]
        public int UpdateInterval
        {
            get => this.updateInterval ?? 33;
            set => this.updateInterval = Math.Max(5, Math.Min(60000, value));
        }

        #endregion Input

        #region Trapping

        [DataMember]
        public bool TrapKeyboard { get; set; }

        [DataMember]
        public bool TrapMouse { get; set; }

        [DataMember]
        public int TrapToggleKeyCode { get; set; } = VK_SCROLL;

        [DataMember]
        public int OverlayTransparencyPercent { get; set; } = 0;

        [DataMember]
        public int KeyboardScalePercent { get; set; } = 100;

        #endregion Trapping

        #region Capitalization

        [DataMember]
        public CapitalizationMethod Capitalization { get; set; } = CapitalizationMethod.FollowKeys;

        [DataMember]
        public bool FollowShiftForCapsInsensitive { get; set; }

        [DataMember]
        public bool FollowShiftForCapsSensitive { get; set; }

        #endregion Capitalization

        #region State

        [DataMember]
        public string LoadedCategory { get; set; } = Constants.DefaultLoadedCategory;

        [DataMember]
        public string LoadedKeyboard { get; set; } = Constants.DefaultLoadedKeyboard;

        [DataMember]
        public string LoadedStyle { get; set; } = Constants.DefaultLoadedStyle;

        [DataMember]
        public bool LoadedGlobalStyle { get; set; }

        #endregion State

        #region Editing

        [DataMember]
        public bool UpdateTextPosition = true;

        #endregion Editing

        #region Methods

        public static void Save()
        {
            var path = Constants.SettingsFilePath;
            FileHelper.EnsurePathExists(path);
            FileHelper.Serialize(path, Settings);
        }

        private static string ResolveSettingsPathForLoad()
        {
            var primary = Constants.SettingsFilePath;
            if (File.Exists(primary))
                return primary;

            var cwdPrimary = Path.Combine(Environment.CurrentDirectory, Constants.SettingsFilename);
            if (File.Exists(cwdPrimary))
                return cwdPrimary;

            return null;
        }

        public static bool Load()
        {
            var path = ResolveSettingsPathForLoad();
            var createdNew = path == null;

            if (createdNew)
            {
                Settings = new GlobalSettings();
            }
            else
            {
                try
                {
                    Settings = FileHelper.Deserialize<GlobalSettings>(path);
                }
                catch (Exception ex)
                {
                    Errors = ex.Message;
                    Settings = new GlobalSettings();
                    createdNew = true;
                }
            }

            var needsSave = Settings.NormalizeAfterLoad() || createdNew;
            ApplyLoadedSettingsSideEffects();

            if (needsSave)
                TryPersistSettings();

            return string.IsNullOrEmpty(Errors);
        }

        public bool NormalizeAfterLoad()
        {
            var changed = false;

            if (this.SettingsVersion < CurrentSettingsVersion)
            {
                if (this.SettingsVersion < 2)
                    this.OverlayTransparencyPercent = 0;

                this.SettingsVersion = CurrentSettingsVersion;
                changed = true;
            }

            var overlay = OverlayTransparency.ClampPercent(this.OverlayTransparencyPercent);
            if (overlay != this.OverlayTransparencyPercent)
            {
                this.OverlayTransparencyPercent = overlay;
                changed = true;
            }

            var scale = Math.Max(25, Math.Min(300, this.KeyboardScalePercent));
            if (scale != this.KeyboardScalePercent)
            {
                this.KeyboardScalePercent = this.KeyboardScalePercent <= 0 ? 100 : scale;
                changed = true;
            }

            var lang = UiLanguageCode.Normalize(this.UiLanguage);
            if (lang != this.UiLanguage)
            {
                this.UiLanguage = lang;
                changed = true;
            }

            if (string.IsNullOrEmpty(this.LoadedCategory) || string.IsNullOrEmpty(this.LoadedKeyboard))
            {
                this.LoadedCategory = Constants.DefaultLoadedCategory;
                this.LoadedKeyboard = Constants.DefaultLoadedKeyboard;
                this.LoadedStyle = Constants.DefaultLoadedStyle;
                this.LoadedGlobalStyle = false;
                changed = true;
            }

            return changed;
        }

        private static void ApplyLoadedSettingsSideEffects()
        {
            Func<Rectangle, Point> getCenter = r => r.Location + new Size(r.Width / 2, r.Height / 2);
            MouseState.SetMouseFromCenter(
                Settings.MouseFromCenter,
                Screen.AllScreens.Select(x => (x.Bounds, getCenter(x.Bounds))).ToList());
        }

        internal static void TryPersistSettings()
        {
            try
            {
                Save();
            }
            catch
            {
            }
        }

        #endregion Methods
    }
}
