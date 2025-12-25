# 目标描述
设计并实现一款魔兽争霸风格的即时战略游戏，支持三大不对称种族，核心胜负条件为摧毁敌方主基地。最终打包为 Android 与 iOS 可运行的移动端应用。

## 用户审查必要
- 选择的游戏引擎（Unity vs Godot）及其移动端支持情况。
- 项目结构与技术栈的决定。
- 关键里程碑的时间预估。

## 拟议更改
### 项目初始化
- **[MODIFY]** `project_setup.md`：添加使用 Unity 2022 LTS 的项目创建步骤。
- **[NEW]** `DesignDoc.md`：完整的游戏设计文档，包括世界观、种族、系统设计。

### 核心系统实现
- **[NEW]** `Core/ResourceSystem.cs`：资源采集与管理。
- **[NEW]** `Core/UnitProduction.cs`：单位生产队列与建筑关联。
- **[NEW]** `Core/CombatSystem.cs`：单位战斗逻辑，包含伤害计算与死亡处理。

### 种族特性
- **[NEW]** `Races/FireTribe/`：火焰族特有的火焰法术与单位。
- **[NEW]** `Races/ShadowClan/`：暗影族的潜行与削弱技能。
- **[NEW]** `Races/SteelLegion/`：钢铁族的高防御机械单位。

### UI 与 HUD
- **[NEW]** `UI/MainHUD.unity`：资源显示、迷你地图、指令面板。
- **[NEW]** `UI/TouchControls.cs`：移动端触控指令实现。

### 音效与音乐
- **[NEW]** `Audio/Background.mp3`、`Audio/Effects/`：背景音乐与单位音效。

### 移动端适配
- **[MODIFY]** `ProjectSettings/PlayerSettings.asset`：分辨率、触控灵敏度、横竖屏支持。
- **[NEW]** `Mobile/ResolutionManager.cs`：动态适配不同屏幕尺寸。

### 性能优化
- 使用对象池（ObjectPool）管理单位实例。
- 减少 Draw Call，合并材质。
- 采用 LOD（Level of Detail）模型。

### 打包发布
- **[NEW]** `Build/AndroidBuild.sh`：使用 Unity CLI 自动化构建 Android APK。
- **[NEW]** `Build/iOSBuild.sh`：使用 Unity CLI 自动化构建 iOS 包。
- **[NEW]** `ReleaseNotes.md`：发布说明与安装指南。

## 验证计划
### 自动化测试
- 单元测试：`Tests/ResourceSystemTests.cs`、`Tests/CombatSystemTests.cs`。
- UI 自动化：使用 Unity Test Runner 检查 UI 元素是否正确显示。

### 手动验证
- 在 Android 与 iOS 真实设备上运行，检查触控响应、帧率、内存占用。
- 验证三大种族的独特机制是否按设计工作。
- 确认摧毁主基地后游戏结束并显示胜负界面。

### 打包检查
- 确认生成的 APK 大小在 150MB 以下，iOS 包符合 App Store 要求。
- 使用 `adb logcat` 与 Xcode Console 检查运行时错误。
