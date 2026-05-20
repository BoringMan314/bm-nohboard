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

namespace ThoNohT.NohBoard.Forms
{
    using Extra;
    using Hooking;
    using Hooking.Interop;
    using Keyboard;
    using Keyboard.ElementDefinitions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Drawing.Text;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using ThoNohT.NohBoard.Keyboard.Styles;
    using Version = NohBoard.Version;

    public partial class MainForm : Form
    {
        #region Fields

        private readonly Dictionary<bool, Dictionary<bool, Brush>> backBrushes =
            new Dictionary<bool, Dictionary<bool, Brush>>();

        private ElementDefinition elementUnderCursor = null;

        private VersionInfo latestVersion = null;

        #endregion Fields

        #region Constructors

        public MainForm()
        {
            this.InitializeComponent();
            this.ApplyExecutableIconAsWindowIcon();
            this.HandleCreated += (_, _) => this.ApplyTaskSwitcherIconicPreviewPreference();
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this._keyboardSurface.HandleCreated += (_, _) =>
            {
                if (!this._overlayLocked)
                    this.ApplyOverlayTransparencyStyle();
            };
            this.InitializeTray();
            this.InitializeOverlayLock();
            this.InitializeLayeredOverlay();
        }

        #endregion Constructors

        private void ApplyExecutableIconAsWindowIcon()
        {
            try
            {
                var exe = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
                    return;

                using (var extracted = Icon.ExtractAssociatedIcon(exe))
                {
                    if (extracted == null)
                        return;
                    this.Icon = (Icon)extracted.Clone();
                }
            }
            catch
            {
            }
        }

        #region Version check

        private void StartLatestVersionCheck()
        {
            _ = this.CheckLatestVersionAsync();
        }

