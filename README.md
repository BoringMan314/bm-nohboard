# [B.M] NohBoard

[Platform](#系統需求)
[.NET](https://dotnet.microsoft.com/download/dotnet/8.0)
[GitHub](https://github.com/BoringMan314/bm-nohboard)
[Upstream](https://github.com/ThoNohT/NohBoard)
[![GitHub all releases](https://img.shields.io/github/downloads/BoringMan314/bm-nohboard/total)](https://github.com/BoringMan314/bm-nohboard/releases)
[License: MIT](LICENSE)

Windows 鍵盤／滑鼠 overlay 可視化工具：在螢幕上顯示按鍵與滑鼠操作，適合直播、錄影與 OBS 視窗擷取。

*Windows 键盘／鼠标 overlay 可视化工具：在屏幕上显示按键与鼠标操作，适合直播、录影与 OBS 窗口捕获。*

*Windows 向けキーボード／マウスオーバーレイ。配信・録画向け。*

*Windows keyboard/mouse overlay for streaming and recording.*

> **聲明**：本專案為 [ThoNohT/NohBoard](https://github.com/ThoNohT/NohBoard) 之**獨立發行 fork**，與上游 `NohBoard.exe`／`NohBoard.json` **不相容**（單一實例、設定檔皆分開）。與遊戲或平台官方無關。

---

NohBoard overlay、系統匣與設定示意程式畫面示意

---

## 目錄

- [功能](#功能)
- [系統需求](#系統需求)
- [安裝方式](#安裝方式)
- [建置與發布](#建置與發布)
- [技術概要](#技術概要)
- [專案結構](#專案結構)
- [版本與多語系](#版本與多語系)
- [與 `master` 分支的差異](#與-master-分支的差異)
- [致謝](#致謝)
- [維護者：更新 GitHub](#維護者更新-github)
- [授權](#授權)
- [問題與建議](#問題與建議)

---

## 功能

- 即時顯示鍵盤按鍵與滑鼠操作（低階 Hook + overlay 繪製）。
- **單一實例**：app id `**bm-nohboard*`*（mutex、`\\.\pipe\bm-nohboard`，見 `Extra/SingleInstanceGuard.cs`）。
- **系統匣**：最小化到匣、顯示主視窗、**overlay 鎖定**、設定、關於（連結本倉庫 Releases）、結束。
- **多語系介面**：`zh_TW`、`zh_CN`、`ja_JP`、`en_US`（設定中切換；預設 **zh_TW**）。
- **overlay**：鍵盤縮放、透明度、鎖定穿透；預設主題 **Input Overlay**；內建 **Tutorial** 等。
- **設定**：重置、**套用**、確定；寫入 exe 旁 `**bm-nohboard.json`**。
- **更新檢查**：對 [BoringMan314/bm-nohboard Releases](https://github.com/BoringMan314/bm-nohboard/releases)。
- 可匯入舊版 `**.kb`**；載入鍵盤對話框支援字型連結與**重新啟動**。
- 介面**不使用**滑鼠懸停 tooltip。

---

## 系統需求

- **Windows 10 或更新版本**（x64）。
- Releases 為 **self-contained**，一般不需另裝 .NET。
- 自編譯需 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。

---

## 安裝方式

### 從 Releases（建議）

1. 開啟 [Releases](https://github.com/BoringMan314/bm-nohboard/releases)（`**main` 發行**時標籤如 **V1.4.0**）。
2. 下載 `**bm-nohboard_windows_x64-Vx.x.x.zip`**，解壓後保持 `**bm-nohboard.exe**` 與 `**keyboards\**` 同層。
3. 執行 `**bm-nohboard.exe**`，設定存於 `**bm-nohboard.json**`。

> 勿與 `**master**` 發行的 `NohBoard.exe`／`NohBoard.json` 混用。

### 從原始碼

```powershell
git clone https://github.com/BoringMan314/bm-nohboard.git
cd bm-nohboard
git checkout main
build_win10.bat
```

產出見 `dist\bm-nohboard_windows_x64-V*.zip`。

---

## 建置與發布

```bat
build_win10.bat
```

- 需 `**NohBoard\gotri.exe**`。
- 版本字串自 `[Version.cs.template](NohBoard/NohBoard/Version.cs.template)` 讀取，ZIP 名稱含 `**V{major}.{minor}.{patch}**`。
- 建置前會結束 `**bm-nohboard.exe**`（若正在執行）。

---

## 技術概要


| 項目  | 說明                                               |
| --- | ------------------------------------------------ |
| UI  | .NET 8 WinForms                                  |
| 繪製  | GDI+；半透明 **layered overlay**                     |
| 輸入  | 低階鍵盤／滑鼠 Hook                                     |
| 設定  | `bm-nohboard.json`（`Constants.SettingsFilePath`） |
| 鍵盤  | `keyboards\` 目錄樹                                 |


上游 Wiki：[ThoNohT/NohBoard/wiki](https://github.com/ThoNohT/NohBoard/wiki)

---

## 專案結構


| 路徑                                               | 說明                    |
| ------------------------------------------------ | --------------------- |
| `[NohBoard/NohBoard.sln](NohBoard/NohBoard.sln)` | 方案                    |
| `[NohBoard/NohBoard/](NohBoard/NohBoard/)`       | 主程式、`Forms/`、`Extra/` |
| `[NohBoard/Hooking/](NohBoard/Hooking/)`         | Hook 與輸入狀態            |
| `[keyboards/](keyboards/)`                       | 鍵盤版面與樣式               |
| `[screenshot/](screenshot/)`                     | 說明用圖                  |
| `[build_win10.bat](build_win10.bat)`             | 私有發行打包（`bm-nohboard`） |


---

## 版本與多語系

- **版本**：`[Version.cs.template](NohBoard/NohBoard/Version.cs.template)` → `Version.Get`（例如 `v1.4.0`）。
- **語系**：`UiLanguage`／`UiTranslate`；預設 **zh_TW**。
- **發布**：僅在確認後於 `**main`** 打 tag 並建立 Release（`--target main`）。

---

## 與 `master` 分支的差異


| 項目   | `**main`（本分支）**    | `**master`（PR）** |
| ---- | ------------------ | ---------------- |
| 用途   | 私有發行               | 上游 PR #222       |
| 執行檔  | `bm-nohboard.exe`  | `NohBoard.exe`   |
| 設定檔  | `bm-nohboard.json` | `NohBoard.json`  |
| 預設語系 | zh_TW              | en_US            |
| 更新來源 | 本倉庫 Releases       | 上游／PR 流程         |


功能修正請先合入 `**master`**，再 cherry-pick／合併私有化到 `**main**`。

---

## 致謝

- **原作者**：Eric「ThoNohT」Bataille — [ThoNohT/NohBoard](https://github.com/ThoNohT/NohBoard)
- 其他貢獻者與鍵盤社群見上游專案說明。

---

## 維護者：更新 GitHub

```powershell
git checkout main
git add .
git commit -m "V1.4.0"
git push origin main
```

Release：`gh release create ... --target main`，只上傳 `**bm-nohboard_windows_x64-*.zip**`；**Source code** 用 GitHub 自建。

---

## 授權

本倉庫發行包裝以 **[MIT License](LICENSE)** 為準。  
所基於之 NohBoard 原始碼依各檔 **[GPL-2.0](https://github.com/ThoNohT/NohBoard/blob/master/LICENSE)** 標頭授權；散布衍生作品時請遵守 GPL 規定。

---

## 問題與建議

歡迎至 [GitHub Issues](https://github.com/BoringMan314/bm-nohboard/issues) 回報。請註明 `**main`**、版本號與重現步驟。