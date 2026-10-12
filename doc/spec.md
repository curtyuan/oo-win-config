# oo-win-config Spec

## 目標

建立一個 C# + WPF 桌面工具，用來讓使用者勾選並備份指定軟體的設定檔。工具以實體複製檔案的方式管理 config，不依賴 symlink 或抽象同步層。

第一階段先支援這台機器上的 `yazi` 與 `nvim` 設定，其中 `nvim` 使用 LazyVim。後續會再討論 `yasb`、`komorebi`、`ahk` 這類互相關聯設定檔的處理方式。

## 核心需求

1. 使用 WPF 提供 GUI。
2. 使用者可以看到支援的軟體清單。
3. 使用者可以勾選要備份的軟體設定檔。
4. 每個軟體支援哪些設定檔，由開發者明確定義。
5. 備份與還原以實體複製檔案或資料夾完成。
6. 軟體來源偵測為輔助功能，例如 `winget`、`choco`。
7. 軟體版本偵測為輔助功能，偵測不到不視為錯誤。
8. 每個軟體可以關聯必要依賴，並提供安裝指令建議。
9. 提供 startup 設定入口，讓使用者把指定項目加入 Windows startup。

## 非目標

1. 第一階段不做自動雲端同步。
2. 第一階段不做即時監控檔案變更。
3. 第一階段不要求完整偵測所有套件管理器。
4. 第一階段不要求自動安裝依賴，只提供建議指令。
5. 第一階段不處理跨機器 merge 衝突。
6. 不加入 Git 功能；backup 與 restore 都以直接複製檔案完成。

## 第一序列範圍

### yazi

需要由開發者定義：

1. yazi 設定檔或設定資料夾來源路徑。
2. 備份時要複製的檔案數量與路徑。
3. 需要關聯的依賴。
4. 建議安裝指令，例如 winget、choco、scoop 或官方來源。

### nvim + LazyVim

需要由開發者定義：

1. nvim 設定資料夾來源路徑。
2. LazyVim 相關設定與 lockfile 是否納入備份。
3. 備份時要複製的檔案數量與路徑。
4. 需要關聯的依賴，例如 Git、Node.js、ripgrep、fd、lazy.nvim 相關需求。
5. 建議安裝指令。

## 軟體定義模型草案

每個軟體應該有一份開發者維護的定義，描述下列資訊：

1. 軟體 ID，例如 `yazi`、`nvim`。
2. 顯示名稱。
3. 設定檔項目清單。
4. 每個設定檔項目的來源路徑。
5. 每個設定檔項目的目標備份路徑。
6. 是否為必要項目。
7. 軟體偵測方式。
8. 版本偵測方式。
9. 依賴清單。
10. 安裝指令建議。

概念範例：

```text
SoftwareProfile
- Id: nvim
- Name: Neovim
- Sources:
  - %LOCALAPPDATA%/nvim
  - %LOCALAPPDATA%/nvim-data/lazy-lock.json
- Detection:
  - command: nvim --version
  - packageManagers: winget, choco
- Dependencies:
  - git
  - nodejs
  - ripgrep
  - fd
- SuggestedInstallCommands:
  - winget install Neovim.Neovim
  - winget install Git.Git
```

### Profile JSON Schema 設計狀態

目前 JSON schema 尚未定案。第一個需要先定義的是「每個軟體有哪些 config 要備份」。這些項目由開發者寫死在 profile JSON 中，不由程式自動猜測。

第一版 `configItems` 建議至少包含：

1. `id`：穩定識別碼，例如 `keymap`、`hotkeys`。
2. `name`：顯示名稱。
3. `kind`：`file` 或 `directory`。
4. `sourcePath`：來源路徑，可包含環境變數。
5. `backupPath`：備份內的相對路徑。
6. `required`：是否為必要項目。
7. `description`：給開發者或 UI 顯示的簡短說明。

Double Commander 初步 config item：

1. `shortcuts`：`{AppData}/doublecmd/shortcuts.scf`。

