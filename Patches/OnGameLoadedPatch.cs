using CasselGames.Data;
using CasselGames.UI;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Utility.IO;
using Utility.IO.Data;
using Utility.UI;

namespace RatopiaTwitchIntegration.Patches
{
    /*
    [HarmonyPatch(typeof(PlayDataMgr))]
    public class PlayDataMgrPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("BeforeLoad")]
        public static void TestPreLoadData(PlayDataMgr __instance)
        {
            ModBase.Instance.mls.LogMessage("Data will load");
        }

        [HarmonyPostfix]
        [HarmonyPatch("CreateGameData")]
        public static void CreateGameDataPost(PlayDataMgr __instance, StartSettingsData data)
        {
            ModBase.Instance.mls.LogMessage("CreateGameData called - " + __instance.LastDataFilePath);
        }

        [HarmonyPrefix]
        [HarmonyPatch("CreateGameData")]
        public static void CreateGameDataPre(PlayDataMgr __instance, StartSettingsData data)
        {
            ModBase.Instance.mls.LogMessage("CreateGameData called - " + __instance.LastDataFilePath);
        }

        [HarmonyPrefix]
        [HarmonyPatch("Save")]
        public static async void Save(PlayDataMgr __instance)
        {   
            
        }
    }*/ // all above is useless

    /*
    [HarmonyPatch(typeof(LoadingSceneMgr))]
    public class LoadingSceneMgrPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("LoadScene", new[] { typeof(string), typeof(bool) })]
        public static void LoadScene(string s_name, bool isNewGame)
        {
            ModBase.Instance.mls.LogMessage("Load scene - " + s_name + " | " + isNewGame);
        }
    }*/ // also useless
    [HarmonyPatch]
    public static class SaveLoadIO_InternalPatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(SaveLoadIO)
                .GetMethod("LoadAsyncLastFile", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                ?.MakeGenericMethod(typeof(D_Data)); // Replace with actual T
        }

        static bool Prefix(
            ref Action<bool> checkCallback,
            ref Action<D_Data, string, SaveLoadConfigure.TypeSaveLoadResult> resultCallback)
        {
            var originalCallback = resultCallback;

            resultCallback = (data, path, result) =>
            {
                originalCallback?.Invoke(data, path, result);
                ModBase.Instance.tUsers.LoadFrom(path);
            };

            return true;
        }
    }
    [HarmonyPatch(typeof(LoadMenuUI))]
    public class LoadMenuUIPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Load")]
        public static void Load(LoadMenuUI __instance, S_Data sData)
        {
            ModBase.Instance.tUsers.LoadFrom(sData.DataPath);
        }
    }
    [HarmonyPatch(typeof(SaveAlarmUI))]
    public class SaveAlarmUIPatch
    {
        private static string GetPathDirectory(D_Data dData, bool isAutoSave)
        {
            string path = SaveLoadIO.PATH_SAVE_DIRECTORY + "/" + dData.DirectoryName + (isAutoSave ? SaveLoadIO.PATH_AUTOSAVE_DIRECTORY : string.Empty) + "/";
            ModBase.Instance.mls.LogMessage(path);
            Directory.CreateDirectory(path);
            return path + dData.FileName + SaveLoadIO.PATH_DATA_EXTENSION;
        }

        [HarmonyPrefix]
        [HarmonyPatch("SaveAsync", new Type[] { typeof(D_Data), typeof(string), typeof(bool), typeof(Action<string>) })]
        public static void SaveAsync_Prefix(SaveAlarmUI __instance, D_Data dData,
                                           string addFileName, bool manual, Action<string> saveEndedCallback)
        {
            string path = GetPathDirectory(dData, !manual);
            ModBase.Instance.mls.LogMessage(path);
            ModBase.Instance.mls.LogMessage(dData);
            ModBase.Instance.mls.LogMessage(dData.CreateSummaryData());
            ModBase.Instance.mls.LogMessage(dData.CreateSummaryData().NowFileName);
            ModBase.Instance.mls.LogMessage(addFileName);

            // TODO: need to figure out the autosave indexing

            ModBase.Instance.tUsers.SaveTo(path);
        }
    }
    [HarmonyPatch(typeof(File), nameof(File.Create), new Type[] { typeof(string) })]
    public static class FileCreateHarmonyPatch
    {
        /// <summary>
        /// Prefix patch that runs before File.Create
        /// </summary>
        public static void Prefix(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return;

                // Ensure directory exists
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"[Harmony] Created directory: {directory}");
                }

                Console.WriteLine($"[Harmony] Attempting to create file: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Harmony] Error in File.Create prefix: {ex}");
            }
        }

        /// <summary>
        /// Postfix patch that runs after File.Create
        /// </summary>
        public static void Postfix(string path, FileStream __result)
        {
            Console.WriteLine(__result != null
                ? $"[Harmony] Successfully created file: {path}"
                : $"[Harmony] Failed to create file: {path}");
        }
    }
}