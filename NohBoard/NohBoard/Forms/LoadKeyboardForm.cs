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
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;
    using Extra;
    using ThoNohT.NohBoard.Hooking.Interop;
    using ThoNohT.NohBoard.Keyboard;
    using ThoNohT.NohBoard.Legacy;

    /// <summary>
    /// The form that is used to load keyboards and styles.
    /// </summary>
    public partial class LoadKeyboardForm : Form
    {
        #region Fields

        /// <summary>
        /// The name of the currently selected category.
        /// </summary>
        private string SelectedCategory => (string)this.CategoryCombo.SelectedItem;

        /// <summary>
        /// The name of the currently selected definition.
        /// </summary>
        private string SelectedDefinition => (string)this.DefinitionsList.SelectedItem;

        /// <summary>
        /// Information about the currently selected style.
        /// </summary>
        private StyleInfo SelectedStyle => (StyleInfo)this.StyleList.SelectedItem;

        /// <summary>
        /// A value indicating whether the form has completed loading.
        /// </summary>
        private bool loaded = false;

        /// <summary>
        /// While repopulating the style list, skip <see cref="StyleList_SelectedIndexChanged"/> (preview runs from the definition handler).
        /// </summary>
        private bool styleListProgrammaticChange;

        /// <summary>Avoid re-entrancy when column widths trigger layout.</summary>
        private bool fontsGridApplyingColumnWidths;

        /// <summary>
        /// The list of global styles, this is constant regardless of the loaded styles for specific keyboards.
        /// </summary>
        private List<StyleInfo> globalStyles;

        /// <summary>
        /// A helper class with information about the currently selected style.
        /// </summary>
        private class StyleInfo
        {
            /// <summary>
            /// Indicates whether the style is global.
            /// </summary>
            public bool Global { get; set; }

            /// <summary>
            /// The name of the style.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Returns a string that represents the current object.
            /// </summary>
            /// <returns>A string that represents the current object.</returns>
            public override string ToString()
            {
                return this.Global ? $"global: {this.Name}" : this.Name;
            }
        }

        /// <summary>
        /// A helper class for filling the missing fonts table.
        /// </summary>
        private class MissingFont
        {
            /// <summary>
            /// The name of the font.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The link to download the font.
            /// </summary>
            public string Link { get; set; }
        }

        #endregion Fields

        #region Events

        /// <summary>
        /// The event that is invoked when the style has been changed. Only invoked when the style is changed through
        /// the user interface, not when it is changed programmatically.
        /// </summary>
        public event Action<KeyboardDefinition, KeyboardStyle, bool> DefinitionChanged;

        #endregion Events

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadKeyboardForm" /> class.
        /// </summary>
        public LoadKeyboardForm()
        {
            this.InitializeComponent();
            this.fontsGrid.Resize += this.fontsGrid_ColumnWidthsResize;
        }

        #endregion Constructors

        /// <summary>
        /// Shows or hides the font download panel depending on wether there are missing fonts in the currently loaded
        /// style.
        /// </summary>
        /// <param name="missingFonts"></param>
        public void ToggleFontsPanel(List<SerializableFont> missingFonts)
        {
            if (!missingFonts.Any())
            {
                // Hide the panel: client width = middle column right (195+180) + 10 px — same control sizes as designer, no stretching.
                this.ClientSize = new System.Drawing.Size(385, 336);
                this.fontsGrid.Enabled = false;
                this.btnRestart.Enabled = false;
                this.lblMissingFonts.Visible = false;
                this.fontsGridPanel.Visible = false;
                this.lblRestart.Visible = false;
                this.btnRestart.Visible = false;
            }
            else
            {
                // Client width = fontsGridPanel right (380+600) + 10 margin — do not use Form.Width here or borders inflate the client area.
                this.ClientSize = new System.Drawing.Size(990, 336);
                this.fontsGrid.Enabled = true;
                this.btnRestart.Enabled = true;
                this.lblMissingFonts.Visible = true;
                this.fontsGridPanel.Visible = true;
                this.lblRestart.Visible = true;
                this.btnRestart.Visible = true;

                var L = UiTranslate.Lang;
                var gridData = missingFonts
                    .Select(f => new MissingFont
                    {
                        Name = f.FontFamily,
                        Link = f.DownloadUrl ?? UiTranslate.T(L, "No download URL provided", "未提供下載連結", "未提供下载链接", "ダウンロード URL がありません")
                    })
                    .ToList();
                this.fontsGrid.DataSource = gridData;
                this.ApplyFontsGridColumnWidthsAfterBind();
                this.ApplyFontsGridColumnHeaders();

                this.fontsGrid.Update();

            }
        }

        /// <summary>
        /// Name column fixed width; link column uses remaining grid width (unchanged behavior aside from name width).
        /// </summary>
        private void ApplyFontsGridColumnWidthsAfterBind()
        {
            if (this.fontsGrid.Columns.Count < 2 || this.fontsGridApplyingColumnWidths)
                return;

            this.fontsGridApplyingColumnWidths = true;
            try
            {
                const int nameColumnWidth = 140;
                this.fontsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                this.fontsGrid.Columns[0].MinimumWidth = 80;
                this.fontsGrid.Columns[0].Width = nameColumnWidth;

                this.fontsGrid.Columns[1].MinimumWidth = 120;
                var displayWidth = this.fontsGrid.DisplayRectangle.Width;
                var linkWidth = displayWidth - nameColumnWidth - 3;
                if (linkWidth < this.fontsGrid.Columns[1].MinimumWidth)
                    linkWidth = this.fontsGrid.Columns[1].MinimumWidth;
                this.fontsGrid.Columns[1].Width = linkWidth;
            }
            finally
            {
                this.fontsGridApplyingColumnWidths = false;
            }
        }

        private void fontsGrid_ColumnWidthsResize(object sender, EventArgs e)
        {
            this.ApplyFontsGridColumnWidthsAfterBind();
        }

        private void ApplyFontsGridColumnHeaders()
        {
            if (this.fontsGrid.Columns.Count < 2)
                return;

            var L = UiTranslate.Lang;
            this.fontsGrid.Columns[0].HeaderText = UiTranslate.T(L, "Name", "名稱", "名称", "名前");
            this.fontsGrid.Columns[1].HeaderText = UiTranslate.T(L, "Link", "連結", "链接", "リンク");
        }

        /// <summary>
        /// Loads a legacy keyboard definition. This immediately closes the form and loads it in the main form.
        /// </summary>
        private void LoadLegacyButton_Click(object sender, System.EventArgs e)
        {
            var L = UiTranslate.Lang;
            var dialog = new OpenFileDialog
            {
                Filter = UiTranslate.T(L, "Legacy Keyboard Files|*.kb", "舊版鍵盤檔|*.kb", "旧版键盘文件|*.kb", "レガシーキーボード|*.kb")
            };
            var result = HookManager.RunModalUi(() => dialog.ShowDialog());

            if (result == DialogResult.Cancel) return;

            this.DefinitionChanged?.Invoke(LegacyKbFileParser.Parse(dialog.FileName), null, false);
            this.Close();
        }

        /// <summary>
        /// Loads the keyboard form, filling the controls with the found categories, definitions and styles.
        /// </summary>
        private void LoadKeyboardForm_Load(object sender, System.EventArgs e)
        {
            this.ApplyLocalizedLoadKeyboardTexts();

            // Load the list of global styles
            var globalStylesRoot = FileHelper.FromKbs(Constants.GlobalStylesFolder);

            if (!globalStylesRoot.Exists)
                globalStylesRoot.Create();

            this.globalStyles = globalStylesRoot.EnumerateFiles()
                .Where(x => x.Extension == KeyboardStyle.StyleExtension)
                .Select(
                    x => new StyleInfo
                    {
                        Global = true,
                        Name = x.Name.Substring(0, x.Name.Length - KeyboardStyle.StyleExtension.Length)
                    })
                .ToList();

            var root = FileHelper.FromKbs();

            // If there are no keyboard files, no initialization is required.
            if (!root.Exists) return;

            this.CategoryCombo.Items.Clear();
            this.CategoryCombo.Items.AddRange(
                root.EnumerateDirectories()
                    .Select(x => (object)x.Name).Where(x => (string)x != Constants.GlobalStylesFolder).ToArray());

            if (GlobalSettings.Settings.LoadedCategory != null)
            {
                this.CategoryCombo.SelectedItem = GlobalSettings.Settings.LoadedCategory;
                this.PopulateKeyboards();

                if (GlobalSettings.Settings.LoadedKeyboard != null)
                    this.DefinitionsList.SelectedItem = GlobalSettings.Settings.LoadedKeyboard;
            }

            this.loaded = true;
            this.ApplyPreviewSelection();
        }

        /// <summary>
        /// Handles a selection change in the category combo, re-populates the list of keyboards and styles.
        /// </summary>
        private void CategoryCombo_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            this.PopulateKeyboards();

            if (this.loaded)
                this.ApplyPreviewSelection();
        }

        /// <summary>
        /// Closes the form.
        /// </summary>
        private void CloseButton_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Deletes the selected keyboard definition and all of its styles. Re-initializes the form to clear the
        /// style from the lists.
        /// </summary>
        private void mnuDeleteDefinition_Click(object sender, System.EventArgs e)
        {
            if (this.SelectedDefinition == null) return;

            var L = UiTranslate.Lang;
            var result = MessageBox.Show(
                string.Format(
                    UiTranslate.T(
                        L,
                        "Are you sure you want to delete {0}/{1}?",
                        "確定要刪除 {0}/{1} 嗎？",
                        "确定要删除 {0}/{1} 吗？",
                        "{0}/{1} を削除しますか？"),
                    this.SelectedCategory,
                    this.SelectedDefinition),
                UiTranslate.T(L, "Delete definition", "刪除定義", "删除定义", "定義の削除"),
                MessageBoxButtons.YesNo);

            if (result != DialogResult.Yes)
                return;

            FileHelper.FromKbs(this.SelectedCategory, this.SelectedDefinition).Delete(true);
            this.LoadKeyboardForm_Load(null, null);
        }

        /// <summary>
        /// Handles a selection change in the definition list, re-populates the styles list for this definition.
        /// Loads the definition in the main form.
        /// </summary>
        private void DefinitionsList_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            try
            {
                this.LoadStyles();
                this.ApplyPreviewSelection();
            }
            catch (Exception ex)
            {
                this.ShowPreviewLoadError(ex, this.SelectedDefinition);
            }
        }

        /// <summary>
        /// Handles a selection change in the styles list, loading that style in the main form.
        /// </summary>
        private void StyleList_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (this.styleListProgrammaticChange)
                return;

            try
            {
                this.ApplyPreviewSelection();
            }
            catch (Exception ex)
            {
                this.ShowPreviewLoadError(ex, this.SelectedStyle?.Name ?? this.SelectedDefinition);
            }
        }

        /// <summary>
        /// Pushes the current category/definition/style selection to the main form preview.
        /// </summary>
        private void ApplyPreviewSelection()
        {
            if (!this.loaded || this.SelectedDefinition == null)
                return;

            if (this.StyleList.Items.Count == 0)
            {
                this.DefinitionChanged?.Invoke(
                    KeyboardDefinition.Load(this.SelectedCategory, this.SelectedDefinition, null),
                    null,
                    false);
                return;
            }

            if (this.SelectedStyle == null)
                return;

            this.DefinitionChanged?.Invoke(
                KeyboardDefinition.Load(
                    this.SelectedCategory,
                    this.SelectedDefinition,
                    this.SelectedStyle.Name),
                KeyboardStyle.Load(this.SelectedStyle.Name, this.SelectedStyle.Global),
                this.SelectedStyle.Global);
        }

        private void ShowPreviewLoadError(Exception ex, string itemName)
        {
            var L = UiTranslate.Lang;
            MessageBox.Show(
                string.Format(
                    UiTranslate.T(
                        L,
                        "Failed to load keyboard {0}: {1}",
                        "載入鍵盤 {0} 失敗：{1}",
                        "加载键盘 {0} 失败：{1}",
                        "キーボード {0} の読み込みに失敗しました：{1}"),
                    itemName,
                    ex.Message));
        }

        /// <summary>
        /// Handles a double click on the link cell. Will open the browser if it contains an URL.
        /// </summary>
        private void fontsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var value = this.fontsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            var linkText = value as string ?? value?.ToString();

            if (string.IsNullOrWhiteSpace(linkText))
                return;

            linkText = linkText.Trim();
            if (!Uri.TryCreate(linkText, UriKind.Absolute, out var uri))
                return;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return;

            Process.Start(new ProcessStartInfo { FileName = uri.AbsoluteUri, UseShellExecute = true });
        }

        #region Helpers

        private void ApplyLocalizedLoadKeyboardTexts()
        {
            var L = UiTranslate.Lang;

            this.Text = UiTranslate.T(L, "Load Keyboard", "載入鍵盤", "加载键盘", "キーボードを読み込む");
            this.lblCategory.Text = UiTranslate.T(L, "Category:", "分類：", "类别：", "カテゴリ：");
            this.lblKeyboardDefinition.Text = UiTranslate.T(L, "Keyboard Definition:", "鍵盤定義：", "键盘定义：", "キーボード定義：");
            this.lblKeyboardStyle.Text = UiTranslate.T(L, "Keyboard Style:", "鍵盤樣式：", "键盘样式：", "キーボードスタイル：");
            this.LoadLegacyButton.Text = UiTranslate.T(L, "Load Legacy kb file...", "載入舊版鍵盤檔...", "加载旧版键盘文件...", "レガシー .kb ファイルを読み込む...");
            this.CloseButton.Text = UiTranslate.T(L, "Close", "關閉", "关闭", "閉じる");
            this.lblMissingFonts.Text = UiTranslate.T(
                L,
                "The following fonts are defined in the chosen style but not present on this system.\r\nIf a link is provided, you may download it by double clicking the link:",
                "下列字型在所選樣式中有使用，但此電腦未安裝。\r\n若有連結，請連按兩下連結以下載：",
                "下列字体在所选样式中有使用，但此电脑未安装。\r\n若有链接，请双击链接以下载：",
                "選択したスタイルで使われている次のフォントが、この PC にありません。\r\nリンクがある場合は、リンクをダブルクリックしてダウンロードできます：");
            this.lblRestart.Text = UiTranslate.T(
                L,
                "After a new font has been installed, NohBoard needs to be restarted to recognize it.",
                "安裝新字型後，需要重新啟動 NohBoard 才會辨識。",
                "安装新字体后，需要重新启动 NohBoard 才会识别。",
                "新しいフォントをインストールした後、NohBoard を再起動すると認識されます。");
            this.btnRestart.Text = UiTranslate.T(L, "Restart", "重新啟動", "重新启动", "再起動");
            this.mnuDeleteDefinition.Text = UiTranslate.T(L, "Delete", "刪除", "删除", "削除");
        }

        /// <summary>
        /// Populates the list of keyboards, for the currently selected category.
        /// </summary>
        private void PopulateKeyboards()
        {
            var root = FileHelper.FromKbs(this.SelectedCategory);
            if (!root.Exists) return;

            this.DefinitionsList.Items.Clear();
            this.DefinitionsList.Items.AddRange(
                root.EnumerateDirectories()
                    .Where(x => File.Exists(Path.Combine(x.FullName, Constants.DefinitionFilename)))
                    .Select(x => (object)x.Name)
                    .ToArray());

            // If the form is still loading, don't set the selected index.
            if (this.DefinitionsList.Items.Count > 0 && this.loaded)
                this.DefinitionsList.SelectedIndex = 0;

            this.LoadStyles();
        }

        /// <summary>
        /// Populates the list of styles for the currently selected keyboard definition.
        /// </summary>
        private void LoadStyles()
        {
            if (this.SelectedDefinition == null)
                return;

            var specificStylesRoot = FileHelper.FromKbs(this.SelectedCategory, this.SelectedDefinition);

            var specificStyles = specificStylesRoot.EnumerateFiles()
                .Where(x => x.Extension == KeyboardStyle.StyleExtension)
                .Select(
                    x => new StyleInfo
                    {
                        Global = false,
                        Name = x.Name.Substring(0, x.Name.Length - KeyboardStyle.StyleExtension.Length)
                    })
                .ToList();

            this.styleListProgrammaticChange = true;
            try
            {
                this.StyleList.Items.Clear();
                this.StyleList.Items.AddRange(this.globalStyles.Cast<object>().ToArray());
                this.StyleList.Items.AddRange(specificStyles.Cast<object>().ToArray());

                if (this.StyleList.Items.Count == 0)
                    return;

                this.StyleList.SelectedIndex = 0;

                var loadedStyle = GlobalSettings.Settings.LoadedStyle;
                if (loadedStyle != null
                    && GlobalSettings.Settings.LoadedCategory == this.SelectedCategory
                    && GlobalSettings.Settings.LoadedKeyboard == this.SelectedDefinition)
                {
                    var styleIndex = this.FindStyleListIndex(loadedStyle);
                    if (styleIndex != -1)
                        this.StyleList.SelectedIndex = styleIndex;
                }
            }
            finally
            {
                this.styleListProgrammaticChange = false;
            }
        }

        /// <summary>
        /// Returns the index of an item in StyleList that has a given name.
        /// </summary>
        private int FindStyleListIndex(string styleName)
        {
            for (var i = 0; i < this.StyleList.Items.Count; i++)
            {
                var item = (StyleInfo)this.StyleList.Items[i];
                if (item.Name == styleName)
                {
                    return i;
                }
            }

            return -1;
        }

        #endregion Helpers

        /// <summary>
        /// Restarts the application.
        /// </summary>
        private void btnRestart_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }
    }
}