Double Commander 官方文件列出 `shortcuts.scf` 是 keyboard layout settings，也就是 hot keys 設定。第一版只備份這個檔案，不備份整個 `{AppData}/doublecmd` 目錄，避免把 history、session、tabs 等 runtime/狀態資料一起收進 backup。

Yazi 初步 config items：

1. `main-config`：`yazi.toml`。
2. `keymap`：`keymap.toml`。
3. `theme`：`theme.toml`。

先不把 plugin cache、preview cache、runtime 狀態納入第一版備份範圍。

Psmux 初步 config item：

1. `psmux-config`：預設 `{UserProfile}/.psmux.conf`。

psmux 是 Windows native 的 tmux 類 terminal multiplexer。官方文件描述它會在啟動時依序讀取第一個存在的設定檔：

1. `{UserProfile}/.psmux.conf`
2. `{UserProfile}/.psmuxrc`
3. `{UserProfile}/.tmux.conf`
4. `{UserProfile}/.config/psmux/psmux.conf`

第一版建議：

1. 預設備份 `{UserProfile}/.psmux.conf`。
2. UI 顯示其他候選路徑的偵測結果。
3. 使用者可以把 source path 改成其他候選路徑。
4. 不備份 `{UserProfile}/.psmux` runtime/data 目錄，除非之後確認其中有需要保存的可攜設定。

範例：

```json
{
  "id": "psmux",
  "name": "psmux",
  "configItems": [
    {
      "id": "psmux-config",
      "name": "psmux config",
      "kind": "file",
      "defaultSourcePath": "{UserProfile}/.psmux.conf",
      "candidateSourcePaths": [
        "{UserProfile}/.psmux.conf",
        "{UserProfile}/.psmuxrc",
        "{UserProfile}/.tmux.conf",
        "{UserProfile}/.config/psmux/psmux.conf"
      ],
      "backupPath": "psmux/psmux.conf",
      "required": false
    }
  ]
}
```

WezTerm 也是 `candidateSourcePaths` 的典型案例。常見設定檔位置包含：

1. `{UserProfile}/.wezterm.lua`
2. `{UserProfile}/.config/wezterm/wezterm.lua`

若加入 WezTerm profile，應該用同一套候選路徑機制，而不是為每個軟體寫特殊邏輯。

Tabby 初步 config item：

1. `tabby-config`：`{AppData}/tabby/config.yaml`。

範例：

```json
{
  "id": "tabby",
  "name": "Tabby",
  "configItems": [
    {
      "id": "tabby-config",
      "name": "Tabby config",
      "kind": "file",
      "defaultSourcePath": "{AppData}/tabby/config.yaml",
      "backupPath": "tabby/config.yaml",
      "required": true
    }
  ]
}
```

qutebrowser 初步 config item：

1. `qutebrowser-config-dir`：`{AppData}/qutebrowser/config`。

範例：

```json
{
  "id": "qutebrowser",
  "name": "qutebrowser",
  "configItems": [
    {
      "id": "qutebrowser-config-dir",
      "name": "qutebrowser config directory",
      "kind": "directory",
      "defaultSourcePath": "{AppData}/qutebrowser/config",
      "backupPath": "qutebrowser/config",
      "required": true
    }
  ]
}
```

Komorebi / Yasb / AHK bundle profile：

Profile ID：`komorebi-yasb-ahk`

顯示名稱：`Komorebi + Yasb + AHK`

1. `komorebi-config`：`{UserProfile}/komorebi.json`。
2. `yasb-config`：`{UserProfile}/.config/yasb/config.yaml`。
3. `yasb-styles`：`{UserProfile}/.config/yasb/styles.css`。
4. `komorebi-ahk`：`{UserProfile}/komorebi.ahk`。

目前決策：這組設定彼此有關聯，使用 bundle profile 表示，作為單一可選軟體項目顯示在 UI 中。備份、還原、版本選擇與 diff 都以這個 bundle profile 為單位。

範例：

