# Slotbound 爆率调节 / Slotbound Drop Rate

> 在运行中的游戏里调节普通抽取的稀有度概率。仅供单机使用，工具本身不修改游戏文件或存档。
> Adjust ordinary rarity rolls while the game is running. Single-player only. The tool does not directly edit game files or saves.

[![下载](https://img.shields.io/github/v/release/zyw313313-collab/slotbound-drop-rate?label=download&color=ff9fd2)](https://github.com/zyw313313-collab/slotbound-drop-rate/releases/latest)
[![网站](https://img.shields.io/badge/website-一键安装-ff9fd2)](https://zyw313313-collab.github.io/slotbound-drop-rate/)
[![English](https://img.shields.io/badge/lang-English-ff9fd2)](https://zyw313313-collab.github.io/slotbound-drop-rate/en.html)

**中文** ｜ [English](#english)

## 界面预览 / Interface preview

![Slotbound 爆率调节 v1.1.2 中文界面](docs/slotbound-v1.1.2-zh.png)

旧版 v1.1.2 界面预览 / Screenshot from the older v1.1.2 release. [View the English interface](docs/slotbound-v1.1.2-en.png).

## 中文

### 功能

- 调节普通 / 高级 / 稀有 / 传说 / 棱彩五档爆率，百分比总和必须为 **100%**。
- **读取当前爆率**查看游戏中的实际数值，**应用一次**写入并验证新数值，**恢复原始爆率**还原本次修改前的数值。
- 中 / 英双语：跟随系统语言，也可在窗口右上角切换。

### 下载 / 安装

- **直接下载**：[SlotboundDropRate.zip](https://github.com/zyw313313-collab/slotbound-drop-rate/releases/latest/download/SlotboundDropRate.zip)。将 `SlotboundDropRate.exe` 和 `slotbound-compatibility.ini` 解压到同一目录，保留两者，再运行 EXE。
- **一键安装脚本**：[install.bat](https://github.com/zyw313313-collab/slotbound-drop-rate/raw/main/install.bat)（下载最新发布的 ZIP，将两个文件安装到 `%LOCALAPPDATA%\SlotboundDropRate` 并创建桌面快捷方式）。
- **仅更新兼容配置**：若后续游戏更新只需更换配置，请关闭工具，下载官方发布页提供的独立 [slotbound-compatibility.ini](https://github.com/zyw313313-collab/slotbound-drop-rate/releases/latest/download/slotbound-compatibility.ini)，手动替换工具 EXE 同目录的文件，再打开工具。工具不会联网自动获取配置。

工具 **v1.2.0** 随附的配置仅支持写入 **Windows x64 的 Steam 版 Slotbound v1.0.4（构建 26899345）**，不再支持旧构建的写入。需要 Windows 10/11 和 .NET Framework 4.x（系统自带）。

游戏 EXE 和 Lua 运行库必须与已知版本匹配。运行库匹配时，未知 Steam 构建或不匹配的 `Slotbound.dat` 仍可尝试只读诊断，但不能应用或恢复。配置缺失、无效或不匹配时绝不会启用写入。游戏 EXE 或 Lua 运行库发生变化时，连只读诊断也会停止。游戏更新后请重启游戏。

### 使用

1. 启动游戏，进入一局并等待加载完成，再运行 `SlotboundDropRate.exe`。
2. 点击 **读取当前爆率**，查看游戏中的五档实际数值。
3. 设置五项百分比，使总和为 **100%**，再点击 **应用一次**。
4. 如需还原，点击 **恢复原始爆率**。重启游戏也会清除爆率修改，但游戏正常存档可能保留已经获得的结果。建议先备份 `%APPDATA%\slotbound_full`。

普通稀有度抽取需要转出匹配的符号。剧情或其他脚本指定的结果可能不遵循这些概率。受控实机测试中，棱彩设为 100% 时出现了一个棱彩长矛单位，传说设为 100% 时出现了两个传说骷髅法师。

## English

### Features

- Set odds for Normal / Advanced / Rare / Legendary / Prismatic. The five percentages must add up to **100%**.
- **Read current odds** shows the live values. **Apply once** writes and verifies the new values. **Restore original** restores the odds from before the tool's edits.
- The bilingual UI follows your system language. Switch between English and Chinese in the top-right corner.

### Download / Install

- **Direct download**: [SlotboundDropRate.zip](https://github.com/zyw313313-collab/slotbound-drop-rate/releases/latest/download/SlotboundDropRate.zip). Extract `SlotboundDropRate.exe` and `slotbound-compatibility.ini` into the same folder, keep them together, then run the EXE.
- **One-click installer**: [install.bat](https://github.com/zyw313313-collab/slotbound-drop-rate/raw/main/install.bat) (downloads the latest release ZIP, installs both files in `%LOCALAPPDATA%\SlotboundDropRate`, and creates a desktop shortcut).
- **Profile-only updates**: If a later game update only needs a new profile, close the tool, download the standalone [slotbound-compatibility.ini](https://github.com/zyw313313-collab/slotbound-drop-rate/releases/latest/download/slotbound-compatibility.ini) from Releases, manually replace the file beside the tool EXE, and reopen the tool. The tool never fetches profiles automatically.

The profile shipped with tool **v1.2.0** enables writing only for **Slotbound v1.0.4 (Steam build 26899345) for Windows x64**. Older builds are no longer supported for writes. Requires Windows 10/11 with .NET Framework 4.x (included in the OS).

If the game EXE and Lua runtime still match, an unknown Steam build or mismatched `Slotbound.dat` can be checked in read-only diagnostic mode; Apply and Restore cannot write. A missing, invalid, or mismatched profile never enables writing. If the game EXE or Lua runtime changes, even read-only diagnostics stop. Restart the game after Steam updates it.

### Usage

1. Start the game, enter a run and wait for it to finish loading, then open `SlotboundDropRate.exe`.
2. Select **Read current odds** to see the game's five live values.
3. Enter five percentages that add up to **100%**, then select **Apply once**.
4. Select **Restore original** to revert the odds. Restarting the game also clears the odds edits. Normal game saves may preserve outcomes you already obtained. Back up `%APPDATA%\slotbound_full` first.

The ordinary rarity roll requires matching symbols. Scripted overrides may produce different results. In a controlled in-game test, 100% Prismatic yielded one prismatic spear unit, and 100% Legendary yielded two legendary skeleton mages.

## 自己编译 / Build

需要 Windows 自带的 .NET Framework 4.x（`csc.exe`）：

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

产物 / Outputs: `dist\SlotboundDropRate.exe`, `dist\slotbound-compatibility.ini`, `dist\SlotboundDropRate.zip`（包含 EXE 和配置 / contains both files）。

## 验证兼容配置 / Maintaining the compatibility profile

1. 更新游戏后，重启游戏并进入一局。使用下面的只读扫描命令。成功时，EXE 同目录的 `scan_result.txt` 会记录 PID、检测到的 Steam 构建、配置支持的构建、只读诊断状态（写入未启用时）、`Slotbound.dat` 的 SHA-256 和五档当前概率。扫描失败时先检查报错；若游戏 EXE 或 Lua 运行库变化，不能仅更新配置。
2. 核对检测到的是目标 Steam 构建，五档概率符合预期。仅在验证通过后更新仓库根目录中的 `slotbound-compatibility.ini`：以已核实的游戏版本填入 `version`，以检测到的构建（不是报告中的“支持”构建）填入 `build`，以报告中的数据哈希填入 `dataSha256`。仅更新配置时保留 `schema=1` 和 `locator=lua-gc64-v1`。
3. 运行测试和构建。在单机测试局中确认新配置可读取、应用并恢复原始概率；再关闭工具，移除或换成无效配置，重新打开后确认无法写入。不要仅凭扫描成功就发布配置。

1. After updating, restart the game and enter a run. Run the read-only scan below. On success, `scan_result.txt` beside the EXE lists the PID, detected Steam build, build supported by the loaded profile, diagnostics-only status when writes are disabled, `Slotbound.dat` SHA-256, and the five live odds. If the scan fails, check its error first. A changed game EXE or Lua runtime requires more than a profile update.
2. Verify that the detected Steam build is the intended target and that the five odds look as expected. Only then update `slotbound-compatibility.ini` at the repository root: set `version` to the independently verified game version, `build` to the **detected** build (not the reported supported build), and `dataSha256` to the reported data hash. Keep `schema=1` and `locator=lua-gc64-v1` for a profile-only update.
3. Run the tests and rebuild. In a single-player test run, verify that the updated profile can read, apply, and restore the original odds. Then close the tool, remove or invalidate the profile, and reopen the tool to verify it cannot write. A successful scan alone is not enough to publish a profile.

```powershell
Start-Process -FilePath .\dist\SlotboundDropRate.exe -ArgumentList '--scan','--lang=en' -Wait
powershell -ExecutionPolicy Bypass -File tests/run.ps1
powershell -ExecutionPolicy Bypass -File build.ps1
```

## 结构 / Layout

```
src/SlotboundDropRate.cs   主程序（WinForms，中英双语）
src/RaritySession.cs       支持的游戏构建校验与运行时爆率读写
src/app.manifest           Windows 应用清单
assets/icon.ico            图标
docs/                      GitHub Pages 站点（index.html 中文 / en.html English）
build.ps1                  编译脚本
install.bat                一键安装脚本
```

## 免责声明 / Disclaimer

仅供单机娱乐，非官方工具。使用风险自负；请勿用于联机或任何影响他人的场景。
Unofficial tool, single-player entertainment only. Use at your own risk; do not use it for multiplayer or anything that affects other players.
