# Slotbound 爆率调节

> Slotbound（转运军团）单机爆率调节工具。运行时修改本局稀有度权重 —— **不改游戏文件、不改存档、不碰联机**。

[![下载](https://img.shields.io/github/v/release/zyw313313-collab/slotbound-drop-rate?label=download&color=ff9fd2)](https://github.com/zyw313313-collab/slotbound-drop-rate/releases/latest)
[![网站](https://img.shields.io/badge/website-一键安装-ff9fd2)](https://zyw313313-collab.github.io/slotbound-drop-rate/)

![界面](docs/app.png)

## 功能

- 五个稀有度独立调节：普通 / 高级 / 稀有 / 传说 / 棱彩
- 「应用一次」立即改写本局爆率串
- 「自动」每秒补一次，新开一局也自动生效
- 「恢复默认」一键还原
- 可选「同时改数值表（实验）」兼容把权重存成数字的版本

默认值来自全元升级存档的真实爆率：**80 / 15 / 4.5 / 0.4 / 0.1**（即传说 0.4%、棱彩 0.1%）。

## 下载 / 安装

- **直接下载**：[SlotboundDropRate.exe](https://github.com/zyw313313-collab/slotbound-drop-rate/releases/latest/download/SlotboundDropRate.exe)
- **一键安装脚本**：[install.bat](https://github.com/zyw313313-collab/slotbound-drop-rate/raw/main/install.bat)（下载到 `%LOCALAPPDATA%\SlotboundDropRate` 并创建桌面快捷方式）

需要 Windows 10/11，.NET Framework 4.x（系统自带）。

## 使用

1. 启动游戏，进入一局，先拉一次杆。
2. 运行 `SlotboundDropRate.exe`，UAC 弹窗点「是」。
3. 工具自动连接 `Slotbound.exe`，底部显示 PID。
4. 填百分比 → **应用一次**；底部会显示匹配/改写数量。
5. 想对新开一局生效：勾 **自动**。

> 游戏的数据包 `Slotbound.dat` 是加密的，改文件不可行；本工具只写运行中进程内存，关闭游戏即恢复。建议先备份 `%APPDATA%\slotbound_full`。

## 自己编译

需要 Windows 自带的 .NET Framework 4.x（`csc.exe`）：

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

产物：`dist\SlotboundDropRate.exe`。

## 结构

```
src/SlotboundDropRate.cs   主程序（WinForms + ReadProcessMemory/AOB 扫描）
src/app.manifest           要求管理员权限（游戏通常由管理员 Steam 启动）
assets/icon.ico            图标
docs/                      GitHub Pages 站点
build.ps1                  编译脚本
install.bat                一键安装脚本
```

## 免责声明

仅供单机娱乐，非官方工具。使用风险自负；请勿用于联机或任何影响他人的场景。
