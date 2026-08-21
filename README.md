# RimWorldMods

A collection of RimWorld 1.6 mod projects, each targeting `.NET Framework 4.7.2` (net472).

## Mods

### BatchColonyCommands — 批处理殖民命令
批量对当前地图上的殖民者执行操作，无需逐个在地图上点击。
- **解除全体殖民者征召**：点击建筑师面板中的命令按钮，立即解除当前地图上所有已征召自由殖民者的征召状态。

### GeneImplantPreview — 基因植入预览
通过 Harmony 补丁在「创建异种人 (Xenogerm)」对话框中展示目标基因植入效果的预览。
- 使用反射动态定位 `HarmonyLib` 并安装补丁，patch 目标 `GeneCreationDialogBase` 与 `Dialog_CreateXenogerm`。
- 依赖一个由 Harmony 类 mod 提供的 `0Harmony.dll`。

## Building

Compile with the RimWorld 1.6 assemblies. Projects reference the DLLs under
`RimWorldWin64_Data\Managed` (`Assembly-CSharp`, `UnityEngine.*`) and a
Harmony-based mod's `0Harmony.dll`.

| Project                | Output                                     |
|------------------------|--------------------------------------------|
| `BatchColonyCommands.csproj`  | `Dist\BatchColonyCommands\Assemblies\` |
| `GeneImplantPreview.csproj`   | `Dist\GeneImplantPreview\Assemblies\`  |