```json
{
  "id": "komorebi-yasb-ahk",
  "name": "Komorebi + Yasb + AHK",
  "type": "bundle",
  "configItems": [
    {
      "id": "komorebi-config",
      "name": "Komorebi config",
      "kind": "file",
      "defaultSourcePath": "{UserProfile}/komorebi.json",
      "backupPath": "komorebi/komorebi.json",
      "required": true
    },
    {
      "id": "yasb-config",
      "name": "Yasb config",
      "kind": "file",
      "defaultSourcePath": "{UserProfile}/.config/yasb/config.yaml",
      "backupPath": "yasb/config.yaml",
      "required": true
    },
    {
      "id": "yasb-styles",
      "name": "Yasb styles",
      "kind": "file",
      "defaultSourcePath": "{UserProfile}/.config/yasb/styles.css",
      "backupPath": "yasb/styles.css",
      "required": true
    },
    {
      "id": "komorebi-ahk",
      "name": "Komorebi AHK script",
      "kind": "file",
      "defaultSourcePath": "{UserProfile}/komorebi.ahk",
      "backupPath": "ahk/komorebi.ahk",
      "required": true
    }
  ]
}
```

### Source Path Default 與 Override

每個 config item 都需要有 default source path。使用者可以在 UI 中修改實際來源路徑，也可以恢復 default。

需求：

1. Profile JSON 提供 `defaultSourcePath`。
2. UI 顯示目前使用中的 source path。
3. 使用者可以修改 source path。
4. 使用者可以按 `Restore default path` 回到 profile JSON 定義的 default。
5. 修改後的 source path 不應該改寫開發者維護的 profile JSON。
6. 使用者修改值應該存到本機 app settings，例如 `data/settings/source-path-overrides.json`。
7. backup manifest 需要記錄實際使用的 source path，方便日後追蹤。
8. 部分工具可以提供 `candidateSourcePaths`，讓 UI 顯示偵測到哪些候選設定檔存在；但實際備份仍以目前選中的 source path 為準。

### Candidate Source Paths

`candidateSourcePaths` 是 profile schema 的通用能力，用來處理一個 config item 可能有多個合法位置的情況。典型案例包含 psmux、WezTerm，以及其他支援 XDG-style 或 home-directory fallback 的工具。

設計規則：

1. `defaultSourcePath` 是還原 default 時使用的主要路徑。
2. `candidateSourcePaths` 是 UI 用來偵測與建議的候選路徑清單。
3. 如果候選路徑中只有一個存在，UI 可以建議使用該路徑。
4. 如果候選路徑中多個存在，UI 需要顯示衝突狀態，讓使用者選擇目前要管理哪一個。
5. 如果沒有候選路徑存在，仍顯示 default source path，狀態為 missing。
6. 使用者手動選定或修改的路徑寫入 source path override。
7. backup manifest 記錄實際使用的 source path，不只記錄 default。
8. restore 時預設還原到 backup manifest 記錄的 source path；若使用者已有 override，UI 需要提示目前 override 與 manifest path 不同。

UI 呈現建議：

1. 在 config item row 顯示目前 source path。
2. 若有候選路徑，提供 `Choose detected path` 或下拉選單。
3. 下拉選單中標示 `Found`、`Missing`、`Default`。
4. 提供 `Browse` 修改任意路徑。
5. 提供 `Restore default path`。

候選路徑不應該造成多份同時備份。若要備份多個候選檔案，應該建成多個 config items，而不是讓單一 item 同時複製多個 candidate。

## Startup 設定

工具需要提供 startup 設定入口。第一版使用 Windows Startup folder 方式，不使用 Registry 或 Task Scheduler。

Startup scope：

1. `common`：system/common startup，預設選項。
2. `user`：目前使用者 startup。

路徑概念：

```text
Common Startup: {CommonStartup}
User Startup: {Startup}
```

需求：

