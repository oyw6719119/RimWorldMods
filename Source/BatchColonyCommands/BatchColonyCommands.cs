using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BatchColonyCommands
{
    public sealed class BatchColonyCommandsMod : Mod
    {
        public BatchColonyCommandsMod(ModContentPack content) : base(content)
        {
            Log.Message("[BatchColonyCommands] 批处理殖民命令已加载。");
        }
    }

    /// <summary>
    /// 解除征召：立即解除当前地图上所有自由殖民者的征召状态。
    /// 点击建筑师面板中的命令按钮即执行，无需再到地图上点击。
    /// </summary>
    public class Designator_BatchUndraft : Designator
    {
        private bool executed;

        public Designator_BatchUndraft()
        {
            defaultLabel = "解除全体殖民者征召";
            defaultDesc = "立即解除当前地图上所有已征召自由殖民者的征召状态。点击本按钮即执行。";
            icon = ContentFinder<Texture2D>.Get("UI/Buttons/BatchColony/Undraft", false);
        }

        public override void Selected()
        {
            base.Selected();
            executed = false;
        }

        public override void Deselected()
        {
            base.Deselected();
            executed = false;
        }

        // 设计器被选中后每帧调用：首帧立即执行命令，随后自动取消选择。
        public override void SelectedUpdate()
        {
            base.SelectedUpdate();
            if (!executed)
            {
                executed = true;
                ExecuteOnce();
            }
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        // 地图点击路径兜底（正常情况下按钮点击后已自动取消选择，不会走到这里）。
        public override void DesignateSingleCell(IntVec3 loc)
        {
            ExecuteOnce();
        }

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            ExecuteOnce();
        }

        private void ExecuteOnce()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("当前没有活动地图。", MessageTypeDefOf.RejectInput, false);
                Find.DesignatorManager.Deselect();
                return;
            }

            // 先快照列表再遍历：修改征召状态会使 MapPawns 的缓存失效，
            // 直接枚举 FreeColonistsSpawned 会抛出 "Collection was modified"。
            List<Pawn> colonists = new List<Pawn>(map.mapPawns.FreeColonistsSpawned);
            int count = 0;
            foreach (Pawn pawn in colonists)
            {
                if (pawn.drafter == null || !pawn.Drafted)
                    continue;
                pawn.drafter.Drafted = false;
                count++;
            }
            Messages.Message("已解除 " + count + " 名殖民者的征召。", MessageTypeDefOf.TaskCompletion, false);
            Find.DesignatorManager.Deselect();
        }
    }

    /// <summary>
    /// 安排活动区：将所有自由殖民者安排到选定的活动区。
    /// 点击建筑师面板中的命令按钮即弹出活动区选择菜单。
    /// </summary>
    public class Designator_BatchAssignArea : Designator
    {
        private bool executed;

        public Designator_BatchAssignArea()
        {
            defaultLabel = "安排活动区";
            defaultDesc = "将所有自由殖民者安排到选定的活动区、居住区，或设为无限制。点击本按钮后选择目标。";
            icon = ContentFinder<Texture2D>.Get("UI/Buttons/BatchColony/AssignArea", false);
        }

        public override void Selected()
        {
            base.Selected();
            executed = false;
        }

        public override void Deselected()
        {
            base.Deselected();
            executed = false;
        }

        public override void SelectedUpdate()
        {
            base.SelectedUpdate();
            if (!executed)
            {
                executed = true;
                ExecuteOnce();
            }
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        public override void DesignateSingleCell(IntVec3 loc)
        {
            ExecuteOnce();
        }

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            ExecuteOnce();
        }

        private void ExecuteOnce()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("当前没有活动地图。", MessageTypeDefOf.RejectInput, false);
                Find.DesignatorManager.Deselect();
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();

            // 无限制：解除所有殖民者的活动区限制。
            options.Add(new FloatMenuOption("无限制",
                delegate
                {
                    AssignArea(map, null);
                    Find.DesignatorManager.Deselect();
                }));

            // 居住区。
            Area homeArea = map.areaManager.Home;
            if (homeArea != null)
            {
                options.Add(new FloatMenuOption(homeArea.Label,
                    delegate
                    {
                        AssignArea(map, homeArea);
                        Find.DesignatorManager.Deselect();
                    }));
            }

            // 玩家自建的活动区（Area_Allowed）。屋顶区/露天区/除雪区/清理污染等
            // 特地区域不适合作为殖民者的活动区限制，且本游戏中文翻译里除雪区标签有损坏。
            foreach (Area area in map.areaManager.AllAreas)
            {
                if (!(area is Area_Allowed))
                    continue;
                Area selectedArea = area;
                options.Add(new FloatMenuOption(selectedArea.Label,
                    delegate
                    {
                        AssignArea(map, selectedArea);
                        Find.DesignatorManager.Deselect();
                    }));
            }

            // 先取消选择再弹出菜单：用户取消菜单后命令按钮不会停留在选中态。
            Find.DesignatorManager.Deselect();
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void AssignArea(Map map, Area area)
        {
            // 同样先快照，避免枚举期间缓存失效。
            List<Pawn> colonists = new List<Pawn>(map.mapPawns.FreeColonistsSpawned);
            int count = 0;
            foreach (Pawn pawn in colonists)
            {
                if (pawn.playerSettings == null)
                    continue;
                // area 为 null 表示无限制。
                pawn.playerSettings.AreaRestrictionInPawnCurrentMap = area;
                count++;
            }
            if (area == null)
                Messages.Message("已将 " + count + " 名殖民者设为无限制。", MessageTypeDefOf.TaskCompletion, false);
            else
                Messages.Message("已将 " + count + " 名殖民者安排到活动区：" + area.Label,
                    MessageTypeDefOf.TaskCompletion, false);
        }
    }
}
