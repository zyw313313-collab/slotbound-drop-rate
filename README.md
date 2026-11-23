# Slotbound 爆率调节 / Slotbound Drop Rate

> 在运行中的游戏里调节普通抽取的稀有度概率。仅供单机使用，工具本身不修改游戏文件或存档。
> Adjust ordinary rarity rolls while the game is running. Single-player only. The tool does not directly edit game files or saves.

[![下载](https://img.shields.io/github/v/release/zyw313313-collab/slotbound-drop-rate?label=download&color=ff9fd2)](https://github.com/zyw313313-collab/slotbound-drop-rate/releases/latest)
[![网站](https://img.shields.io/badge/website-一键安装-ff9fd2)](https://zyw313313-collab.github.io/slotbound-drop-rate/)
[![English](https://img.shields.io/badge/lang-English-ff9fd2)](https://zyw313313-collab.github.io/slotbound-drop-rate/en.html)

**中文** ｜ [English](#english)

## 中文

### 功能

- 调节普通 / 高级 / 稀有 / 传说 / 棱彩五档爆率，百分比总和必须为 **100%**。
- **读取当前爆率**查看游戏中的实际数值，**应用一次**写入并验证新数值，**恢复原始爆率**还原本次修改前的数值。
- 中 / 英双语：跟随系统语言，也可在窗口右上角切换。

### 下载 / 安装

- **直接下载**：[SlotboundDropRate.exe](https://github.com/zyw313313-collab/slotbound-drop-rate/releases/latest/download/SlotboundDropRate.exe)
- **一键安装脚本**：[install.bat](https://github.com/zyw313313-collab/slotbound-drop-rate/raw/main/install.bat)（下载到 `%LOCALAPPDATA%\SlotboundDropRate` 并创建桌面快捷方式）

工具 **v1.1.1** 支持 **Windows x64 的 Steam 版 Slotbound v1.0.2（构建 26791687）和 v1.0.3（构建 26848798）**。需要 Windows 10/11 和 .NET Framework 4.x（系统自带）。

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

- **Direct download**: [SlotboundDropRate.exe](https://github.com/zyw313313-collab/slotbound-drop-rate/releases/latest/download/SlotboundDropRate.exe)
- **One-click installer**: [install.bat](https://github.com/zyw313313-collab/slotbound-drop-rate/raw/main/install.bat) (downloads to `%LOCALAPPDATA%\SlotboundDropRate` and creates a desktop shortcut)

Tool **v1.1.1** supports **Slotbound v1.0.2 (Steam build 26791687) and v1.0.3 (Steam build 26848798) for Windows x64**. Requires Windows 10/11 with .NET Framework 4.x (included in the OS).

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

产物：`dist\SlotboundDropRate.exe`。

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