1. UI 提供 `Startup settings` 按鈕。
2. 預設 scope 使用 `common`。
3. 使用者可以切換為 `user`。
4. 顯示目前 startup shortcut 是否存在。
5. 提供 `Enable startup`。
6. 提供 `Disable startup`。
7. 提供 `Open startup folder`。
8. 若 common startup 需要系統權限，UI 應顯示提示，不應靜默失敗。

第一版 startup 目標：

1. 建立指向 `oo-win-config.exe` 的 shortcut。
2. 不自動啟動備份。
3. 不自動還原設定。
4. 僅讓工具隨 Windows 啟動，除非後續另行定義 startup action。

Path token 第一版建議：

1. `{AppData}`：對應 `%APPDATA%`。
2. `{LocalAppData}`：對應 `%LOCALAPPDATA%`。
3. `{UserProfile}`：對應 `%USERPROFILE%`。
4. `{ProgramFiles}`：對應 `%ProgramFiles%`。

範例：

```json
{
  "id": "doublecmd",
  "name": "Double Commander",
  "configItems": [
    {
      "id": "shortcuts",
      "name": "Keyboard shortcuts",
      "kind": "file",
      "defaultSourcePath": "{AppData}/doublecmd/shortcuts.scf",
      "backupPath": "doublecmd/shortcuts.scf",
      "required": true
    }
  ]
}
```

## 備份行為

備份時：

1. 讀取開發者定義的軟體設定檔清單。
2. 檢查來源路徑是否存在。
3. 顯示存在、缺失、偵測不到來源等狀態。
4. 使用者勾選要備份的項目。
5. 將檔案或資料夾複製到備份位置。
6. 保留相對於該軟體 profile 的結構。
7. 寫入備份結果紀錄。

還原時：

1. 讀取備份位置內的軟體設定檔。
2. 顯示可還原項目。
3. 使用者勾選要還原的項目。
4. 複製回原始設定位置。
5. 若目標已存在，需提示覆蓋策略。

## 偵測行為

偵測結果只影響 UI 提示，不阻止備份流程。

可能狀態：

1. Installed：偵測到軟體。
2. NotFound：偵測不到軟體。
3. Unknown：偵測方式失敗或尚未定義。

來源偵測可以包含：

1. `winget list`。
2. `choco list --local-only`。
3. 可執行檔命令，例如 `nvim --version`。
4. 常見設定檔路徑是否存在。

## UI 草案

主畫面需要呈現：

1. 軟體清單。
2. 每個軟體的偵測狀態。
3. 每個軟體的版本資訊，如果可偵測。
4. 設定檔項目清單與勾選框。
5. 來源路徑存在狀態。
6. 依賴與建議安裝指令。
7. 備份按鈕。
8. 還原按鈕。
9. 操作結果與錯誤訊息。

備份入口：

1. Selected profile config items 欄位底部提供 `Create backup`，只針對目前選中的單一 profile 與其中勾選的 config items 建立 snapshot。
2. Selected profile config items 欄位底部提供 `Compare to stored config`，用來針對目前選中的 software profile 選擇既有 snapshot 進行 diff。
3. Software profiles 欄位底部提供 `Backup profiles`，當左側有任一或多個 software profile 被勾選時啟用。
4. `Backup profiles` 用於一次替多個 profile 建立 snapshot。第一版可使用每個 profile 的全部 config items；後續應加確認 popup。

Snapshot map：

