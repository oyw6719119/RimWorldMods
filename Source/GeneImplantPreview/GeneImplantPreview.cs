using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneImplantPreview
{
    public sealed class GeneImplantPreviewMod : Mod
    {
        public GeneImplantPreviewMod(ModContentPack content) : base(content)
        {
            LongEventHandler.ExecuteWhenFinished(Core.Install);
        }
    }

    internal static class Core
    {
        private static readonly Dictionary<Dialog_CreateXenogerm, Pawn> Targets =
            new Dictionary<Dialog_CreateXenogerm, Pawn>();
        private static PawnSummaryWindow summaryWindow;

        public static void Install()
        {
            try
            {
                Assembly harmonyAssembly = FindHarmonyAssembly();
                if (harmonyAssembly == null) throw new InvalidOperationException("0Harmony was not loaded.");
                Type harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony", true);
                Type harmonyMethodType = harmonyAssembly.GetType("HarmonyLib.HarmonyMethod", true);
                object harmony = Activator.CreateInstance(harmonyType, "local.geneimplantpreview");
                Patch(harmonyType, harmonyMethodType, harmony, typeof(GeneCreationDialogBase),
                    "DoBottomButtons", nameof(DoBottomButtonsPatch));
                Patch(harmonyType, harmonyMethodType, harmony, typeof(Dialog_CreateXenogerm),
                    "DrawSection", nameof(DrawSectionPatch));
                Log.Message("[GeneImplantPreview] Harmony patches installed.");
            }
            catch (Exception ex)
            {
                Log.Error("[GeneImplantPreview] Failed to install: " + ex);
            }
        }

        private static void Patch(Type harmonyType, Type harmonyMethodType, object harmony, Type targetType,
            string targetName, string patchName)
        {
            MethodInfo target = targetType.GetMethod(targetName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo method = typeof(Core).GetMethod(patchName, BindingFlags.Static | BindingFlags.NonPublic);
            object postfix = Activator.CreateInstance(harmonyMethodType, method);
            harmonyType.GetMethod("Patch").Invoke(harmony, new object[] { target, null, postfix, null, null });
        }

        private static Assembly FindHarmonyAssembly()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetType("HarmonyLib.Harmony", false) != null) return assembly;
            return null;
        }

        private static void DoBottomButtonsPatch(GeneCreationDialogBase __instance, Rect rect)
        {
            Dialog_CreateXenogerm dialog = __instance as Dialog_CreateXenogerm;
            if (dialog == null) return;
            Pawn target;
            Targets.TryGetValue(dialog, out target);
            string buttonLabel = target == null ? "载入殖民者基因" : "殖民者：" + target.LabelShortCap;
            bool previousGuiEnabled = GUI.enabled;
            GUI.enabled = true;
            try
            {
                Rect buttonRect = new Rect(rect.x + 210f, rect.y, 260f, 30f);
                if (Widgets.ButtonText(buttonRect, buttonLabel))
                    OpenPawnMenu(dialog);
                if (target != null && Mouse.IsOver(buttonRect))
                {
                    if (summaryWindow == null)
                    {
                        summaryWindow = new PawnSummaryWindow(dialog, target);
                        Find.WindowStack.Add(summaryWindow);
                    }
                }
                else if (summaryWindow != null)
                {
                    summaryWindow.Close(false);
                    summaryWindow = null;
                }

                string preview = target == null ? "未选择殖民者" : PreviewText(dialog, target);
                Widgets.Label(new Rect(rect.x + 350f, rect.y - 35f, 300f, 24f), preview);
            }
            finally
            {
                GUI.enabled = previousGuiEnabled;
            }
        }

        private static void DrawSectionPatch(Dialog_CreateXenogerm __instance, Rect rect,
            List<Genepack> genepacks, string label, ref float curY, ref float sectionHeight,
            bool adding, Rect containingRect)
        {
            if (adding) return;
            Pawn target;
            if (!Targets.TryGetValue(__instance, out target) || target == null || target.genes == null)
                return;
            List<Gene> endogenes = target.genes.Endogenes;
            if (endogenes == null || endogenes.Count == 0) return;

            float headingHeight = 26f;
            float cardWidth = 87f;
            float cardHeight = 68f;
            float gap = 4f;
            int columns = Math.Max(1, (int)((containingRect.width + gap) / (cardWidth + gap)));
            int rows = (endogenes.Count + columns - 1) / columns;
            float totalHeight = headingHeight + rows * (cardHeight + gap);
            Widgets.Label(new Rect(containingRect.x, curY, containingRect.width, headingHeight), "系谱基因（只读）");

            HashSet<GeneDef> effective = EffectiveGeneDefs(__instance, target);
            for (int i = 0; i < endogenes.Count; i++)
            {
                GeneDef displayedGene = endogenes[i].def;
                int row = i / columns;
                int column = i % columns;
                Rect geneRect = new Rect(containingRect.x + column * (cardWidth + gap),
                    curY + headingHeight + row * (cardHeight + gap), cardWidth, cardHeight);
                GeneUIUtility.DrawGeneDef(displayedGene, geneRect, GeneType.Endogene,
                    null, true, false, !effective.Contains(endogenes[i].def));
                string suppression = SuppressionText(__instance, target, displayedGene);
                if (!string.IsNullOrEmpty(suppression))
                    TooltipHandler.TipRegion(geneRect, () => suppression, displayedGene.shortHash);
            }
            curY += totalHeight;
            sectionHeight += totalHeight;
        }

        private static HashSet<GeneDef> EffectiveGeneDefs(Dialog_CreateXenogerm dialog, Pawn target)
        {
            HashSet<GeneDef> result = new HashSet<GeneDef>();
            List<GeneDefWithType> all = new List<GeneDefWithType>();
            foreach (Gene gene in target.genes.Endogenes)
                all.Add(new GeneDefWithType(gene.def, false));
            PropertyInfo selectedGenes = typeof(GeneCreationDialogBase).GetProperty("SelectedGenes",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            List<GeneDef> selected = selectedGenes?.GetValue(dialog, null) as List<GeneDef>;
            if (selected != null)
                foreach (GeneDef gene in selected)
                    all.Add(new GeneDefWithType(gene, true));
            foreach (GeneDef gene in GeneUtility.NonOverriddenGenes(all)) result.Add(gene);
            return result;
        }

        private static string SuppressionText(Dialog_CreateXenogerm dialog, Pawn target, GeneDef gene)
        {
            try
            {
                List<string> suppressors = new List<string>();
                foreach (Gene existing in target.genes.Endogenes)
                    if (existing.def != gene && GeneUtility.Overrides(existing.def, gene, false, false))
                        suppressors.Add(existing.def.LabelCap);

                PropertyInfo selectedGenes = typeof(GeneCreationDialogBase).GetProperty("SelectedGenes",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                List<GeneDef> selected = selectedGenes?.GetValue(dialog, null) as List<GeneDef>;
                if (selected != null)
                    foreach (GeneDef candidate in selected)
                        if (candidate != gene && GeneUtility.Overrides(candidate, gene, true, false))
                            suppressors.Add(candidate.LabelCap);

                return suppressors.Count == 0
                    ? string.Empty
                    : "被以下基因抑制：\n- " + string.Join("\n- ", suppressors);
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[GeneImplantPreview] Suppression tooltip failed: " + ex, 194022);
                return string.Empty;
            }
        }

        private static string PreviewText(Dialog_CreateXenogerm dialog, Pawn target)
        {
            try
            {
                PropertyInfo selectedGenes = typeof(GeneCreationDialogBase).GetProperty("SelectedGenes",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                List<GeneDef> genes = selectedGenes?.GetValue(dialog, null) as List<GeneDef>;

                // With no design genes selected, only lineage genes remain in the implant.
                if (genes == null || genes.Count == 0)
                {
                    GeneSet lineageGeneSet = new GeneSet();
                    foreach (Gene gene in target.genes.Endogenes)
                        lineageGeneSet.AddGene(gene.def);

                    int lineageMetabolism = lineageGeneSet.MetabolismTotal;
                    string lineageValue = lineageMetabolism >= 0 ? "+" + lineageMetabolism : lineageMetabolism.ToString();
                    return "植入后代谢率：" + lineageValue;
                }

                List<GeneDefWithType> implantedGenome = new List<GeneDefWithType>();
                foreach (Gene gene in target.genes.Endogenes)
                    implantedGenome.Add(new GeneDefWithType(gene.def, false));

                foreach (GeneDef gene in genes)
                    implantedGenome.Add(new GeneDefWithType(gene, true));

                int metabolism = 0;
                foreach (GeneDef gene in GeneUtility.NonOverriddenGenes(implantedGenome))
                    metabolism += gene.biostatMet;
                string value = metabolism >= 0 ? "+" + metabolism : metabolism.ToString();
                return "植入后代谢率：" + value;
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[GeneImplantPreview] Preview failed: " + ex, 194021);
                return "植入后代谢率：无法计算";
            }
        }

        private static string PawnSummary(Pawn pawn)
        {
            StringBuilder text = new StringBuilder();
            List<Trait> traits = pawn.story?.traits?.allTraits;
            if (traits != null && traits.Count > 0)
            {
                for (int i = 0; i < traits.Count; i++)
                {
                    if (i > 0) text.Append(", ");
                    text.Append(traits[i].LabelCap);
                    if (traits[i].Suppressed) text.Append("（已抑制）");
                }
                text.AppendLine();
            }

            List<SkillRecord> skills = pawn.skills?.skills;
            if (skills == null || skills.Count == 0)
                return text.ToString().TrimEnd();

            int rows = (skills.Count + 1) / 2;
            for (int row = 0; row < rows; row++)
            {
                string left = FormatSkill(skills[row]);
                int right = row + rows;
                if (right < skills.Count)
                {
                    text.Append(PadSkillColumn(left, 14));
                    text.Append(FormatSkill(skills[right]));
                }
                else
                {
                    text.Append(left);
                }
                text.AppendLine();
            }
            return text.ToString().TrimEnd();
        }

        private static string FormatSkill(SkillRecord skill)
        {
            string stars = skill.passion == Passion.Major ? "★★" :
                skill.passion == Passion.Minor ? "★" : string.Empty;
            return skill.def.LabelCap + stars + " " + skill.Level;
        }

        private static string PadSkillColumn(string value, int width)
        {
            int displayWidth = 0;
            foreach (char character in value)
                displayWidth += character <= 127 ? 1 : 2;
            return value + new string('\u00A0', Math.Max(1, width - displayWidth));
        }

        private static void OpenPawnMenu(Dialog_CreateXenogerm dialog)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            Map map = Find.CurrentMap;
            if (map != null)
                foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    Pawn captured = pawn;
                    options.Add(new FloatMenuOption(captured.LabelShortCap, () => Targets[dialog] = captured));
                }
            options.Add(new FloatMenuOption("清除选择", () => Targets.Remove(dialog)));
            if (options.Count == 1) options.Insert(0, new FloatMenuOption("当前地图没有可选殖民者", null));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        internal static void NotifySummaryClosed(PawnSummaryWindow window)
        {
            if (summaryWindow == window) summaryWindow = null;
        }
    }

    internal sealed class PawnSummaryWindow : Window
    {
        private readonly Dialog_CreateXenogerm owner;
        private readonly Pawn pawn;

        public PawnSummaryWindow(Dialog_CreateXenogerm owner, Pawn pawn)
        {
            this.owner = owner;
            this.pawn = pawn;
            doWindowBackground = true;
            draggable = false;
            resizeable = false;
            closeOnClickedOutside = false;
            closeOnCancel = false;
            absorbInputAroundWindow = false;
            windowRect = new Rect(UI.MousePositionOnUI.x, UI.MousePositionOnUI.y - 210f, 330f, 200f);
        }

        public override Vector2 InitialSize => new Vector2(330f, 200f);

        public override void PreClose()
        {
            Core.NotifySummaryClosed(this);
            base.PreClose();
        }

        public override void DoWindowContents(Rect rect)
        {
            if (owner == null || !owner.IsOpen)
            {
                Close(false);
                return;
            }
            Text.Font = GameFont.Small;
            List<Trait> traits = pawn.story?.traits?.allTraits;
            string traitText = traits == null ? string.Empty : string.Join(", ", traits.ConvertAll(t => t.LabelCap));
            Widgets.Label(new Rect(12f, 8f, rect.width - 24f, 24f), traitText);

            List<SkillRecord> skills = pawn.skills?.skills;
            if (skills == null) return;
            int rows = (skills.Count + 1) / 2;
            for (int row = 0; row < rows; row++)
            {
                DrawSkill(pawn, skills[row], 12f, 40f + row * 21f);
                int right = row + rows;
                if (right < skills.Count)
                    DrawSkill(pawn, skills[right], 160f, 40f + row * 21f);
            }
        }

        private static void DrawSkill(Pawn pawn, SkillRecord skill, float x, float y)
        {
            Passion basePassion = GetBasePassion(pawn, skill);
            string stars = basePassion == Passion.Major ? "★★" :
                basePassion == Passion.Minor ? "★" : string.Empty;
            Color previousColor = GUI.color;
            if (basePassion != Passion.None)
                GUI.color = new Color(1f, 0.72f, 0.72f);
            Widgets.Label(new Rect(x, y, 85f, 21f), skill.def.LabelCap + stars);
            GUI.color = previousColor;
            TextAnchor previousAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(x + 88f, y, 42f, 21f), skill.GetLevel(false).ToString());
            Text.Anchor = previousAnchor;
        }

        private static Passion GetBasePassion(Pawn pawn, SkillRecord skill)
        {
            Passion passion = skill.passion;
            if (pawn.genes == null) return passion;
            foreach (Gene gene in pawn.genes.GenesListForReading)
            {
                if (!gene.Active || gene.def.passionMod == null || gene.def.passionMod.skill != skill.def)
                    continue;
                if (gene.def.passionMod.modType == PassionMod.PassionModType.DropAll)
                    passion = Passion.None;
                else if (gene.def.passionMod.modType == PassionMod.PassionModType.AddOneLevel)
                    passion = passion == Passion.Major ? Passion.Minor :
                        passion == Passion.Minor ? Passion.None : Passion.None;
            }
            return passion;
        }
    }
}
