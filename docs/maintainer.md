# 构建与兼容维护 / Build and maintenance

[中文玩家教程](../README.md) · [English player guide](../README.en.md)

## 构建与测试 / Build and test

在仓库根目录执行以下命令。需要 Windows x64 和 .NET Framework 4.x C# 编译器。

Run these commands from the repository root on Windows x64 with the .NET Framework 4.x C# compiler.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\run.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

The Windows ZIP in `dist` bundles the EXE, the current profile, and the double-click compatibility launcher. GitHub Actions runs the tests and publishes the artifacts from the same build on a release tag.

## 验证构建 / Validate a game build

1. Steam 更新后退出并重启游戏，进入可丢弃的单机局。用玩家教程中的兼容助手生成候选；需要调查时，可运行下方的只读命令查看报告。
2. 测试候选是否能够正确应用和恢复爆率，并核对普通召唤的实际结果。移除配置后再次测试，确认不能写入。只有确认后才更新仓库根目录的配置并发布。同一定位器适用时，玩家可只替换配置，不必更换 EXE。
3. 若游戏 EXE、Lua 运行库或权重表已不匹配，不要跳过检查；先更新程序并重做游戏内验证。

After updating through Steam, restart the game, enter a disposable single-player run, and use the helper to generate a candidate. The command below writes a read-only report beside the EXE with the detected build, SHA-256, and live odds. Before updating the repository profile or publishing it, verify apply, real regular summons, restore, and rejection with the profile removed. If the game executable, Lua runtime, or rarity table no longer matches, update and retest the tool instead of bypassing its checks.

```powershell
Start-Process -FilePath .\dist\SlotboundDropRate.exe -ArgumentList '--scan','--lang=en' -Wait
```