1. Snapshot map 不再是頂部 tab。
2. Snapshot map 由 Backup page 的 `Selected profile config items` header 旁按鈕進入。
3. 進入 Snapshot map 後，左側共用原本 profile list，左側標題從 `Softwares` 切換成 `Profiles`。
4. Snapshot map mode 可以返回 config items mode。
5. Snapshot map mode 的左側 profile card 不顯示 checkbox。
6. Snapshot map mode 單擊左側 profile card 會切換該 profile 是否納入 map。
7. 被納入 map 的 profile card 應顯示綠色外框；再次單擊同一張 card 取消選取並移除綠色外框。
8. Map 內容會隨左側 selected profiles 即時更新。
9. 中間 map 以 software profile 作為 Y 軸，由上往下排列。
10. 每個 software profile row 以 snapshot date 作為 X 軸，由左到右遞增，最右側為最近日期。
11. Snapshot date headers 應從 card 內抽出到 timeline 頂部列，並與 snapshot cards 使用一致欄寬。
12. 同一日期的 snapshot cards 必須在不同 software rows 中縱向對齊。
13. Snapshot card 以三行呈現：`name`、`date`、`tag`。
14. Snapshot card 不使用 checkbox；若需要比較，第一版可沿用 click/toggle card selection，但視覺上應是 card selected state，而不是 checkbox。
15. Snapshot/config card 樣式必須適配 dark theme，不可使用亮底或預設系統色造成突兀背景。
16. Timeline scroll viewer 在查詢或 map 更新後，預設水平捲到最右側，讓使用者先看到最近 snapshot。
17. 右側欄位拆成獨立 widgets：`Actions` 在上，`Filter` 在下。
18. `Compare` action 只在剛好選到兩張 snapshot card 時啟用。
19. `Delete snapshot` action 只在選到至少一張 snapshot card 時啟用。
20. Actions/Tags sidebar 可以 toggle 隱藏或顯示。
21. Sidebar toggle 預設提供兩種方式：點擊右側 sidebar rail 的箭頭，或使用 `Ctrl+R`。
22. Sidebar 展開時箭頭顯示向右；收起時箭頭顯示向左。
23. Compare 會開啟同視窗 overlay popup 顯示 diff。
24. Diff popup 需要提供 linked scroll / unlinked scroll 控制鈕。
25. 右側需要支援依 tag/category 過濾，例如 `All`、`Manual`、`Batch`。
26. Snapshot map mode 左側 profile 欄仍提供 `All` / `Clear`，但行為是選取或清空 map profiles，不是 backup checkbox selection。
27. 第一版先建立版面與資料入口，後續再接 backup manifest 產生實際矩陣。

Trash map：

1. 使用 `Delete snapshot` 不直接永久刪除 snapshot，而是將 snapshot 目錄搬到 trash。
2. Trash 目錄結構為 `data/trash/{scope}/{profileId}/{snapshotId}/`。
3. 第一版 scope 包含 `general` 與 `remote`。
4. Snapshot map header 提供 `Trash` 入口，以 map mode 檢視 trash 中的 snapshots。
5. Trash mode 左側仍使用 profile list 選擇要看的 software。
6. Trash mode 可在左上控制 `General` / `Remote` scope。
7. 右鍵 `Open in Explorer` 在 trash mode 也應開啟 trash 中的 snapshot folder。

Remote page：

1. 新增 `Remote` tab，與 `Backup`、`Snapshot map` 在 header 同列顯示。
2. Remote tab 第一版是 UI 骨架，不實作 SSH 連線。
3. 左側顯示 remote targets，例如 Kali/Linux 與 Windows SSH target。
4. 中間顯示 remote collection plan，以 profile 定義要收集哪些 config。
5. 右側顯示 SSH connection/package 設定。
6. Remote collection 輸出應維持 profile-based package 形式，包含 manifest 與 setup scripts。
7. 目標平台第一階段先考慮 Linux/Kali 與 Windows。
8. 當 SSH 連線成功後，Remote tab 應切換到類似 Backup tab 的檢視：左側顯示遠端可用 software profiles，中間顯示該 profile 的 remote config items。
9. Remote config items 由 remote software profile 定義，行為類似本機 config item，但 source path 代表遠端路徑。
10. Remote tab 的主要動作不是本機 backup，而是 `Collect selected`，將遠端檔案收集成本機 profile-based package/snapshot。
11. `New remote` 不常駐在右側欄位，應以 in-window overlay/popup 顯示 connection settings。
12. SSH 成功連線後，SSH config 側欄應收起，頁面轉成類 Backup 的 remote profiles + selected remote config items layout。

UI 外觀與設定需求：

