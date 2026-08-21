# RimWorldMods

一个 RimWorld 1.6 的 Mod 项目集合，每个项目均以 `.NET Framework 4.7.2`（net472）为目标框架。

## 包含的 Mod

### BatchColonyCommands —— 批处理殖民命令

批量对当前地图上的殖民者执行操作，无需再在地图上逐个点击。点击「建筑师」面板中的命令按钮即可执行。

- **解除全体殖民者征召**：点击按钮后立即解除当前地图上所有已征召自由殖民者的征召状态。
- **安排活动区**：点击按钮后弹出菜单，可将所有自由殖民者一次性安排到某个活动区、居住区，或设为「无限制」。

> 实现说明：按钮作为设计器（`Designator`）实现，选中后首帧自动执行并取消选择，避免地图点击路径。

### GeneImplantPreview —— 基因植入预览

通过 Harmony 补丁扩展「创建异种人（Xenogerm）」对话框，方便在创建前查看基因植入到某个殖民者后的效果。

- **载入殖民者基因**：在对话框底部添加按钮，可选择一位殖民者作为基因载入目标。
- **植入后代谢率**：实时预览将所选基因加上该殖民者的系谱基因后，植入物最终的代谢率。
- **系谱基因展示**：对话框中展示目标殖民者的系谱基因（只读）卡片。
- **抑制关系提示**：鼠标悬停时提示该基因被哪些已选择的基因或系谱基因抑制。

> 实现说明：通过反射动态定位 `HarmonyLib` 并安装补丁，目标是 `GeneCreationDialogBase.DoBottomButtons` 与 `Dialog_CreateXenogerm.DrawSection`，需要由其他 Harmony 类 Mod 提供 `0Harmony.dll`。

### DevInYourLanguage_zh —— Dev In Your Language 中文汉化（含补全）

[Dev In Your Language](https://steamcommunity.com/sharedfiles/filedetails/?id=2142743468) 的中文翻译包，用于将开发者模式中各类未翻译的工具、视图设置等本地化。本目录在原汉化 [Dev In Your Language_zh](https://github.com/Obsoletes/Dev-In-Your-Language_zh) 的基础上**补全了遗漏 / 未翻译的 19 处条目**（简体 + 繁体均补齐）。

- **补全条目**：`DrawRoomGroups`、`DebugAction_Take5000FlameDamage`、`DebugAction_DestroyTrees21x21`、`DebugAction_NameAnimalByNuzzling`、`EnableTranslationWindowInEnglish`、`DrawPatherState`、`DrawHateChanterPositions`、`AnomalyDarkeningFX`、`SearchIgnoresRestrictions`、`ShowHiddenInfo`、`DrawMapRooms`、`DrawMapGraphs`、`DrawDarknessOverlay`、`PauseOnError`、`DrawNonCombatantTimer`、`SingleThreadedDrawing`、`FastMonolithRespawn`、`DrawShamblerAlertMote`、`ShowHiddenPawns`。
- **依赖**：需先安装原模组 `Dev In Your Language`（Steam 创意工坊 ID `2142743468`，包 id `latta.devl10n`）。
- **版权声明**：原始翻译版权归原作者 **leafzxg、Observer** 所有，本目录仅做翻译补充，保留原作者署名与 [Github](https://github.com/Obsoletes/Dev-In-Your-Language_zh) 链接。

> **⚠️ 测试状态：暂时未通过（2026-08-21）**
>
> 本目录额外附带了原版基因（GeneDef）中文名称翻译（41 个，简体 + 繁体，位于 `Languages/*/DefInjected/GeneDef/`），
> 用于开发者工具「给殖民者添加基因」中的基因名称汉化。但在当前整合版游戏环境中**测试未通过**——
> 经多次尝试（嵌套 `Defs` 与扁平 `LanguageData` 两种 DefInjected 格式、修正 XML 声明、同步到游戏 Mods 目录并重启），
> 基因名称仍显示英文/defName。推测为该整合版游戏的翻译加载机制特殊，常规语言包的 `GeneDef` 覆盖未被加载。
> 该部分暂视为**未生效**，开发者工具其余补全条目不受影响。

## 编译方法

使用 RimWorld 1.6 的程序集进行编译。项目引用 `RimWorldWin64_Data\Managed` 目录下的程序集（`Assembly-CSharp`、`UnityEngine.*` 等），以及 Harmony 类 Mod 提供的 `0Harmony.dll`。

| 项目                          | 输出目录                                |
|-------------------------------|-----------------------------------------|
| `BatchColonyCommands.csproj`  | `Dist\BatchColonyCommands\Assemblies\`  |
| `GeneImplantPreview.csproj`   | `Dist\GeneImplantPreview\Assemblies\`   |

## 开源许可

本项目使用 **MIT License** 开源，允许任何人自由使用、修改、分发和商用，仅要求保留版权与许可声明。详见 [LICENSE](LICENSE)。
