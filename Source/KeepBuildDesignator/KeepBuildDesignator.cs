using System;
using System.Collections;
using System.Reflection;
using Verse;

namespace KeepBuildDesignator
{
    public static class Core
    {
        private static Type FindType(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(name, false);
                if (type != null) return type;
            }
            return null;
        }

        public static void Install() => LongEventHandler.ExecuteWhenFinished(InstallPatches);

        private static void InstallPatches()
        {
            try
            {
                Assembly harmonyAssembly = FindHarmonyAssembly();
                if (harmonyAssembly == null)
                    throw new InvalidOperationException("0Harmony was not loaded.");
                Type harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony", true);
                Type harmonyMethodType = harmonyAssembly.GetType("HarmonyLib.HarmonyMethod", true);
                object harmony = Activator.CreateInstance(harmonyType, "local.keepbuilddesignator");
                object prefix = Activator.CreateInstance(harmonyMethodType, typeof(ProcessInputEventsPatch).GetMethod(nameof(ProcessInputEventsPatch.Prefix), BindingFlags.Static | BindingFlags.Public));
                Type managerType = FindType("Verse.DesignatorManager");
                MethodInfo target = managerType?.GetMethod("ProcessInputEvents", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                harmonyType.GetMethod("Patch").Invoke(harmony, new object[] { target, prefix, null, null, null });
                Log.Message("[KeepBuildDesignator] Harmony patch installed.");
            }
            catch (Exception exception)
            {
                Log.Error("[KeepBuildDesignator] Failed to install Harmony patch: " + exception);
            }
        }

        private static Assembly FindHarmonyAssembly()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetType("HarmonyLib.Harmony", false) != null)
                    return assembly;
            return null;
        }
    }

    internal static class ProcessInputEventsPatch
    {
        private static readonly Type DraggerType = FindType("Verse.DesignationDragger");
        private static readonly Type EventType = Type.GetType("UnityEngine.Event, UnityEngine.IMGUIModule");
        private static FieldInfo Field(string name) => DraggerType?.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo Dragging = Field("dragging");
        private static readonly FieldInfo StartDragCell = Field("startDragCell");
        private static readonly FieldInfo Buffer = Field("buffer");
        private static readonly FieldInfo DragCells = Field("dragCells");
        private static readonly FieldInfo TmpHighlightCells = Field("tmpHighlightCells");
        private static readonly FieldInfo NumSelectedCells = Field("numSelectedCells");
        private static readonly FieldInfo LastStart = Field("lastStart");
        private static readonly FieldInfo LastEnd = Field("lastEnd");
        private static bool suppressRightMouseUp;

        public static bool Prefix(object __instance)
        {
            object current = EventType?.GetProperty("current", BindingFlags.Static | BindingFlags.Public)?.GetValue(null, null);
            if (current == null || Convert.ToInt32(EventType.GetProperty("button")?.GetValue(current, null)) != 1)
                return true;
            string eventName = EventType.GetProperty("type")?.GetValue(current, null)?.ToString();
            bool mouseDown = eventName == "MouseDown";
            bool mouseUp = eventName == "MouseUp";
            if (!mouseDown && !mouseUp) return true;

            object selected = __instance.GetType().GetProperty("SelectedDesignator")?.GetValue(__instance, null);
            object dragger = __instance.GetType().GetProperty("Dragger")?.GetValue(__instance, null);
            if (selected == null || dragger == null)
                return true;

            if (mouseUp && suppressRightMouseUp)
            {
                suppressRightMouseUp = false;
                EventType.GetMethod("Use")?.Invoke(current, null);
                return false;
            }

            if (!IsDragging(dragger))
                return true;

            ClearDragState(dragger);
            suppressRightMouseUp = mouseDown;
            EventType.GetMethod("Use")?.Invoke(current, null);
            return false;
        }

        private static bool IsDragging(object dragger)
        {
            return Dragging != null && (bool)Dragging.GetValue(dragger);
        }

        private static void ClearDragState(object dragger)
        {
            Dragging?.SetValue(dragger, false);
            StartDragCell?.SetValue(dragger, IntVec3.Invalid);
            ClearList(Buffer, dragger);
            ClearList(DragCells, dragger);
            ClearList(TmpHighlightCells, dragger);
            NumSelectedCells?.SetValue(dragger, 0);
            LastStart?.SetValue(dragger, IntVec3.Invalid);
            LastEnd?.SetValue(dragger, IntVec3.Invalid);
        }

        private static void ClearList(FieldInfo field, object instance)
        {
            (field?.GetValue(instance) as IList)?.Clear();
        }

        private static Type FindType(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(name, false);
                if (type != null) return type;
            }
            return null;
        }
    }
}