1. 預設使用暗色背景。
2. 左側軟體清單每個項目需要比一般 list row 更高，方便顯示軟體名稱、ID、狀態與後續補充資訊。
3. 提供 `Settings` overlay 管理軟體設定，不使用可見的 Configuration tab。
4. `Settings` overlay 第一版包含 UI scale 與 base font size。
5. 快捷鍵 `Ctrl+,` 開啟 `Settings` overlay。
6. UI 設定存入 `data/settings/ui-settings.json`。
7. 調整 UI scale 或 base font size 後自動保存。
8. 重開程式時載入上次設定。
9. Settings 應提供 light background/theme 選項，但預設仍使用 dark theme。
10. Settings 應提供 shortcuts 區，列出目前預設快捷鍵，例如 `Ctrl+,` 開啟 settings、`Ctrl+R` toggle snapshot sidebar。

目前 UI 決策：主畫面一次只檢視一個軟體 profile。因為每個軟體的設定檔路徑、依賴、可備份項目都不同，單一 profile 檢視會比同時混合多個軟體更清楚。多軟體同時顯示或比較可以先保留成後續功能入口。

WPF 可落地方式：

1. 主畫面使用 `Grid` 建立三欄 layout：軟體清單、單一 profile 預覽、backup 清單。
2. 軟體清單使用 `ListBox` 或 `ItemsControl` 搭配 `DataTemplate`。
3. 單一 profile 的設定檔項目使用 `DataGrid`，包含勾選欄、項目名稱、來源路徑、狀態、大小。
4. backup 檔清單使用 `ListBox` 或 `ItemsControl` 搭配 card-like `DataTemplate`，顯示日期、大小、檔案數、標籤與軟體版本。
5. diff 結果第一版使用 `DataGrid`，顯示新增、刪除、修改、未變更。
6. diff popup 使用同一個 `Window` 內的 overlay `Grid`，不另外 spawn 子視窗。
7. 不需要嵌入 WebView；HTML demo 只作為畫面規劃參考。

## GUI 技術決策

目前決策：使用 WPF，不使用 WinForms。

理由：

1. 需求已經包含三欄 layout、backup card、單一 profile view、diff overlay 與未來 bundle view，WPF 的 layout 與 data binding 比 WinForms 更適合。
2. WPF 的 `Grid`、`DataTemplate`、`Binding` 能讓畫面結構跟資料模型保持清楚。
3. diff popup 可以用主視窗內的 overlay `Grid` 實作，不需要開新視窗。
4. 未來若要加入 multi-profile view 或 `yasb`、`komorebi`、`ahk` bundle，WPF 擴充成本較低。

代價：

1. 初期需要 XAML 與基本 MVVM 結構。
2. 比 WinForms 多一點架構成本。
3. 不追求華麗 UI，只使用 WPF 取得較好的 layout 與維護性。

Diff overlay 原則：

1. 不使用半透明效果。
2. 不開啟新視窗。
3. 在主 `Window` 最上層放一個預設 `Visibility=Collapsed` 的 overlay `Grid`。
4. 點擊 `Run diff` 時切換為 `Visible`。
5. overlay 中央顯示檔案檢視/比較 panel，內容包含 summary、file-level diff table、目前選中設定檔內容、search、scroll、關閉按鈕。

Popup overlay 適合承載的功能：

1. Diff 檢視：目前選中軟體的目前設定檔 vs 該軟體某份 backup。
2. Current file preview：顯示目前選中的設定檔內容。
3. Backup file preview：顯示選中 backup 中對應檔案的內容。
4. Side-by-side preview：左右比對目前檔案與 backup 檔案。
5. Search：在目前開啟的檔案內容中搜尋文字。
6. Scroll：大檔案內容可捲動檢視。
7. Metadata：顯示檔案大小、修改日期、hash、來源路徑、備份相對路徑。
8. Restore confirmation：從 popup 內確認還原單一檔案或整個選中 backup。
9. Copy path / copy content：複製來源路徑、backup 路徑或檔案內容。
10. Open external editor：用系統預設編輯器打開目前檔案，作為輔助功能。

