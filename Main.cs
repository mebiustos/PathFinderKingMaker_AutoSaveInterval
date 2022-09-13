using HarmonyLib;
using Kingmaker.EntitySystem.Persistence;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace AutoSaveInterval
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public static readonly float _AutoSaveInterval_MIN = 0f;
        public static readonly float _AutoSaveInterval_MAX = 3600f;

        [Draw("Minimum Interval (sec)", Precision = 0, Min = 0, Max = 3600)] public float AutoSaveInterval = 300f;

        public void OnChange()
        {
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }

    public class Main
    {
        public static Settings settings;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Harmony harmony2 = new Harmony(modEntry.Info.Id + modEntry.Info.Author);
            harmony2.PatchAll(Assembly.GetExecutingAssembly());

            Main.settings = Settings.Load<Settings>(modEntry);
            Main.settings.AutoSaveInterval = Mathf.Clamp(
                Main.settings.AutoSaveInterval,
                Settings._AutoSaveInterval_MIN,
                Settings._AutoSaveInterval_MAX);

            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Space(5);
            Main.settings.Draw(modEntry);
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
    }

    [HarmonyPatch(typeof(SaveManager))]
    static class MySaveManager
    {
        private static readonly bool skipFirstSave = true;
        private static bool isFirstSave = true;

        private static float lastAutoSaveTime = 0f;

        [HarmonyPrefix]
        [HarmonyPatch("SaveRoutine")]
        public static bool Prefix(SaveInfo saveInfo)
        {
            var currentTime = Time.time;

            if (saveInfo.Type != SaveInfo.SaveType.Auto || (saveInfo.Type == SaveInfo.SaveType.Auto && saveInfo.IsAutoLevelupSave))
            {
                isFirstSave = false;
                lastAutoSaveTime = currentTime;
                return true;
            }

            if (isFirstSave && skipFirstSave == false)
            {
                isFirstSave = false;
                lastAutoSaveTime = currentTime;
                return false;
            }

            if (currentTime - lastAutoSaveTime > Main.settings.AutoSaveInterval)
            {
                lastAutoSaveTime = currentTime;
                return true;
            } else
            {
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(LoadingProcess))]
    static class MyLoadingProcess
    {
        [HarmonyPrefix]
        [HarmonyPatch("StartLoadingProcess")]
        public static bool Prefix(IEnumerator process)
        {
            if (process != null)
            {
                return true;
            } else
            {
                return false;
            }
        }
    }
}
