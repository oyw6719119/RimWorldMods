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

## 编译方法

使用 RimWorld 1.6 的程序集进行编译。项目引用 `RimWorldWin64_Data\Managed` 目录下的程序集（`Assembly-CSharp`、`UnityEngine.*` 等），以及 Harmony 类 Mod 提供的 `0Harmony.dll`。

| 项目                          | 输出目录                                |
|-------------------------------|-----------------------------------------|
| `BatchColonyCommands.csproj`  | `Dist\BatchColonyCommands\Assemblies\`  |
| `GeneImplantPreview.csproj`   | `Dist\GeneImplantPreview\Assemblies\`   |

## 开源许可

本项目使用 **MIT License** 开源，允许任何人自由使用、修改、分发和商用，仅要求保留版权与许可声明。详见 [LICENSE](LICENSE)。