第一版 popup 建議範圍：

1. 上方：標題列，顯示軟體名稱、backup label、關閉按鈕。
2. 左側：file-level diff 清單。
3. 右側：目前選中檔案內容 preview。
4. 右上：search box。
5. 右下：metadata 與操作按鈕。

第一版不需要做完整文字 diff 演算法。可以先在 popup 中顯示目前檔案內容與 backup 檔案內容，並用檔案層級狀態標記 `Added`、`Modified`、`Deleted`、`Unchanged`。內容級 diff 可作為第二版。

## Backup 版本與 Diff

因為備份是 file-based，每份 backup 需要保存 metadata，至少包含：

1. backup ID。
2. 使用者可讀的版本標籤，例如 `lazyvim-clean-install`。
3. 建立日期。
4. 對應的 software profile ID。
5. profile schema 版本。
6. 偵測到的軟體版本，如果有。
7. 檔案數量與總大小。
8. 每個檔案的相對路徑、大小、最後修改時間、hash。

Diff 第一版建議做檔案層級 diff：

1. Added：目前設定中有，選定 backup 沒有。
2. Deleted：選定 backup 有，目前設定中沒有。
3. Modified：路徑相同但大小、修改時間或 hash 不同。
4. Unchanged：路徑相同且 hash 相同。

內容級 diff 可行，但不建議第一版全面啟用。建議只對小型文字檔按需開啟，例如 `.json`、`.lua`、`.toml`、`.yaml`、`.ahk`。大型檔案、binary 檔案與資料夾先只顯示檔案層級變化。

### Version 選擇

本專案中的 version 第一版定義為 backup snapshot 的收錄時間。也就是每次 backup snapshot 產生一份新的版本。

版本顯示建議格式：

```text
2026-10-08 21:30 - yazi-keymap-before-change
```

版本資料至少包含：

1. `snapshotId`：穩定 ID，例如 `20261008-213000`。
2. `createdAt`：收錄時間。
3. `label`：使用者輸入的簡短標籤。
4. `category`：snapshot 分類，例如 `manual` 或 `batch`。
5. `softwareId`：例如 `yazi`。
6. `profileId` 或 profile file，例如 `yazi.json`。
7. `appVersion`：偵測到的軟體版本，如果有。
8. `fileCount`。
9. `totalSize`。
10. `manifestPath`。

UI 需要支援：

1. 在 backup 清單中依時間排序顯示版本。
2. 在 popup overlay 中選擇要比較的版本。
3. 切換版本後重新計算目前 config 與該版本 snapshot 的 file-level diff。
4. 顯示選中版本的 label、時間、大小、檔案數。
5. 依 snapshot category 過濾，例如 `All`、`Manual backups`、`Batch backups`。

Snapshot category 第一版：

1. `manual`：使用者手動建立的備份。
2. `batch`：從 Backup page 使用 profile 批次備份流程建立的備份。

Batch 備份預設 label 建議使用 `batch-yyyyMMdd-HHmmss`。若同一天產生多份，可以顯示成：

```text
2026-10-08 21:30 - batch
```

### Backup Comment 功能

Comment 第一版定義為「使用者替某份 backup snapshot 加上的文字備註」。它不是 commit，也不是 Git commit。本專案不需要 Git 功能。

建立 backup snapshot 流程：

1. 使用者選擇軟體 profile。
2. 使用者勾選要收錄的 config items。
3. 使用者按下 `Create backup`。
4. 顯示 backup overlay 或 inline panel。
5. 使用者輸入 label，例如 `before-changing-yazi-keymap`。
6. 程式產生 snapshot ID 與 createdAt。
7. 程式複製檔案到 backup snapshot 目錄。
8. 程式產生 manifest，記錄檔案大小、修改日期、hash 與來源路徑。
9. UI 更新版本清單。

Backup overlay 第一版欄位：