        private async Task CheckLatestVersionAsync()
        {
            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(Constants.AppId);
                    var json = await client.GetStringAsync(
                        "https://api.github.com/repos/BoringMan314/bm-nohboard/releases/latest")
                        .ConfigureAwait(false);

                    using (var doc = JsonDocument.Parse(json))
                    {
                        if (!doc.RootElement.TryGetProperty("tag_name", out var tagProp))
                            return;

                        var tag = tagProp.GetString();
                        if (string.IsNullOrWhiteSpace(tag) || !TryParseReleaseTag(tag, out var versionInfo))
                            return;

                        var isNewer =
                            versionInfo.Major > Version.Major
                            || (versionInfo.Major == Version.Major && versionInfo.Minor > Version.Minor)
                            || (versionInfo.Major == Version.Major && versionInfo.Minor == Version.Minor
                                && versionInfo.Patch > Version.Patch);

                        if (!isNewer)
                            return;

                        void applyOnUi()
                        {
                            if (this.IsDisposed)
                                return;
                            this.latestVersion = versionInfo;
                            this.ApplyLocalizedMainMenu();
                        }

                        if (this.IsDisposed)
                            return;

                        if (this.InvokeRequired)
                            this.BeginInvoke(new Action(applyOnUi));
                        else
                            applyOnUi();
                    }
                }
            }
            catch
            {
            }
        }

        private static bool TryParseReleaseTag(string tag, out VersionInfo versionInfo)
        {
            versionInfo = null;
            var t = tag.Trim();
            if (t.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                t = t.Substring(1);

            var parts = t.Split('.');
            if (parts.Length < 3)
                return false;

            if (!int.TryParse(parts[0], out var major)
                || !int.TryParse(parts[1], out var minor)
                || !int.TryParse(parts[2], out var patch))
                return false;

            versionInfo = new VersionInfo { Major = major, Minor = minor, Patch = patch };
            return true;
        }

        #endregion Version check

        #region Keyboard loading and saving

        private void ReloadDefinitionForLoadedStyle()
        {
            var category = GlobalSettings.Settings.LoadedCategory;
            var keyboard = GlobalSettings.Settings.LoadedKeyboard;
            if (category == null || keyboard == null)
                return;

            GlobalSettings.Settings.UpdateDefinition(
                KeyboardDefinition.Load(category, keyboard, GlobalSettings.Settings.LoadedStyle),
                false);
        }

        private List<SerializableFont> LoadKeyboard()
        {
            if (GlobalSettings.CurrentDefinition == null)
            {
                HookManager.DisableKeyboardHook();
                HookManager.DisableMouseHook();

                this.ResetBackBrushes();
                this.ApplyOverlayTransparencyStyle();
                return new List<SerializableFont>();
            }
            if (GlobalSettings.CurrentDefinition.Elements.Any(x => !(x is KeyboardKeyDefinition)))
                HookManager.EnableMouseHook();
            else
                HookManager.DisableMouseHook();

            if (GlobalSettings.CurrentDefinition.Elements.Any(x => x is KeyboardKeyDefinition))
                HookManager.EnableKeyboardHook();
            else
                HookManager.DisableKeyboardHook();

            var missingFonts = this.CheckMissingFonts();

            GlobalSettings.Settings.InitUndoHistory();

            if (this.mnuToggleEditMode.Checked)
            {
                this.mnuToggleEditMode.Checked = false;
                this.mnuToggleEditMode_Click(null, null);
            }

            this.currentlyManipulating = null;
            this.highlightedDefinition = null;
            this.selectedDefinition = null;

            this.ResetBackBrushes();
            this.RefreshKeyboardDisplayAfterDefinitionChange();

            return missingFonts;
        }

        private void RefreshKeyboardDisplayAfterDefinitionChange()
        {
            if (this.UsesLayeredOverlay())
            {
                this._layeredBitmap?.Dispose();
                this._layeredBitmap = null;
            }

            this.ApplyKeyboardWindowLayout();

            if (this.UsesLayeredOverlay())
            {
                this.EnsureLayeredOverlayForm();
                this.SyncLayeredOverlayBounds();
                this.PresentLayeredOverlay();
            }
            else
                this._keyboardSurface?.Invalidate();

            this.ApplyOverlayTransparencyStyle();
        }

        private List<SerializableFont> CheckMissingFonts()
        {
            var style = GlobalSettings.CurrentStyle;
            var usedFonts = style.ElementStyles.Values.OfType<KeyStyle>()
                .SelectMany(s => new[] { s.Loose?.Font, s.Pressed?.Font })
                .Union(new[] { style.DefaultKeyStyle?.Loose?.Font, style.DefaultKeyStyle?.Pressed?.Font })
                .Where(f => f != null).ToList();

            var installedFonts = new InstalledFontCollection();
            var installedFontFamilyNames = new HashSet<string>(installedFonts.Families.Select(f => f.Name));
            var notInstalledUsedFonts = usedFonts.Where(f => !installedFontFamilyNames.Contains(f.FontFamily)).ToList();

            foreach (var font in notInstalledUsedFonts)
            {
                font.AlternateFontFamily = SystemFonts.DefaultFont.FontFamily.Name;
            }

            if (!notInstalledUsedFonts.Any()) return new List<SerializableFont>();

            return notInstalledUsedFonts.OrderBy(f => f.DownloadUrl == null).Distinct(new SerializableFont.FamilyComparer()).ToList();
        }

        private void ResetBackBrushes()
        {
            GlobalSettings.StyleDependencyCounter++;
            ImageCache.Clear();

            var hideFrameAndFill = OverlayTransparency.HidesFrameAndFill(
                GlobalSettings.Settings?.OverlayTransparencyPercent ?? 0);

            foreach (var brush in this.backBrushes)
            {
                foreach (var b in brush.Value)
                    b.Value.Dispose();

                brush.Value.Clear();
            }
            this.backBrushes.Clear();

            if (hideFrameAndFill)
            {
                this.InvalidateKeyboardSurface(immediate: true);
                return;
            }

            var definition = GlobalSettings.CurrentDefinition;
            if (definition == null || definition.Width <= 0 || definition.Height <= 0)
                return;

            var keyboardBounds = new Rectangle(0, 0, definition.Width, definition.Height);

            foreach (var shift in new[] { false, true })
            {
                this.backBrushes.Add(shift, new Dictionary<bool, Brush>());

                foreach (var caps in new[] { false, true })
                {
                    var bmp = new Bitmap(
                        definition.Width,
                        definition.Height,
                        PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Transparent);

                        var cs = GlobalSettings.CurrentStyle;
                        if (cs.BackgroundImageFileName != null
                            && FileHelper.StyleImageExists(cs.BackgroundImageFileName))
                        {
                            OverlayTransparency.DrawImage(
                                g,
                                ImageCache.Get(cs.BackgroundImageFileName),
                                keyboardBounds,
                                GlobalSettings.Settings.OverlayTransparencyPercent);
                        }

                        foreach (var def in definition.Elements)
                        {
                            if (def is KeyboardKeyDefinition)
                                ((KeyboardKeyDefinition)def).Render(g, false, shift, caps);

                            if (def is MouseKeyDefinition)
                                ((MouseKeyDefinition)def).Render(g, false, shift, caps);

                            if (def is MouseScrollDefinition)
                                ((MouseScrollDefinition)def).Render(g, 0);

                        }
                    }

                    this.backBrushes[shift].Add(caps, new TextureBrush(bmp));
                }
            }

            this.InvalidateKeyboardSurface(immediate: true);
        }

        private void mnuLoadKeyboard_Click(object sender, EventArgs e)
        {
            if (HasUnsavedChangesPendingUserPrompt())
            {
                var result = this.ShowAppMessageBox(
                    UiTranslate.T(
                        "You have unsaved changes. Loading a new keyboard will undo them. Are you sure you want to load a new keyboard?",
                        "有尚未儲存的變更。載入新鍵盤將放棄這些變更。確定要載入嗎？",
                        "有未保存的更改。加载新键盘将放弃这些更改。确定要加载吗？",
                        "未保存の変更があります。新しいキーボードを読み込むと破棄されます。読み込みますか？"),
                    UiTranslate.T("Discard changes", "放棄變更", "放弃更改", "変更の破棄"),
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.OK) return;
            }

            this.menuOpen = false;

            using (var manageForm = new LoadKeyboardForm())
            {
                manageForm.DefinitionChanged += (kbDef, kbStyle, globalStyle) =>
                {
                    var backupDef = GlobalSettings.CurrentDefinition;
                    var backupStyle = GlobalSettings.CurrentStyle;

                    var backupCat = GlobalSettings.Settings.LoadedCategory;
                    var backupKb = GlobalSettings.Settings.LoadedKeyboard;
                    var backupKbStyle = GlobalSettings.Settings.LoadedStyle;
                    var backupkbGlobalStyle = GlobalSettings.Settings.LoadedGlobalStyle;

                    GlobalSettings.Settings.UpdateDefinition(kbDef, false);
                    GlobalSettings.Settings.UpdateStyle(kbStyle ?? new KeyboardStyle(), false);

                    GlobalSettings.Settings.LoadedCategory = kbDef.Category;
                    GlobalSettings.Settings.LoadedKeyboard = kbDef.Name;
                    GlobalSettings.Settings.LoadedStyle = kbStyle?.Name;
                    GlobalSettings.Settings.LoadedGlobalStyle = globalStyle;

                    try
                    {
                        var missingFonts = this.LoadKeyboard();
                        manageForm.ToggleFontsPanel(missingFonts);
                    }
                    catch (Exception ex)
                    {
                        GlobalSettings.Settings.UpdateDefinition(backupDef, false);
                        GlobalSettings.Settings.UpdateStyle(backupStyle, false);

                        GlobalSettings.Settings.LoadedCategory = backupCat;
                        GlobalSettings.Settings.LoadedKeyboard = backupKb;
                        GlobalSettings.Settings.LoadedStyle = backupKbStyle;
                        GlobalSettings.Settings.LoadedGlobalStyle = backupkbGlobalStyle;

                        this.LoadKeyboard();

                        this.ShowAppMessageBox(
                            ex.Message + Environment.NewLine
                            + UiTranslate.T(
                                "Reverted keyboard change.",
                                "已還原鍵盤變更。",
                                "已还原键盘更改。",
                                "キーボードの変更を元に戻しました。"),
                            UiTranslate.T("Error", "錯誤", "错误", "エラー"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                };

                this.ShowAppModalDialog(manageForm);
            }
        }

        private void mnuSaveDefinitionAsName_Click(object sender, EventArgs e)
        {
            this.menuOpen = false;

            GlobalSettings.CurrentDefinition.Save();
            GlobalSettings.Settings.LoadedCategory = GlobalSettings.CurrentDefinition.Category;
            GlobalSettings.Settings.LoadedKeyboard = GlobalSettings.CurrentDefinition.Name;
        }

        private void mnuSaveDefinitionAs_Click(object sender, EventArgs e)
        {
            this.menuOpen = false;

            using (var saveForm = new SaveKeyboardAsForm())
                this.ShowAppModalDialog(saveForm);
        }

        #endregion Keyboard loading and saving

        #region Settings

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (!GlobalSettings.Load())
            {
                this.ShowAppMessageBox(
                    $"{UiTranslate.T("Failed to load the settings:", "無法載入設定：", "无法加载设置：", "設定を読み込めません：")} {GlobalSettings.Errors}",
                    UiTranslate.T("Failed to load settings", "載入設定失敗", "加载设置失败", "設定の読み込み失敗"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            FormPlacement.MoveMainFormToDefaultPosition(this);
            var title = GlobalSettings.Settings.WindowTitle;

            this.Text = string.IsNullOrWhiteSpace(title) ? Version.DefaultTitle : title;

            this.StartLatestVersionCheck();

            if (GlobalSettings.Settings.LoadedKeyboard != null && GlobalSettings.Settings.LoadedCategory != null)
            {
                try
                {
                    GlobalSettings.Settings.UpdateDefinition(KeyboardDefinition.Load(
                        GlobalSettings.Settings.LoadedCategory,
                        GlobalSettings.Settings.LoadedKeyboard,
                        GlobalSettings.Settings.LoadedStyle), false);
                }
                catch (Exception ex)
                {
                    this.ShowAppMessageBox(
                        UiTranslate.T(
                            "There was an error loading the saved keyboard definition file:",
                            "載入已儲存的鍵盤定義檔時發生錯誤：",
                            "加载已保存的键盘定义文件时出错：",
                            "保存されたキーボード定義の読み込み中にエラーが発生しました：")
                        + $"{Environment.NewLine}{ex.Message}",
                        UiTranslate.T("Error", "錯誤", "错误", "エラー"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    GlobalSettings.Settings.LoadedCategory = null;
                    GlobalSettings.Settings.LoadedKeyboard = null;
                }
            }

            var savedStyleName = GlobalSettings.Settings.LoadedStyle;
            if (GlobalSettings.CurrentDefinition != null && savedStyleName != null)
            {
                try
                {
                    GlobalSettings.Settings.UpdateStyle(KeyboardStyle.Load(
                        savedStyleName,
                        GlobalSettings.Settings.LoadedGlobalStyle), false);
                    this.ReloadDefinitionForLoadedStyle();
                }
                catch (Exception ex)
                {
                    GlobalSettings.Settings.LoadedStyle = null;
                    this.ShowAppMessageBox(
                        string.Format(
                            UiTranslate.T(
                                "Failed to load style {0}, loading default style.",
                                "無法載入樣式 {0}，改載入預設樣式。",
                                "无法加载样式 {0}，正在加载默认样式。",
                                "スタイル {0} を読み込めません。既定のスタイルを読み込みます。"),
                            savedStyleName)
                        + Environment.NewLine
                        + ex.Message,
                        UiTranslate.T("Error loading style.", "載入樣式錯誤", "加载样式错误", "スタイル読み込みエラー"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }

            this.UpdateTimer.Interval = GlobalSettings.Settings.UpdateInterval;
            this.UpdateTimer.Enabled = true;
            this.KeyCheckTimer.Enabled = true;

            this.Activate();
            this.ApplySettings();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this._exitingApp && !CrashHandler.Crashed && e.CloseReason == CloseReason.UserClosing)
                this._exitingApp = true;

            if (!this._exitingFromPeer && !this.TryConfirmDiscardUnsaved())
            {
                e.Cancel = true;
                this._exitingApp = false;
                return;
            }

            this.ReleaseOverlayLock();
            this.DisposeLayeredOverlay();
            this.DisposeTray();

            HookManager.DisableMouseHook();
            HookManager.DisableKeyboardHook();

            try
            {
                GlobalSettings.Save();
            }
            catch (Exception ex)
            {
                if (!this._exitingFromPeer)
                {
                    this.ShowAppMessageBox(
                        $"無法儲存設定檔：{Environment.NewLine}{ex.Message}",
                        "NohBoard",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        internal void ApplySettings()
        {
            HookManager.TrapKeyboard = GlobalSettings.Settings.TrapKeyboard;
            HookManager.TrapMouse = GlobalSettings.Settings.TrapMouse;
            HookManager.TrapToggleKeyCode = GlobalSettings.Settings.TrapToggleKeyCode;
            HookManager.ScrollHold = GlobalSettings.Settings.ScrollHold;
            HookManager.PressHold = GlobalSettings.Settings.PressHold;

            var title = GlobalSettings.Settings.WindowTitle;
            this.Text = string.IsNullOrWhiteSpace(title) ? Version.DefaultTitle : title;
            if (this._trayIcon != null)
                this._trayIcon.Text = this.Text;

            this.LoadKeyboard();
            this.ApplyLocalizedMainMenu();
        }

        private void ApplyLocalizedMainMenu()
        {
            var L = UiTranslate.Lang;

            this.ApplyLocalizedOverlayLockMenuText();
            this.mnuSettings.Text = UiTranslate.T(L, "&Settings", "設定(&S)", "设置(&S)", "設定(&S)");
            this.mnuKeyboards.Text = UiTranslate.T(L, "&Load Keyboard", "載入鍵盤(&L)", "加载键盘(&L)", "キーボードを読み込む(&L)");
            this.mnuUpdateTextPosition.Text = UiTranslate.T(L, "&Update Text Position", "更新文字位置(&U)", "更新文字位置(&U)", "テキスト位置を更新(&U)");
            this.mnuMoveElement.Text = UiTranslate.T(L, "&Move", "移動(&M)", "移动(&M)", "移動(&M)");
            this.mnuMoveToTop.Text = UiTranslate.T(L, "Move to &top", "移到最上", "移到最上", "最前面へ");
            this.mnuMoveUp.Text = UiTranslate.T(L, "Move &up", "上移", "上移", "上へ");
            this.mnuMoveDown.Text = UiTranslate.T(L, "Move &down", "下移", "下移", "下へ");
            this.mnuMoveToBottom.Text = UiTranslate.T(L, "Move to &bottom", "移到最下", "移到最下", "最背面へ");
            this.mnuAddElement.Text = UiTranslate.T(L, "&Add Element", "新增元素(&A)", "添加元素(&A)", "要素を追加(&A)");
            this.mnuAddKeyboardKeyDefinition.Text = UiTranslate.T(L, "Add &Keyboard Key", "新增鍵盤按鍵", "添加键盘按键", "キーボードキーを追加");
            this.mnuAddMouseKeyDefinition.Text = UiTranslate.T(L, "Add &Mouse Key", "新增滑鼠按鍵", "添加鼠标按键", "マウスキーを追加");
            this.mnuAddMouseScrollDefinition.Text = UiTranslate.T(L, "Add Mouse &Scroll", "新增滑鼠捲動", "添加鼠标滚动", "マウススクロールを追加");
            this.mnuAddMouseSpeedIndicatorDefinition.Text = UiTranslate.T(L, "Add Mouse Speed &Indicator", "新增滑鼠速度指示", "添加鼠标速度指示", "マウス速度インジケーターを追加");
            this.mnuRemoveElement.Text = UiTranslate.T(L, "&Remove Element", "移除元素(&R)", "移除元素(&R)", "要素を削除(&R)");
            this.mnuAddBoundaryPoint.Text = UiTranslate.T(L, "Add Boundary Point", "新增邊界點", "添加边界点", "境界点を追加");
            this.mnuRemoveBoundaryPoint.Text = UiTranslate.T(L, "Remove Boundary Point", "移除邊界點", "移除边界点", "境界点を削除");
            this.mnuKeyboardProperties.Text = UiTranslate.T(L, "Keyboard &Properties", "鍵盤屬性(&P)", "键盘属性(&P)", "キーボードのプロパティ(&P)");
            this.mnuElementProperties.Text = UiTranslate.T(L, "Element &Properties", "元素屬性(&P)", "元素属性(&P)", "要素のプロパティ(&P)");
            this.mnuEditKeyboardStyle.Text = UiTranslate.T(L, "Keyboard &Style", "鍵盤樣式(&S)", "键盘样式(&S)", "キーボードスタイル(&S)");
            this.mnuEditElementStyle.Text = UiTranslate.T(L, "Element &Style", "元素樣式(&S)", "元素样式(&S)", "要素スタイル(&S)");
            this.mnuSaveDefinition.Text = UiTranslate.T(L, "Save &Definition", "儲存定義(&D)", "保存定义(&D)", "定義を保存(&D)");
            this.mnuSaveDefinitionAs.Text = UiTranslate.T(L, "Save &As", "另存新檔(&A)", "另存为(&A)", "名前を付けて保存(&A)");
            this.mnuSaveStyle.Text = UiTranslate.T(L, "Save St&yle", "儲存樣式(&Y)", "保存样式(&Y)", "スタイルを保存(&Y)");
            this.mnuSaveStyleAs.Text = UiTranslate.T(L, "Save &As", "另存新檔(&A)", "另存为(&A)", "名前を付けて保存(&A)");
            this.mnuExit.Text = UiTranslate.T(L, "E&xit", "結束(&X)", "退出(&X)", "終了(&X)");
            this.mnuGenerateLog.Text = UiTranslate.T(L, "Generate crash log", "產生當機記錄", "生成崩溃日志", "クラッシュログを生成");

            this.ApplyLocalizedTrayMenu();
            this.ApplyLocalizedEditModeToggleText();
        }

        private void ApplyLocalizedOverlayLockMenuText()
        {
            var L = UiTranslate.Lang;
            this.mnuToggleOverlayLock.Text = this._overlayLocked
                ? UiTranslate.T(L, "&Unlock", "解除鎖定(&U)", "解除锁定(&U)", "ロック解除(&U)")
                : UiTranslate.T(L, "&Lock", "鎖定(&L)", "锁定(&L)", "ロック(&L)");
        }

        private void ApplyLocalizedEditModeToggleText()
        {
            var L = UiTranslate.Lang;
            this.mnuToggleEditMode.Text = this.mnuToggleEditMode.Checked
                ? UiTranslate.T(L, "Stop Editing", "停止編輯", "停止编辑", "編集を終了")
                : UiTranslate.T(L, "Start &Editing", "開始編輯(&E)", "开始编辑(&E)", "編集を開始(&E)");
        }

        private void mnuSettings_Click(object sender, EventArgs e) => this.OpenSettingsDialog();

        private void OpenSettingsDialog()
        {
            this.menuOpen = false;

            DialogResult result;
            using (var settingsForm = new SettingsForm())
                result = this.ShowAppModalDialog(settingsForm);

            if (result == DialogResult.OK)
                this.ApplySettings();
        }

        private void MainMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.menuOpen = true;
            this.ApplyLocalizedOverlayLockMenuText();

            this.mnuSaveDefinition.Enabled = GlobalSettings.CurrentDefinition != null;
            if (GlobalSettings.CurrentDefinition != null)
            {
                this.mnuSaveDefinitionAsName.Text = string.Format(
                    UiTranslate.T(
                        "Save &To '{0}'",
                        "儲存至「{0}」(&T)",
                        "保存到「{0}」(&T)",
                        "「{0}」に保存(&T)"),
                    $"{GlobalSettings.CurrentDefinition.Category}/{GlobalSettings.CurrentDefinition.Name}");

                var mousePos = this.GetContextMenuClientPoint();
                this.elementUnderCursor =
                    GlobalSettings.CurrentDefinition.Elements.FirstOrDefault(x => x.Inside(mousePos));

                if (this.mnuToggleEditMode.Checked && this.selectedDefinition == null)
                {
                    this.highlightedDefinition = this.elementUnderCursor;
                    this.highlightedDefinition?.StartManipulating(mousePos, false);
                }

                var relevantElement = this.selectedDefinition ?? this.elementUnderCursor;
                this.mnuEditElementStyle.Enabled = relevantElement != null;
                this.mnuElementProperties.Enabled = relevantElement != null;
            }

            this.mnuKeyboardProperties.Visible = this.mnuToggleEditMode.Checked;
            this.mnuUpdateTextPosition.Visible = this.mnuToggleEditMode.Checked;
            this.mnuElementProperties.Visible = this.mnuToggleEditMode.Checked;
            this.mnuEditKeyboardStyle.Visible = this.mnuToggleEditMode.Checked;
            this.mnuEditElementStyle.Visible = this.mnuToggleEditMode.Checked;
            this.MainMenuSep1.Visible = this.mnuToggleEditMode.Checked;

            this.mnuSaveStyleToName.Text = string.Format(
                UiTranslate.T("Save &To '{0}'", "儲存至「{0}」(&T)", "保存到「{0}」(&T)", "「{0}」に保存(&T)"),
                GlobalSettings.CurrentStyle.Name);
            this.mnuSaveStyleToName.Visible = !GlobalSettings.Settings.LoadedGlobalStyle;
            this.mnuSaveToGlobalStyleName.Text = string.Format(
                UiTranslate.T(
                    "Save To &Global '{0}'",
                    "儲存至全域樣式「{0}」",
                    "保存到全局样式「{0}」",
                    "グローバルに保存「{0}」"),
                GlobalSettings.CurrentStyle.Name);
            this.mnuSaveToGlobalStyleName.Enabled = GlobalSettings.CurrentStyle.IsGlobal;
            this.mnuSaveToGlobalStyleName.Visible = GlobalSettings.Settings.LoadedGlobalStyle;

            this.mnuToggleEditMode.Enabled = GlobalSettings.CurrentDefinition != null;

            if (this.latestVersion != null && !this.mnuUpdate.Visible)
            {
                this.mnuUpdate.Text = string.Format(
                    UiTranslate.T(
                        "New version available: {0}.",
                        "有新版本：{0}。",
                        "有可用新版本：{0}。",
                        "新しいバージョンがあります：{0}。"),
                    this.latestVersion.Format());
                this.mnuUpdate.Visible = true;
                this.mnuUpdate.Click += (s, ea) =>
                {
                    Process.Start(new ProcessStartInfo { FileName = Constants.ReleasesUrl, UseShellExecute = true });
                };
            }

            this.mnuMoveElement.Visible = this.relevantDefinition != null;

            var highlightedSomething = this.mnuToggleEditMode.Checked && this.relevantDefinition != null;

            this.mnuAddBoundaryPoint.Visible = highlightedSomething &&
                this.relevantDefinition.RelevantManipulation.Type == ElementManipulationType.MoveEdge;

            this.mnuRemoveBoundaryPoint.Visible = highlightedSomething &&
                this.relevantDefinition.RelevantManipulation.Type == ElementManipulationType.MoveBoundary;

            this.mnuRemoveElement.Visible = highlightedSomething;
            this.mnuAddElement.Visible = this.mnuToggleEditMode.Checked && this.relevantDefinition == null;
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            this.menuOpen = false;
        }

        private void mnuExit_Click(object sender, EventArgs e)
        {
            this.ExitApplication();
        }

        #endregion Settings

        #region Rendering

        private void Render(
            Graphics g,
            ElementDefinition def,
            List<ElementDefinition> allDefs,
            IReadOnlyList<int> kbKeys,
            List<int> mouseKeys,
            IReadOnlyList<int> scrollCounts,
            bool alwaysRender)
        {
            if (def is KeyboardKeyDefinition kkDef)
            {
                var pressed = true;
                if (!kkDef.KeyCodes.Any() || !kkDef.KeyCodes.All(kbKeys.Contains)) pressed = false;

                if (kkDef.KeyCodes.Count == 1
                    && allDefs.OfType<KeyboardKeyDefinition>()
                        .Any(d => d.KeyCodes.Count > 1
                        && d.KeyCodes.All(kbKeys.Contains)
                        && d.KeyCodes.ContainsAll(kkDef.KeyCodes))) pressed = false;

                if (!pressed && !alwaysRender) return;

                kkDef.Render(g, pressed, KeyboardState.ShiftDown, KeyboardState.CapsActive);
            }
            if (def is MouseKeyDefinition mkDef)
            {
                var pressed = mouseKeys.Contains(mkDef.KeyCodes.Single());
                if (pressed || alwaysRender)
                    mkDef.Render(g, pressed, KeyboardState.ShiftDown, KeyboardState.CapsActive);
            }
            if (def is MouseScrollDefinition msDef)
            {
                var scrollCount = scrollCounts[msDef.KeyCodes.Single()];
                if (scrollCount > 0 || alwaysRender) msDef.Render(g, scrollCount);
            }
            if (def is MouseSpeedIndicatorDefinition)
            {
                ((MouseSpeedIndicatorDefinition)def).Render(g, MouseState.AverageSpeed);
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (KeyboardState.Updated || MouseState.Updated)
                this.InvalidateKeyboardSurface();
        }

        private void KeyCheckTimer_Tick(object sender, EventArgs e)
        {
            MouseState.CheckKeys(GlobalSettings.Settings.PressHold);
            KeyboardState.CheckKeys(GlobalSettings.Settings.PressHold);
        }

        #endregion Rendering

        private void mnuGenerateLog_Click(object sender, EventArgs e)
        {
            if (this.ShowAppMessageBox(
                    UiTranslate.T(
                        "This will crash NohBoard in order to generate a log, are you sure you want to do this?",
                        "將會刻意讓 NohBoard 當機以產生記錄檔，確定嗎？",
                        "将故意使 NohBoard 崩溃以生成日志，确定吗？",
                        "ログ生成のため NohBoard をクラッシュさせます。よろしいですか？"),
                    UiTranslate.T("Generate crash log", "產生當機記錄", "生成崩溃日志", "クラッシュログを生成"),
                    MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                throw new Exception("A crash log was requested.");
            }
        }
    }
}