1. Software：唯讀。
2. Selected config items：唯讀清單。
3. Label：必填或可用時間自動產生。
4. Category：預設 `manual`。
5. Comment：選填，用來描述這份設定收錄的原因或狀態。
6. CreatedAt：預設目前時間。
7. Buttons：`Create backup`、`Cancel`。

部署流程建立 backup 時：

1. Category 使用 `batch`。
2. Label 預設 `batch-yyyyMMdd-HHmmss`。
3. Comment 可記錄部署動作來源或部署目標，例如 `restore yazi from 20261008-213000`。
4. 產生方式仍然是直接複製檔案，不透過 Git。

建立 backup 後的目錄概念：

```text
data/backups/yazi/20261008-213000/
  manifest.json
  config/yazi.toml
  config/keymap.toml
  config/theme.toml
```

Git integration 不列入本專案需求。若未來另開需求，也必須與目前 file-based snapshot 機制分開處理，避免使用者混淆。

## 初始專案結構

已確認需要：

```text
/src
/dist
/doc/spec.md
```

建議待討論：

```text
/data
/data/profiles
/doc/adr
/doc/notes
/tests
```

專案名稱暫定：`oo-win-config`。

## 設定檔定義位置待決策

需要決定「開發者定義每個軟體支援哪些檔案」要放在哪裡。

候選方案：

1. 放在 C# 程式碼內。
2. 放在專案內的 JSON / YAML profile 檔，例如 `/data/profiles/nvim.json`。
3. 放在執行檔旁的外部 profile 檔，讓未來不用重新編譯即可調整。

目前決策：開發階段使用 JSON profile 檔，放在 repo 的 `/data/profiles`，publish 時複製到 portable 輸出目錄的 `/data/profiles`。正式 portable 成品放在 `/dist/oo-win-config`。理由是軟體設定檔清單屬於資料，不應該每次修改都重新編譯；但仍由開發者維護，不需要開放給一般使用者編輯。

## 備份檔存放位置待決策

需要決定實際備份出來的 config 放哪裡。

候選方案：

1. 放在 repo 內 `/backups`。
2. 放在使用者指定的任意資料夾。
3. 放在 `%USERPROFILE%/ConfigBackups`。
4. 放在 AppData，例如 `%APPDATA%/ConfigBackupManager/backups`。

目前決策：第一階段先不使用機器上的預設備份位置。portable build 內會有軟體隨附資料目錄 `/data`，用來放 profiles 與軟體需要攜帶的資料。實際 config 備份輸出位置仍需另行決定，避免誤把使用者真實備份資料混入程式內建資料。

## 專案名稱

目前名稱：`oo-win-config`。

名稱評估：

1. 優點：短、明確指向 Windows config、適合 repo 或工具名稱。
2. 風險：`oo` 的語意不明，第一次看到的人不容易知道它代表什麼。
3. 建議：如果 `oo` 是你的個人或工具鏈命名前綴，可以保留；如果沒有既有含義，建議改成更直覺的名稱。

候選名稱：

1. `oo-win-config`：保留目前命名，適合個人工具鏈。
2. `win-config-vault`：強調保存與集中管理 config。
3. `win-config-backup`：最直白，功能一眼可懂。
4. `config-pack`：偏 portable bundle 的語感，但 Windows 指向較弱。
5. `dotwin`：類似 dotfiles for Windows，短但比較品牌化。

目前建議：若這是個人使用或個人工具鏈，使用 `oo-win-config`；若未來可能公開或交給他人使用，使用 `win-config-backup` 或 `win-config-vault`。

## 後續要討論的問題

1. 設定檔定義要使用 JSON、YAML，還是 C# 程式碼。
2. 備份檔的預設存放位置。
3. 是否需要為每次備份產生 timestamp snapshot。
4. 還原時遇到既有檔案要覆蓋、跳過、或先備份原檔。
5. `yazi` 與 `nvim` 在這台機器上的實際設定路徑。
6. LazyVim 的哪些檔案算必備，哪些只是 cache 或 plugin data。
7. `yasb`、`komorebi`、`ahk` 這類互相關聯設定是否需要 group/profile bundle 概念。
